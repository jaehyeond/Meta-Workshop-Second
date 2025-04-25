using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using System.Linq;
using Unity.Assets.Scripts.Resource;
using Unity.Assets.Scripts.Objects; // BaseObject 타입 참조를 위해 추가
using UnityEngine.Rendering;
using static Snake; // Visual Effect Graph 색상 변경을 위해 추가
// using Jaehyeon.Scripts; // 네임스페이스 불확실하여 일단 주석 처리


public class PlayerSnakeController : NetworkBehaviour
{
    // 세그먼트-스킨 매핑용 네트워크 직렬화 구조체
    private struct SegmentSkinData : INetworkSerializable, IEquatable<SegmentSkinData>
    {
        public ulong SegmentId;
        public int SkinIndex;
        
        public SegmentSkinData(ulong segmentId, int skinIndex)
        {
            SegmentId = segmentId;
            SkinIndex = skinIndex;
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SegmentId);
            serializer.SerializeValue(ref SkinIndex);
        }
        
        public bool Equals(SegmentSkinData other)
        {
            return SegmentId == other.SegmentId && SkinIndex == other.SkinIndex;
        }
        
        public override bool Equals(object obj)
        {
            return obj is SegmentSkinData other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(SegmentId, SkinIndex);
        }
    }
    
    // 세그먼트별 스킨 정보를 추적하는 NetworkList
    private readonly NetworkList<SegmentSkinData> _segmentSkins = new NetworkList<SegmentSkinData>();
    
    #region Dependencies
    /// <summary>게임 매니저 - 게임 상태 및 이벤트 관리</summary>
    private GameManager _gameManager;
    private ResourceManager _resourceManager;
    private SnakeBodyHandler _snakeBodyHandler;
    private ObjectManager _objectManager;
    private NetUtils _netUtils;
    #endregion



    #region Core Components
    [Header("Core Components")]
    [SerializeField] public Snake _snake;
    #endregion

    #region Skin Settings
    [Header("Skin Settings")]
    [Tooltip("플레이어 스킨으로 사용할 Material 리스트. Inspector에서 할당해야 합니다.")]
    public List<Material> playerSkins = new List<Material>();
    public Material targetMaterial;
    #endregion

    #region Runtime Variables
    // SnakeBodyHandler 중복 초기화 방지 플래그
    private bool _isBodyHandlerInitialized = false;

    private List<SnakeBodySegment> _bodySegmentComponents = new List<SnakeBodySegment>();
    /// <summary>스네이크의 이동 기록을 저장하는 큐</summary>
   
    #endregion
    
    #region Network Variables
    /// <summary>네트워크를 통해 동기화되는 플레이어 스킨 Material 인덱스</summary>
    public readonly NetworkVariable<int> NetworkSkinIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>네트워크를 통해 동기화되는 스네이크 속도</summary>
    public readonly NetworkVariable<float> NetworkSnakeSpeed = new(5.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>네트워크를 통해 동기화되는 플레이어 점수</summary>
    private readonly NetworkVariable<int> _networkScore = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>네트워크를 통해 동기화되는 스네이크 크기</summary>
    private readonly NetworkVariable<int> _networkBodyCount = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>네트워크를 통해 동기화되는 플레이어 ID</summary>
    private readonly NetworkVariable<NetworkString> _networkPlayerId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>네트워크를 통해 동기화되는 세그먼트 리스트</summary>
    #endregion
    public readonly NetworkVariable<float> NetworkSnakeScale = new(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);



    #region Unity Lifecycle


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        var lifetimeScope = FindAnyObjectByType<LifetimeScope>();
        if (lifetimeScope != null)
        {
            _gameManager = lifetimeScope.Container.Resolve<GameManager>();
            _resourceManager = lifetimeScope.Container.Resolve<ResourceManager>();
            _objectManager = lifetimeScope.Container.Resolve<ObjectManager>();
            _netUtils = lifetimeScope.Container.Resolve<NetUtils>();
        }

        // Log network state
        Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] OnNetworkSpawn - OwnerClientId: {OwnerClientId}, LocalClientId: {NetworkManager.Singleton.LocalClientId}, IsServer: {IsServer}, IsClient: {IsClient}, IsOwner: {IsOwner}");
        _snake = GetComponentInChildren<Snake>(true); 
        NetworkSkinIndex.OnValueChanged += HandleSkinIndexChanged;
        NetworkSnakeSpeed.OnValueChanged += HandleSnakeSpeedChanged;
        NetworkSnakeScale.OnValueChanged += HandleSnakeScaleChanged;


        // NetworkList 변경 이벤트 구독 (모든 클라이언트)
        _segmentSkins.OnListChanged += HandleSegmentSkinListChanged;
        
        if (IsServer && playerSkins.Count > 0)
        {
            NetworkSkinIndex.Value = UnityEngine.Random.Range(0, playerSkins.Count);
        }

        ApplyPlayerSkin(NetworkSkinIndex.Value);
        
        // 초기 속도 적용
        if (_snake != null && _snake.Head != null)
        {
            _snake.Head.ChangeSpeed(NetworkSnakeSpeed.Value);
        }

        if (IsServer)
        {
            InitializeServerState();
        }
        // 클라이언트 또는 초기화되지 않은 서버에서 SnakeBodyHandler 초기화 (시각적 요소 등에 필요)
        if (!_isBodyHandlerInitialized)
        {
            InitializeBodyHandler();
        }

        // 소유자만 입력 처리 및 카메라 설정
        if (IsOwner)
        {
            _gameManager.OnMoveDirChanged += HandleMoveDirChanged;
            StartCoroutine(FollowPlayerWithCamera());
        }
        
        // Late Joiner를 위한 추가 코드: 클라이언트만 실행
        if (IsClient && !IsServer) // 순수 클라이언트인 경우만 실행 (호스트는 제외)
        {
            // NetworkList의 초기 내용 로깅
            string content = "";
            for (int i = 0; i < _segmentSkins.Count; i++)
            {
                content += $"[{i}]: ID={_segmentSkins[i].SegmentId}, Skin={_segmentSkins[i].SkinIndex} | ";
            }
            Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Late Joiner 접속 - NetworkList 초기 상태 ({_segmentSkins.Count}개): {content}");
            
            if (_segmentSkins.Count > 0)
            {
                // 현재 NetworkList에 있는 모든 항목 처리 (Late Joiner용)
                Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Late Joiner: 기존 {_segmentSkins.Count}개 세그먼트 스킨 정보 적용 시작");
                
                // 지연 적용을 위한 코루틴 시작 (NetworkObject 동기화 완료 대기)
                StartCoroutine(ApplyInitialSkinsWithDelay());
            }
        }
    }// 4. 크기 변경 이벤트 핸들러
private void HandleSnakeScaleChanged(float previousScale, float newScale)
{
    Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Snake 크기 변경 감지: {previousScale} → {newScale}");
    
    // 헤드 크기 조정
    if (_snake != null && _snake.Head != null)
    {
        _snake.Head.transform.localScale = new Vector3(newScale, newScale, newScale);
    }
    
    // 모든 바디 세그먼트 크기 조정
    if (_snakeBodyHandler != null && _snakeBodyHandler._bodySegments != null)
    {
        foreach (var segment in _snakeBodyHandler._bodySegments)
        {
            if (segment != null)
            {
                segment.transform.localScale = new Vector3(newScale, newScale, newScale);
            }
        }
    }
}

    /// <summary>
    /// 네트워크 디스폰 시 호출되는 메서드
    /// 이벤트 구독 해지 및 정리 작업 수행
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // 색상 변경 감지 콜백 해제 -> 스킨 인덱스 변경 감지 콜백 해제
        if (NetworkSkinIndex != null)
        {
            NetworkSkinIndex.OnValueChanged -= HandleSkinIndexChanged;
        }
        // 크기 변경 감지 콜백 해제
        if (NetworkSnakeScale != null)
        {
            NetworkSnakeScale.OnValueChanged -= HandleSnakeScaleChanged;
        }
        // 속도 변경 감지 콜백 해제
        if (NetworkSnakeSpeed != null)
        {
            NetworkSnakeSpeed.OnValueChanged -= HandleSnakeSpeedChanged;
        }
        
        // NetworkList 이벤트 구독 해제
        _segmentSkins.OnListChanged -= HandleSegmentSkinListChanged;

        if (IsClient){}
  
        if (IsOwner && _gameManager != null)
        {
            _gameManager.OnMoveDirChanged -= HandleMoveDirChanged;
        }
        
    }

    #endregion

    #region Initialization
    /// <summary>
    /// 서버 상태 초기화 (NetworkVariables)
    /// </summary>
    private void InitializeServerState()
    {
        if (!IsServer) return;

        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] InitializeServerState: OwnerClientId={OwnerClientId}.");

        // Initialize Network Variables
        string playerId = "Player_" + OwnerClientId;
        int initialScore = 0;
        int initialSize = 1;
        float initialSpeed = 5.0f;

        // 플레이어 고유 색상 결정 (예시: Client ID 기반) -> 스킨 인덱스 결정으로 변경
        int initialSkinIndex = 0;
        if (playerSkins.Count > 0)
        {

            initialSkinIndex = UnityEngine.Random.Range(0, playerSkins.Count); // 랜덤 인덱스 선택
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name} Server ID:{NetworkObjectId}] Player Skins 리스트가 비어있습니다! 스킨 인덱스를 0으로 설정합니다. Inspector에서 Material을 할당해주세요.");
        }

        // 중복 호출 방지를 위해 값 변경 확인 (선택 사항)
        if (_networkPlayerId.Value != playerId) _networkPlayerId.Value = playerId;
        if (_networkScore.Value != initialScore) _networkScore.Value = initialScore;
        if (_networkBodyCount.Value != initialSize) _networkBodyCount.Value = initialSize;
        if (NetworkSkinIndex.Value != initialSkinIndex) NetworkSkinIndex.Value = initialSkinIndex; // 서버에서 스킨 인덱스 설정
        if (NetworkSnakeSpeed.Value != initialSpeed) NetworkSnakeSpeed.Value = initialSpeed; // 서버에서 초기 속도 설정

        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] NetworkVariables Set: PlayerID={_networkPlayerId.Value}, Score={_networkScore.Value}, BodyCount={_networkBodyCount.Value}, SkinIndex={NetworkSkinIndex.Value}, Speed={NetworkSnakeSpeed.Value}");

        InitializeBodyHandler(); 
    }

    /// <summary>
    /// SnakeBodyHandler 초기화 (서버/클라이언트 공통, 중복 방지 포함)
    /// </summary>
    private void InitializeBodyHandler()
    {
        if (_isBodyHandlerInitialized) return; // 이미 초기화되었으면 반환

        _snakeBodyHandler = GetComponent<SnakeBodyHandler>();
        if (_snakeBodyHandler == null)
        {
            _snakeBodyHandler = gameObject.AddComponent<SnakeBodyHandler>();
            Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Added SnakeBodyHandler.");
        }

        try
        {
            _snakeBodyHandler.Initialize(_snake);
            _isBodyHandlerInitialized = true; // 초기화 성공 시 플래그 설정
            Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Initialized SnakeBodyHandler. IsServer={IsServer}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} ID:{NetworkObjectId}] Error initializing SnakeBodyHandler: {ex.Message}");
        }
    }


    private IEnumerator FollowPlayerWithCamera()
    {
        if (_snake == null)
        {
             Debug.LogError($"[{GetType().Name} Owner - ID:{NetworkObjectId}] FollowPlayerWithCamera 코루틴 시작 시 _snake가 null입니다!");
             yield break;
        }

        Debug.Log($"[{GetType().Name} Owner - ID:{NetworkObjectId}] 카메라 추적 코루틴 시작. CameraProvider 및 Snake Head 대기.");
        float waitTime = 0f;
        const float maxWaitTime = 5f; // 5초간 대기

        // CameraProvider.Instance, _snake, _snake.Head가 모두 준비될 때까지 대기
        while ((CameraProvider.Instance == null || _snake.Head == null) && waitTime < maxWaitTime)
        {
            if (CameraProvider.Instance == null) Debug.LogWarning($"[{GetType().Name} Owner - ID:{NetworkObjectId}] CameraProvider 대기 중...");
            if (_snake.Head == null) Debug.LogWarning($"[{GetType().Name} Owner - ID:{NetworkObjectId}] Snake Head 대기 중...");

            yield return null; // 다음 프레임까지 대기
            waitTime += Time.deltaTime;
        }
        // 대기 후 확인
        if (CameraProvider.Instance != null && _snake.Head != null)
        {
             Debug.Log($"[{GetType().Name} Owner - ID:{NetworkObjectId}] CameraProvider와 Snake Head 준비 완료. 카메라 추적 시작.");
             CameraProvider.Instance.Follow(_snake.Head.transform);
        }
    }

    #endregion
    private void FixedUpdate()
    {
        // 스네이크와 핸들러가 유효한지 확인
        if (_snake == null || _snake.Head == null || _snakeBodyHandler == null)
        {
            return;
        }

        if (IsOwner)
        {
            _snakeBodyHandler.UpdateBodySegmentsPositions();
        }

    }

[ServerRpc]
public void NotifyFoodEatenServerRpc(int foodValue, ulong foodNetworkId)
{
    // 음식 타입 확인 (값 기반)
    string foodType = DetermineFoodTypeName(foodValue);
    Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] NotifyFoodEatenServerRpc 수신: Value={foodValue}, FoodID={foodNetworkId}, Type={foodType}");
    
    // 현재 값 저장
    int oldHeadValue = _snake._networkHeadValue.Value;
    int oldScore = _networkScore.Value;
    int segmentCount = _snakeBodyHandler.GetBodySegmentCount();
    
    Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 현재 상태: 헤드값={oldHeadValue}, 점수={oldScore}, 세그먼트수={segmentCount}");
    
    // 음식 종류별 처리
    switch (DetermineFoodType(foodValue))
    {
        case FoodType.Apple:
            ProcessAppleFood(foodValue, oldHeadValue, oldScore);
            break;
            
        case FoodType.Candy:
        case FoodType.Beer:
            if (segmentCount > 0)
            {
                ProcessNegativeFood(foodValue, oldHeadValue, oldScore);
            }
            else
            {
                Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] {foodType} 효과 무시: 몸통이 없어 적용되지 않음");
            }
            break;
            
        default:
            Debug.LogWarning($"[{GetType().Name} Server ID:{NetworkObjectId}] 알 수 없는 음식 값: {foodValue}");
            break;
    }
    
    // 음식 오브젝트 제거
    DespawnFoodObject(foodNetworkId, foodType);
    
    // 새 음식 생성
    SpawnNewRandomFood();
}

// 7. 추가 헬퍼 메서드
private FoodType DetermineFoodType(int foodValue)
{
    if (foodValue >= 30) return FoodType.Beef;
    if (foodValue > 0) return FoodType.Apple;
    if (foodValue <= -30) return FoodType.Beer;
    if (foodValue < 0) return FoodType.Candy;
    return FoodType.Unknown;
}

private string DetermineFoodTypeName(int foodValue)
{
    switch (DetermineFoodType(foodValue))
    {
        case FoodType.Apple: return "Apple";
        case FoodType.Beef: return "Beef";
        case FoodType.Candy: return "Candy";
        case FoodType.Beer: return "Beer";
        default: return "Unknown";
    }
}

// 8. 음식 유형별 처리 메서드
private void ProcessAppleFood(int foodValue, int oldHeadValue, int oldScore)
{
    // 점수와 헤드 값 업데이트
    _networkScore.Value += foodValue;
    _snake._networkHeadValue.Value += foodValue;
    
    int newScore = _networkScore.Value;
    int newHeadValue = _snake._networkHeadValue.Value;
    
    Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 점수 업데이트: {oldScore} + {foodValue} = {newScore}");
    Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 헤드 값 업데이트: {oldHeadValue} + {foodValue} = {newHeadValue}");
    
    // 매 4점마다 몸통 세그먼트 추가
    if (newScore >= 8 && (newScore / 4 > oldScore / 4))
    {
        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 점수 4점 단위 증가: {oldScore}→{newScore}, 새 세그먼트 추가");
        AddBodySegmentOnServer();
        UpdateBodyValuesOnServer(newHeadValue);
    }
    
    // BasicGameState 업데이트
    UpdateGameState("Apple");
}

private void ProcessNegativeFood(int foodValue, int oldHeadValue, int oldScore)
{
    // 음식 종류 결정
    string foodType = foodValue <= -30 ? "Beer" : "Candy";
    
    // 점수 업데이트 (음수가 되지 않도록)
    int newScore = Mathf.Max(0, oldScore + foodValue);
    _networkScore.Value = newScore;
    
    // 헤드 값 업데이트 (최소 2 유지)
    int newHeadValue = Mathf.Max(2, oldHeadValue + foodValue);
    _snake._networkHeadValue.Value = newHeadValue;
    
    Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 점수 업데이트: {oldScore} + {foodValue} = {newScore}");
    Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 헤드 값 업데이트: {oldHeadValue} + {foodValue} = {newHeadValue}");
    
    // 세그먼트 하나 제거
    Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] {foodType} 먹음: 세그먼트 하나 제거");
    RemoveLastBodySegmentOnServer();
    
    // BasicGameState 업데이트
    UpdateGameState(foodType);
}

private void UpdateGameState(string foodType)
{
    BasicGameState gameState = FindObjectOfType<BasicGameState>();
    if (gameState != null)
    {
        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] BasicGameState.UpdatePlayerScore 호출: clientId={OwnerClientId}, foodType={foodType}");
        gameState.UpdatePlayerScore(OwnerClientId, foodType);
    }
    else
    {
        Debug.LogWarning($"[{GetType().Name} Server ID:{NetworkObjectId}] BasicGameState를 찾을 수 없어 스코어보드 업데이트 불가");
    }
}
    
    /// <summary>
    /// 음식 오브젝트 디스폰 처리
    /// </summary>
    private void DespawnFoodObject(ulong foodNetworkId, string foodType)
    {
        try 
        {
            if (!IsServer)
            {
                Debug.LogWarning($"[{GetType().Name} ID:{NetworkObjectId}] 서버가 아닌 환경에서 DespawnFoodObject 호출됨");
                return;
            }
            
            if (_objectManager == null)
            {
                Debug.LogError($"[{GetType().Name} ID:{NetworkObjectId}] _objectManager가 null입니다");
                return;
            }
            
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(foodNetworkId, out NetworkObject foodNetObj))
            {
                if (foodNetObj != null)
                {
                    var baseObj = foodNetObj.GetComponent<BaseObject>();
                    if (baseObj != null)
                    {
                        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 서버에서 {foodType} 제거: FoodID={foodNetworkId}");
                        _objectManager.Despawn(baseObj);
                    }
                    else
                    {
                        Debug.LogWarning($"[{GetType().Name} Server ID:{NetworkObjectId}] FoodID={foodNetworkId}에 BaseObject 컴포넌트가 없음");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name} Server ID:{NetworkObjectId}] FoodID={foodNetworkId}를 찾을 수 없음");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] {foodType} 제거 중 오류: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 새로운 랜덤 음식 생성
    /// </summary>
    private void SpawnNewRandomFood()
    {
        try
        {
            Vector3 randomPosition = Util.GetRandomPosition();
            randomPosition.y = 0f;
            
            // AppleManager 사용하여 음식 생성
            var appleManager = GameObject.FindObjectOfType<AppleManager>();
            if (appleManager != null)
            {
                appleManager.SpawnApple();  // 이 메소드는 AppleManager의 SpawnablePrefabNames에서 랜덤으로 하나 선택함
                Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] AppleManager를 통해 새 음식 생성 요청됨");
            }
            else
            {
                // AppleManager가 없다면 직접 생성
                string[] foodPrefabs = {"Apple", "Beer", "Beef", "Candy"};
                string randomFood = foodPrefabs[UnityEngine.Random.Range(0, foodPrefabs.Length)];
                var newFood = _objectManager.Spawn<BaseObject>(randomPosition, Quaternion.identity, prefabName: randomFood);
                Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 새 {randomFood} 직접 생성됨: 위치={randomPosition}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] 새 음식 생성 중 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 마지막 바디 세그먼트 제거 (서버 전용 로직)
    /// </summary>
    private void RemoveLastBodySegmentOnServer()
    {
        if (_snakeBodyHandler == null)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] SnakeBodyHandler가 null입니다!");
            return;
        }
        
        int segmentCount = _snakeBodyHandler.GetBodySegmentCount();
        if (segmentCount <= 0)
        {
            Debug.LogWarning($"[{GetType().Name} Server ID:{NetworkObjectId}] 제거할 세그먼트가 없습니다. 현재 세그먼트 수: {segmentCount}");
            return;
        }
        
        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 마지막 세그먼트 제거 시작. 현재 세그먼트 수: {segmentCount}");
        
        // 마지막 세그먼트 가져오기
        BaseObject lastSegment = _snakeBodyHandler.GetLastSegment();
        if (lastSegment == null)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] 마지막 세그먼트를 가져오지 못했습니다!");
            return;
        }
        
        // NetworkObject 컴포넌트 가져오기
        NetworkObject networkObject = lastSegment.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] 세그먼트에 NetworkObject 컴포넌트가 없습니다!");
            return;
        }
        
        // 세그먼트 NetworkObjectId 저장
        ulong segmentNetworkId = networkObject.NetworkObjectId;
        
        try
        {
            // 서버에서 세그먼트 제거
            _snakeBodyHandler.RemoveLastSegment();
            
            // 클라이언트에 세그먼트 제거 알림
            NotifySegmentRemovedClientRpc(segmentNetworkId);
            
            // 네트워크 객체 디스폰
            networkObject.Despawn();
            
            Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 세그먼트 제거 완료: NetworkObjectId={segmentNetworkId}, 남은 세그먼트: {_snakeBodyHandler.GetBodySegmentCount()}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] 세그먼트 제거 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
        }
    }

    [ClientRpc]
    private void NotifySegmentRemovedClientRpc(ulong segmentNetworkId)
    {
        // 서버가 아닌 경우에만 처리 (클라이언트에서 실행)
        if (IsServer) return;
        
        Debug.Log($"[{GetType().Name} Client ID:{NetworkObjectId}] 세그먼트 제거 알림 수신: SegmentID={segmentNetworkId}");
        
        // SnakeBodyHandler 확인 및 재시도 로직 시작
        if (_snakeBodyHandler == null)
        {
            // 핸들러가 초기화되지 않았다면 초기화 시도
            InitializeBodyHandler();
            
            // 그래도 null이면 코루틴으로 재시도
            if (_snakeBodyHandler == null)
            {
                StartCoroutine(RetryRemoveSegmentWithDelay(segmentNetworkId));
                return;
            }
        }
        
        // 핸들러가 있으면 바로 처리
        ProcessSegmentRemoval(segmentNetworkId);
        }
    private IEnumerator RetryRemoveSegmentWithDelay(ulong segmentNetworkId)
    {
        // 최대 3번까지 재시도
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.3f * (i + 1)); // 점점 더 긴 간격으로 재시도
            
            Debug.Log($"[{GetType().Name} Client ID:{NetworkObjectId}] SnakeBodyHandler 초기화 재시도 #{i+1}");
            
            // 핸들러 초기화 재시도
            if (_snakeBodyHandler == null)
            {
                InitializeBodyHandler();
            }
            
            // 초기화 성공하면 세그먼트 제거 처리
            if (_snakeBodyHandler != null)
            {
                ProcessSegmentRemoval(segmentNetworkId);
            }
        }
        
        Debug.LogError($"[{GetType().Name} Client ID:{NetworkObjectId}] SnakeBodyHandler 초기화 최종 실패. 세그먼트 제거를 건너뜁니다.");
    }

    private void ProcessSegmentRemoval(ulong segmentNetworkId)
    {
        try
        {
            // 현재 세그먼트 수 기록
            int beforeCount = _snakeBodyHandler.GetBodySegmentCount();
            
            // 클라이언트에서 세그먼트 목록 업데이트
            _snakeBodyHandler.RemoveLastSegment();
            int afterCount = _snakeBodyHandler.GetBodySegmentCount();
            
            Debug.Log($"[{GetType().Name} Client ID:{NetworkObjectId}] 클라이언트에서 세그먼트 제거 완료: {beforeCount} → {afterCount}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Client ID:{NetworkObjectId}] 클라이언트에서 세그먼트 제거 중 오류: {ex.Message}");
        }
    }
    #region Movement and Body Management

    private void HandleMoveDirChanged(Vector2 moveDirection)
    {
        if (IsOwner)
        {
            // 클라이언트가 소유한 오브젝트의 움직임을 직접 제어
            // ClientNetworkTransform이 이 변경을 서버로 동기화
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Vector3 currentPosition = _snake.Head.transform.position;
                Vector3 targetDirection = new Vector3(moveDirection.x, 0, moveDirection.y).normalized;
                Vector3 targetPosition = currentPosition + targetDirection;
                _snake.Head.LookAt(targetPosition);
            }
        }
    }

    /// <summary>
    /// 새 몸체 세그먼트 추가 (서버 전용 로직)
    /// 서버에서 세그먼트 생성 및 네트워크 동기화
    /// </summary>
    private void AddBodySegmentOnServer()
    {
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        // 기존 세그먼트가 있는지 확인하고 마지막 세그먼트 또는 헤드를 참조로 사용
        BaseObject referenceObj = _snakeBodyHandler.GetBodySegmentCount() > 0 ? _snakeBodyHandler.GetLastSegment() : null;

        if (referenceObj == null)
        {
            if (_snakeBodyHandler.GetBodySegmentCount() > 0)
            {
                Debug.LogError($"[{GetType().Name} Server - ID:{NetworkObjectId}] AddBodySegmentOnServer: 마지막 세그먼트를 찾을 수 없습니다!");
                return;
            }
            referenceObj = _snake.Head.GetComponent<BaseObject>();
        }

        // 세그먼트 위치 및 회전 계산
        spawnPosition = referenceObj.transform.position - referenceObj.transform.forward * _snakeBodyHandler.GetSegmentSpacing();
        spawnRotation = referenceObj.transform.rotation;

        try
        {
            // 서버에서 스폰 및 소유권 이전 시도
            
            // 1. 프리팹 로드 (서버에서)
            GameObject segmentPrefab = _resourceManager.Load<GameObject>("Body Detail");
            if (segmentPrefab == null)
            {
                Debug.LogError($"[{GetType().Name} Server - ID:{NetworkObjectId}] \'Body Detail\' 프리팹 로드 실패!");
                return;
            }

            // 2. 프리팹 인스턴스화 (서버에서)
            GameObject segmentInstance = Instantiate(segmentPrefab, spawnPosition, spawnRotation);
            
            if (segmentInstance == null)
            {
                Debug.LogError($"[{GetType().Name} Server - ID:{NetworkObjectId}] 세그먼트 인스턴스화 실패!");
                return;
            }
            float currentScale = NetworkSnakeScale.Value;
            segmentInstance.transform.localScale = new Vector3(currentScale, currentScale, currentScale);
            Debug.Log($"[{GetType().Name} Server - ID:{NetworkObjectId}] 새 세그먼트에 스케일 {currentScale} 적용");

            // 스킨 적용 로직 추가 (활성화)
            int skinIndex = NetworkSkinIndex.Value;
            if (skinIndex >= 0 && skinIndex < playerSkins.Count)
            {
                // 현재 스킨 인덱스에 맞는 Material 가져오기
                Material segmentMaterial = playerSkins[skinIndex];
                if (segmentMaterial != null)
                {
                    SnakeSkin segmentSkin = segmentInstance.GetComponent<SnakeSkin>();
                    if (segmentSkin != null)
                    {
                        // ChangeTo 메소드를 사용해 스킨 적용
                        segmentSkin.ChangeTo(segmentMaterial);
                        Debug.Log($"[{GetType().Name} Server - ID:{NetworkObjectId}] 새 세그먼트에 스킨(인덱스:{skinIndex}) 적용 완료");
                    }
                }
            }

            // 3. NetworkObject 컴포넌트 가져오기
            NetworkObject networkObject = segmentInstance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError($"[{GetType().Name} Server - ID:{NetworkObjectId}] 생성된 세그먼트에 NetworkObject 컴포넌트가 없습니다!");
                Destroy(segmentInstance);
                return;
            }

            // 4. 서버에서 스폰
            networkObject.Spawn(true); // destroyWithScene = true
            Debug.Log($"[{GetType().Name} Server - ID:{NetworkObjectId}] 세그먼트 스폰 완료: NetworkObjectId={networkObject.NetworkObjectId}");
            
            // 5. NetworkList에 세그먼트-스킨 정보 추가 (Late Joiner를 위한 핵심 부분)
            _segmentSkins.Add(new SegmentSkinData(networkObject.NetworkObjectId, skinIndex));
            Debug.Log($"[{GetType().Name} Server - ID:{NetworkObjectId}] NetworkList에 세그먼트 {networkObject.NetworkObjectId}의 스킨 정보 추가");

            // 6. 소유권 클라이언트에게 이전
            networkObject.ChangeOwnership(OwnerClientId);
            Debug.Log($"[{GetType().Name} Server - ID:{NetworkObjectId}] 세그먼트 소유권 이전 -> Client {OwnerClientId}");

            // 7. 모든 클라이언트에게 스폰 알림 (PlayerController ID, Segment ID, 그리고 스킨 인덱스 전달)
            NotifySegmentSpawnedClientRpc(this.NetworkObjectId, networkObject.NetworkObjectId, skinIndex);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Server - ID:{NetworkObjectId}] 세그먼트 생성 중 오류: {ex.Message}\n{ex.StackTrace}");
        }
    }


    [ClientRpc]
    private void NotifySegmentSpawnedClientRpc(ulong ownerPlayerControllerId, ulong spawnedSegmentNetworkId, int skinIndex)
    {
        Debug.Log($"[{GetType().Name} Client - ID:{NetworkObjectId}] NotifySegmentSpawnedClientRpc 수신: PlayerControllerID={ownerPlayerControllerId}, SegmentID={spawnedSegmentNetworkId}, SkinIndex={skinIndex}");

        // 1. PlayerController 찾기
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ownerPlayerControllerId, out NetworkObject playerControllerNetworkObject) || playerControllerNetworkObject == null)
        {
            Debug.LogError($"[{GetType().Name} Client - ID:{NetworkObjectId}] PlayerControllerID={ownerPlayerControllerId}에 해당하는 NetworkObject를 찾지 못했습니다.");
            return;
        }
        PlayerSnakeController targetController = playerControllerNetworkObject.GetComponent<PlayerSnakeController>();
        if (targetController == null || targetController._snakeBodyHandler == null)
        {
            Debug.LogError($"[{GetType().Name} Client - ID:{NetworkObjectId}] PlayerControllerID={ownerPlayerControllerId}에서 PlayerSnakeController 컴포넌트 또는 SnakeBodyHandler를 찾지 못했습니다.");
            return;
        }


        // 2. 스폰된 세그먼트 찾기
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(spawnedSegmentNetworkId, out NetworkObject segmentNetworkObject))
        {
            if (segmentNetworkObject != null)
            {
                GameObject segmentInstance = segmentNetworkObject.gameObject;
                BaseObject segment = segmentInstance.GetComponent<BaseObject>();
                SnakeBodySegment segmentComponent = segmentInstance.GetComponent<SnakeBodySegment>();


                if (segment != null && segmentComponent != null)
                {
                                        // 클라이언트에서도 최신 스케일 적용
                    if (targetController.NetworkSnakeScale != null)
                    {
                        float currentScale = targetController.NetworkSnakeScale.Value;
                        segmentInstance.transform.localScale = new Vector3(currentScale, currentScale, currentScale);
                        Debug.Log($"[{GetType().Name} Client - ID:{NetworkObjectId}] 클라이언트에서 세그먼트에 스케일 {currentScale} 적용");
                    }

                    // 클라이언트에서도 스킨 적용
                    if (skinIndex >= 0 && skinIndex < targetController.playerSkins.Count)
                    {
                        Material segmentMaterial = targetController.playerSkins[skinIndex];
                        if (segmentMaterial != null)
                        {
                            SnakeSkin segmentSkin = segmentInstance.GetComponent<SnakeSkin>();
                            if (segmentSkin != null)
                            {
                                segmentSkin.ChangeTo(segmentMaterial);
                                Debug.Log($"[{GetType().Name} Client - ID:{NetworkObjectId}] 클라이언트에서 세그먼트에 스킨(인덱스:{skinIndex}) 적용 완료");
                            }
                        }
                    }

                    // 3. 타겟 컨트롤러의 핸들러에 세그먼트 추가
                    // 서버에서만 부모 설정을 수행하도록 수정
                    if (IsServer)
                    {
                        segment.transform.SetParent(targetController._snake.transform, true); // 부모 설정 (서버만 실행)
                        Debug.Log($"[{GetType().Name} Server - ID:{NetworkObjectId}] 서버에서 세그먼트의 부모 설정 완료");
                    }
              
                    // 핸들러에 세그먼트 추가 (모든 클라이언트 실행)
                    targetController._snakeBodyHandler.AddBodySegment(segment, segmentComponent);
                    Debug.Log($"[{GetType().Name} Client - ID:{NetworkObjectId}] PlayerControllerID={ownerPlayerControllerId}의 핸들러에 SegmentID={spawnedSegmentNetworkId} 추가 완료.");
                }
                else
                {
                    Debug.LogError($"[{GetType().Name} Client - ID:{NetworkObjectId}] SegmentID={spawnedSegmentNetworkId} 객체에서 필요한 컴포넌트(BaseObject/SnakeBodySegment)를 찾지 못했습니다.");
                }
            }
            else
            {
                Debug.LogError($"[{GetType().Name} Client - ID:{NetworkObjectId}] SegmentID={spawnedSegmentNetworkId}에 해당하는 NetworkObject를 찾았으나 null입니다.");
            }
        }
        else
        {
            Debug.LogError($"[{GetType().Name} Client - ID:{NetworkObjectId}] SegmentID={spawnedSegmentNetworkId}에 해당하는 NetworkObject를 찾지 못했습니다. 스폰 동기화 지연일 수 있습니다.");
        }
    }
  

    /// <summary>
    /// 몸체 세그먼트 값 업데이트 (서버 전용 로직)
    /// 헤드 값 변경 시 모든 세그먼트의 값 재계산
    /// </summary>
    private void UpdateBodyValuesOnServer(int headValue)
    {
        int segmentCount = _snakeBodyHandler.GetBodySegmentCount();
        if (segmentCount == 0) return;

    try
    {
        // 1. _bodySegmentComponents 리스트와 실제 세그먼트 수 동기화 (한 번만 수행)
        if (_bodySegmentComponents.Count < segmentCount)
        {
            // 기존 리스트 유지하며 부족한 부분만 채우기
            int startIndex = _bodySegmentComponents.Count;
            for (int i = startIndex; i < segmentCount; i++)
            {
                if (i < _snakeBodyHandler._bodySegments.Count && _snakeBodyHandler._bodySegments[i] != null)
                {
                    var segment = _snakeBodyHandler._bodySegments[i].GetComponent<SnakeBodySegment>();
                    SnakeSkin segmentSkin = segment.GetComponent<SnakeSkin>();
                    segmentSkin.ChangeTo(targetMaterial);

                    _bodySegmentComponents.Add(segment);
                }
                else
                {
                    _bodySegmentComponents.Add(null);
                }
            }
            Debug.Log($"[{GetType().Name} Server - ID:{NetworkObjectId}] 컴포넌트 리스트 크기 조정: {startIndex} -> {_bodySegmentComponents.Count}");
        }

        // 2. 헤드 값에 따른 세그먼트 값 계산
        float log2HeadValue = Mathf.Log(headValue, 2);
        int headPower = (int)Mathf.Round(log2HeadValue);

        // 3. 세그먼트 값 설정 (필요한 인덱스 검사만 수행)
        int validUpdates = 0;
        for (int i = 0; i < Mathf.Min(segmentCount, _bodySegmentComponents.Count); i++)
        {
            var segment = _bodySegmentComponents[i];
            if (segment == null) continue;

            int segmentValue;
            // 값 계산 로직 (간결화)
            if (i == segmentCount - 1)
                segmentValue = 2;
            else if (i == 0)
                segmentValue = Mathf.Max(2, (int)Mathf.Pow(2, headPower - 1));
            else
                segmentValue = Mathf.Max(2, (int)Mathf.Pow(2, headPower - i - 1));

            segment.SetValue(segmentValue);
            validUpdates++;
        }

        // 로그는 한 번만 출력
        Debug.Log($"[{GetType().Name} Server - ID:{NetworkObjectId}] 서버: 총 {validUpdates}개 세그먼트 값 업데이트 완료");

        // 4. 클라이언트 동기화 (한 번만 호출)
        SyncBodyValuesClientRpc();
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[{GetType().Name} Server - ID:{NetworkObjectId}] UpdateBodyValuesOnServer 오류: {ex.Message}\n{ex.StackTrace}");
    }
}

    [ClientRpc]
    private void SyncBodyValuesClientRpc()
    {
        if (IsServer) return;
        
        if (_snake == null || _snake.Head == null || _bodySegmentComponents.Count == 0) return;
        
        int headValue = _snake.Head.Value; // Head.Value 속성 사용
        float log2 = Mathf.Log(headValue, 2);
        int headPower = (int)Mathf.Round(log2);
        
        Debug.Log($"[{GetType().Name} Client - ID:{NetworkObjectId}] 클라이언트: 헤드 값={headValue}, 2^{headPower} 패턴으로 Body 값 업데이트");
        
        for (int i = 0; i < _bodySegmentComponents.Count; i++)
        {
            if (_bodySegmentComponents[i] == null) continue;
            
            int segmentPower = headPower - i - 1;
            int segmentValue;
            
            if (i == _bodySegmentComponents.Count - 1 || segmentPower < 1)
            {
                segmentValue = 2;
            }
            else
            {
                segmentValue = (int)Mathf.Pow(2, segmentPower);
            }
            
            _bodySegmentComponents[i].SetValue(segmentValue);
            Debug.Log($"[{GetType().Name} Client - ID:{NetworkObjectId}] 클라이언트: Body[{i}] 값={segmentValue}");
        }
    }
    #endregion

    #region Skin Handling

    /// <summary>
    /// NetworkSkinIndex 값이 변경될 때 호출되는 콜백 메서드 (모든 클라이언트에서 실행됨)
    /// </summary>
    private void HandleSkinIndexChanged(int previousIndex, int newIndex)
    {
        Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Player skin index changed from {previousIndex} to {newIndex}. Applying skin.");
        ApplyPlayerSkin(newIndex);
    }

    /// <summary>
    /// 주어진 인덱스에 해당하는 스킨 Material을 플레이어 지렁이 머리와 모든 바디 세그먼트에 적용합니다.
    /// </summary>
    private void ApplyPlayerSkin(int skinIndex)
    {
  
        targetMaterial = playerSkins[skinIndex];


        // 1. Snake Head에 스킨 적용
        ApplyMaterialToComponent(_snake?.Head?.gameObject);

        // 2. 이미 존재하는 모든 몸통 세그먼트에 스킨 적용
        if (_snakeBodyHandler != null && _snakeBodyHandler._bodySegments != null)
        {
            int segmentCount = _snakeBodyHandler.GetBodySegmentCount();
            int appliedCount = 0;

            foreach (var segment in _snakeBodyHandler._bodySegments)
            {
                if (segment != null)
                {
                    if (ApplyMaterialToComponent(segment.gameObject))
                    {
                        appliedCount++;
                    }
                }
            }

            if (segmentCount > 0)
            {
                Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] 총 {appliedCount}/{segmentCount} 바디 세그먼트에 스킨 적용 완료");
            }
        }
        
        // 3. 서버인 경우에만 NetworkList 업데이트
        if (IsServer && _snakeBodyHandler != null)
        {
            // NetworkList 초기화 (기존 항목 제거)
            _segmentSkins.Clear();
            
            // 모든 세그먼트의 스킨 정보 다시 추가
            foreach (var segment in _snakeBodyHandler._bodySegments)
            {
                if (segment != null)
                {
                    NetworkObject netObj = segment.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        _segmentSkins.Add(new SegmentSkinData(netObj.NetworkObjectId, skinIndex));
                    }
                }
            }
            
            Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] NetworkList 업데이트: {_segmentSkins.Count}개 세그먼트 스킨 정보 갱신");
        }
    }

    /// <summary>
    /// 지정된 게임 오브젝트에 스킨을 적용합니다 (SnakeSkin 컴포넌트 필요)
    /// </summary>
    /// <param name="targetObject">스킨을 적용할 게임 오브젝트</param>
    /// <returns>스킨 적용 성공 여부</returns>
    private bool ApplyMaterialToComponent(GameObject targetObject)
    {
        if (targetObject == null || targetMaterial == null)
        {
            return false;
        }

        SnakeSkin skinComponent = targetObject.GetComponent<SnakeSkin>();
        if (skinComponent == null)
        {
            Debug.LogWarning($"[{GetType().Name} ID:{NetworkObjectId}] 게임 오브젝트 {targetObject.name}에서 SnakeSkin 컴포넌트를 찾을 수 없습니다.");
            return false;
        }

        try
        {
            // ChangeTo 메소드를 사용하여 스킨 적용
            skinComponent.ChangeTo(targetMaterial);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} ID:{NetworkObjectId}] 스킨 적용 중 오류 발생: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// NetworkList 변경 감지 핸들러
    /// Late Joiner를 포함한 모든 클라이언트에서 세그먼트 스킨 변경 처리
    /// </summary>
    private void HandleSegmentSkinListChanged(NetworkListEvent<SegmentSkinData> changeEvent)
    {
        // 서버에서는 이미 스킨이 적용되어 있으므로 클라이언트만 처리
        if (IsServer && !IsClient) return;
        
        // 변경 유형에 따라 처리
        if (changeEvent.Type == NetworkListEvent<SegmentSkinData>.EventType.Add || 
            changeEvent.Type == NetworkListEvent<SegmentSkinData>.EventType.Value)
        {
            SegmentSkinData skinData = _segmentSkins[changeEvent.Index];
            bool success = ApplySegmentSkinById(skinData.SegmentId, skinData.SkinIndex);
            
            if (!success && IsClient)
            {
                // 적용 실패 시 지연 적용 시도 (NetworkObject가 아직 동기화되지 않았을 수 있음)
                Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] 세그먼트 {skinData.SegmentId}에 즉시 스킨 적용 실패, 지연 적용 시도");
                StartCoroutine(RetryApplySkinWithDelay(skinData.SegmentId, skinData.SkinIndex));
            }
        }
    }
    
    /// <summary>
    /// 스킨 적용 재시도 코루틴
    /// </summary>
    private IEnumerator RetryApplySkinWithDelay(ulong segmentId, int skinIndex)
    {
        // 3번까지 재시도
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.5f * (i + 1)); // 점점 더 긴 간격으로 재시도
            
            Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] 세그먼트 {segmentId} 스킨 지연 적용 시도 #{i+1}");
            if (ApplySegmentSkinById(segmentId, skinIndex))
            {
                Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] 세그먼트 {segmentId} 스킨 지연 적용 성공");
                yield break; // 성공적으로 적용됨
            }
        }
        
        Debug.LogWarning($"[{GetType().Name} ID:{NetworkObjectId}] 세그먼트 {segmentId} 스킨 지연 적용 최종 실패");
    }

    /// <summary>
    /// NetworkObjectId를 기반으로 세그먼트에 스킨 적용
    /// </summary>
    /// <param name="segmentId">적용할 세그먼트의 NetworkObjectId</param>
    /// <param name="skinIndex">적용할 스킨 인덱스</param>
    /// <param name="logWarning">NetworkObject를 찾지 못했을 때 경고 로그 출력 여부</param>
    /// <returns>스킨 적용 성공 여부</returns>
    private bool ApplySegmentSkinById(ulong segmentId, int skinIndex, bool logWarning = true)
    {
        // 스킨 인덱스 유효성 검사
        if (skinIndex < 0 || skinIndex >= playerSkins.Count)
        {
            Debug.LogError($"[{GetType().Name} ID:{NetworkObjectId}] 유효하지 않은 스킨 인덱스: {skinIndex}");
            return false;
        }
        
        // NetworkObject 찾기
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(segmentId, out NetworkObject segmentObj))
        {
            // 유효성 검사
            if (segmentObj != null)
            {
                SnakeSkin skinComponent = segmentObj.GetComponent<SnakeSkin>();
                if (skinComponent != null)
                {
                    Material skinMaterial = playerSkins[skinIndex];
                    skinComponent.ChangeTo(skinMaterial);
                    Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] NetworkList 변경으로 세그먼트 {segmentId}에 스킨 {skinIndex} 적용");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name} ID:{NetworkObjectId}] 세그먼트 {segmentId}에 SnakeSkin 컴포넌트가 없습니다");
                    return false;
                }
            }
        }
        else if (logWarning)
        {
            // 아직 NetworkObject가 스폰되지 않았을 수 있음 (타이밍 이슈)
            Debug.LogWarning($"[{GetType().Name} ID:{NetworkObjectId}] 세그먼트 ID {segmentId}를 찾을 수 없습니다 (아직 스폰되지 않았을 수 있음)");
        }
        
        return false;
    }

    #endregion

     [ServerRpc]
    public void NotifyHeadValueChangedServerRpc(int previousValue, int newValue)
    {
        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] NotifyHeadValueChangedServerRpc 수신: {previousValue} -> {newValue}");
        
        // 4점 단위로 세그먼트 추가 여부 결정
        int oldSegments = previousValue / 4;
        int newSegments = newValue / 4;
        
        if (newSegments > oldSegments)
        {
            Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 점수 4점 단위 증가 감지, 세그먼트 추가");
            AddBodySegmentOnServer();
            UpdateBodyValuesOnServer(newValue);
        }
    }

    // 속도 변경 감지 콜백 메소드
    private void HandleSnakeSpeedChanged(float previousSpeed, float newSpeed)
    {
        Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Snake 속도 변경 감지: {previousSpeed} → {newSpeed}");
        
        if (_snake != null && _snake.Head != null)
        {
            _snake.Head.ChangeSpeed(newSpeed);
        }
    }

 // 5. NotifyBeefEatenServerRpc에 크기 조절 기능 추가   
    [ServerRpc(RequireOwnership = true)] // 소유권 요구 명시적 설정
    public void NotifyBeefEatenServerRpc(ulong beefNetworkId)
    {
        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] Beef 먹음 알림: BeefID={beefNetworkId}");
        if (!IsServer)
        {
            Debug.LogError($"[{GetType().Name} ID:{NetworkObjectId}] ServerRpc가 서버가 아닌 환경에서 실행됨");
            return;
        }
        
        // 권한 확인
        if (!IsOwner && !NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning($"[{GetType().Name} ID:{NetworkObjectId}] 소유하지 않은 오브젝트에 대한 ServerRpc 호출");
        }
        try
        {
            // 1. 속도 증가 처리 (Beef 고유 효과)
            const float SPEED_INCREMENT = 2f;
            const float MAX_SPEED = 10.0f;
            
            float currentSpeed = NetworkSnakeSpeed.Value;
            float newSpeed = Mathf.Min(currentSpeed + SPEED_INCREMENT, MAX_SPEED);
            NetworkSnakeSpeed.Value = newSpeed;
            
            Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 속도 증가: {currentSpeed} → {newSpeed}");
            
            // 2. 크기 증가 처리 (새로 추가된 기능)
            const float SCALE_INCREMENT = 0.1f;
            const float MAX_SCALE = 2.0f;
            
            float currentScale = NetworkSnakeScale.Value;
            float newScale = Mathf.Min(currentScale + SCALE_INCREMENT, MAX_SCALE);
            NetworkSnakeScale.Value = newScale;
            
            Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 크기 증가: {currentScale} → {newScale}");
            
            // 3. 점수 및 헤드 값 처리 (Apple과 유사한 방식)
            const int BEEF_VALUE = 30; // Beef는 30점
            
            // 현재 값 저장
            int oldHeadValue = _snake._networkHeadValue.Value;
            int oldScore = _networkScore.Value;
            int segmentCount = _snakeBodyHandler.GetBodySegmentCount();
            
            Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 현재 상태: 헤드값={oldHeadValue}, 점수={oldScore}, 세그먼트수={segmentCount}");
            
            // 점수와 헤드 값 업데이트
            _networkScore.Value += BEEF_VALUE;
            _snake._networkHeadValue.Value += BEEF_VALUE;
            
            // 변경 후 값
            int newHeadValue = _snake._networkHeadValue.Value;
            int newScore = _networkScore.Value;
            
            Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 적용 후 상태: 헤드값={newHeadValue}, 점수={newScore}");
            
            // 세그먼트 추가 로직 (점수 4점 단위로)
            if (newScore >= 8 && (newScore / 4 > oldScore / 4))
            {
                Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 점수 4점 단위 증가: {oldScore}→{newScore}, 새 세그먼트 추가");
                AddBodySegmentOnServer();
                UpdateBodyValuesOnServer(newHeadValue);
            }
            
            // BasicGameState 업데이트
            BasicGameState gameState = FindObjectOfType<BasicGameState>();
            if (gameState != null)
            {
                gameState.UpdatePlayerScore(OwnerClientId, "Beef");
            }
            
            // 음식 오브젝트 제거 및 새 음식 생성
            DespawnFoodObject(beefNetworkId, "Beef");
            SpawnNewRandomFood();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] Beef 처리 중 오류: {ex.Message}");
        }
    }


    /// <summary>
    /// Late Joiner를 위한 지연 스킨 적용 코루틴
    /// NetworkObject 동기화가 완료된 후 스킨 적용을 시도합니다.
    /// </summary>
    private IEnumerator ApplyInitialSkinsWithDelay()
    {
        // NetworkObject 동기화 완료 대기
        yield return new WaitForSeconds(0.5f);
        
        // 첫 번째 시도
        Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Late Joiner: 첫 번째 스킨 적용 시도 시작");
        int appliedCount = 0;
        int totalCount = _segmentSkins.Count;
        
        for (int i = 0; i < _segmentSkins.Count; i++)
        {
            SegmentSkinData skinData = _segmentSkins[i];
            if (ApplySegmentSkinById(skinData.SegmentId, skinData.SkinIndex, false))
            {
                appliedCount++;
            }
        }
        
        Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Late Joiner: 첫 번째 시도 결과 - {appliedCount}/{totalCount} 성공");
        
        // 일부 적용 실패시 추가 시도
        if (appliedCount < totalCount)
        {
            // 추가 대기 후 두 번째 시도
            yield return new WaitForSeconds(1.0f);
            
            Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Late Joiner: 두 번째 스킨 적용 시도 시작");
            appliedCount = 0;
            
            for (int i = 0; i < _segmentSkins.Count; i++)
            {
                SegmentSkinData skinData = _segmentSkins[i];
                if (ApplySegmentSkinById(skinData.SegmentId, skinData.SkinIndex, false))
                {
                    appliedCount++;
                }
            }
            
            Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Late Joiner: 두 번째 시도 결과 - {appliedCount}/{totalCount} 성공");
            
            // 세 번째 시도 (마지막)
            if (appliedCount < totalCount)
            {
                yield return new WaitForSeconds(1.5f);
                
                Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Late Joiner: 세 번째(마지막) 스킨 적용 시도 시작");
                appliedCount = 0;
                
                for (int i = 0; i < _segmentSkins.Count; i++)
                {
                    SegmentSkinData skinData = _segmentSkins[i];
                    if (ApplySegmentSkinById(skinData.SegmentId, skinData.SkinIndex, true))
                    {
                        appliedCount++;
                    }
                }
                
                Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] Late Joiner: 세 번째 시도 결과 - {appliedCount}/{totalCount} 성공");
            }
        }
    }
}