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
using UnityEngine.Rendering; // Visual Effect Graph 색상 변경을 위해 추가
// using Jaehyeon.Scripts; // 네임스페이스 불확실하여 일단 주석 처리


public class PlayerSnakeController : NetworkBehaviour
{
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
        // 초기 색상 적용 (스폰 시점의 값으로) -> 초기 스킨 적용으로 변경
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
        
        // 속도 변경 감지 콜백 해제
        if (NetworkSnakeSpeed != null)
        {
            NetworkSnakeSpeed.OnValueChanged -= HandleSnakeSpeedChanged;
        }

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
        // 음식 타입 확인 (양수: Apple, 음수: Candy)
        string foodType = foodValue > 0 ? "Apple" : "Candy";
        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] NotifyFoodEatenServerRpc 수신: Value={foodValue}, FoodID={foodNetworkId}, Type={foodType}");
        
        // 현재 값 저장
        int oldHeadValue = _snake._networkHeadValue.Value;
        int oldScore = _networkScore.Value;
        int segmentCount = _snakeBodyHandler.GetBodySegmentCount();
        
        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 현재 상태: 헤드값={oldHeadValue}, 점수={oldScore}, 세그먼트수={segmentCount}");
        
        // Candy 처리 - 몸통이 없는 경우 적용하지 않음
        if (foodValue < 0 && segmentCount <= 0)
        {
            Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] Candy 효과 무시: 몸통이 없어 적용되지 않음");
            // 음식 오브젝트만 제거하고 끝냄
            DespawnFoodObject(foodNetworkId, foodType);
            return;
        }
        
        // 점수와 헤드 값 업데이트
        _networkScore.Value += foodValue;
        _snake._networkHeadValue.Value += foodValue;
        
        // 변경 후 값
        int newHeadValue = _snake._networkHeadValue.Value;
        int newScore = _networkScore.Value;
        
        // 최소값 보호 (2 미만으로 내려가지 않도록)
        if (newHeadValue < 2)
        {
            newHeadValue = 2;
            _snake._networkHeadValue.Value = 2;
            newScore = 2;
            _networkScore.Value = 2;
            Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 헤드 값이 최소값(2)보다 작아져 2로 조정됨");
        }
        
        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 적용 후 상태: 헤드값={newHeadValue}, 점수={newScore}");
        
        if (foodValue > 0) // Apple 처리 (+2 또는 +4)
        {
            // 매 4점마다 몸통 세그먼트 추가
            // 예: 2→4→6→8(+세그먼트)→10→12→14→16(+세그먼트)
            if (newScore >= 8 && (newScore / 4 > oldScore / 4))
            {
                Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 점수 4점 단위 증가: {oldScore}→{newScore}, 새 세그먼트 추가");
                AddBodySegmentOnServer();
                UpdateBodyValuesOnServer(newHeadValue);
            }
        }
        else if (foodValue < 0) // Candy 처리 (-4)
        {
            // Candy가 -4 값이라면 즉시 세그먼트 하나 제거
            if (segmentCount > 0)
            {
                Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] Candy 먹음: 세그먼트 하나 제거 (남은 세그먼트: {segmentCount-1})");
                RemoveLastBodySegmentOnServer();
            }
            else
            {
                Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] Candy 먹음: 제거할 세그먼트 없음");
            }
        }
        
        // 음식 오브젝트 제거
        DespawnFoodObject(foodNetworkId, foodType);
        
        // 새 음식 생성
        SpawnNewRandomFood();
    }
    
    /// <summary>
    /// 음식 오브젝트 디스폰 처리
    /// </summary>
    private void DespawnFoodObject(ulong foodNetworkId, string foodType)
    {
        try 
        {
            if (IsHost || IsServer)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(foodNetworkId, out NetworkObject foodNetObj))
                {
                    if (foodNetObj != null)
                    {
                        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 서버에서 {foodType} 제거: FoodID={foodNetworkId}");
                        _objectManager.Despawn(foodNetObj.GetComponent<BaseObject>());
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] {foodType} 제거 중 오류: {ex.Message}");
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
        
        // SnakeBodyHandler 확인
        if (_snakeBodyHandler == null)
        {
            Debug.LogError($"[{GetType().Name} Client ID:{NetworkObjectId}] SnakeBodyHandler가 null입니다!");
            return;
        }
        
        // 현재 세그먼트 수 기록
        int beforeCount = _snakeBodyHandler.GetBodySegmentCount();
        
        // 클라이언트에서 세그먼트 목록 업데이트
        try
        {
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

            // 5. 소유권 클라이언트에게 이전
            networkObject.ChangeOwnership(OwnerClientId);
            Debug.Log($"[{GetType().Name} Server - ID:{NetworkObjectId}] 세그먼트 소유권 이전 -> Client {OwnerClientId}");

            // 6. 모든 클라이언트에게 스폰 알림 (PlayerController ID, Segment ID, 그리고 스킨 인덱스 전달)
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
        if (skinIndex < 0 || skinIndex >= playerSkins.Count)
        {
            Debug.LogError($"[{GetType().Name} ID:{NetworkObjectId}] Player Skins 리스트의 인덱스 {skinIndex}가 범위를 벗어났습니다. 리스트 크기: {playerSkins.Count}");
            return;
        }

        // 타겟 Material 저장
        targetMaterial = playerSkins[skinIndex];
        if (targetMaterial == null)
        {
            Debug.LogError($"[{GetType().Name} ID:{NetworkObjectId}] Player Skins 리스트의 인덱스 {skinIndex}에 할당된 Material이 null입니다.");
            return;
        }

        Debug.Log($"[{GetType().Name} ID:{NetworkObjectId}] 스킨 적용 시작: 인덱스={skinIndex}, Material={targetMaterial.name}");

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

    [ServerRpc]
    public void NotifyBeefEatenServerRpc(ulong beefNetworkId)
    {
        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] Beef 먹음 알림: BeefID={beefNetworkId}");
        
        try
        {
            // 속도 증가 처리
            const float SPEED_INCREMENT = 1.0f;
            const float MAX_SPEED = 15.0f;
            
            // 현재 속도와 새 속도 계산
            float currentSpeed = NetworkSnakeSpeed.Value;
            float newSpeed = Mathf.Min(currentSpeed + SPEED_INCREMENT, MAX_SPEED);
            
            // NetworkVariable 값 변경 (자동으로 모든 클라이언트에 동기화됨)
            NetworkSnakeSpeed.Value = newSpeed;
            
            Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 속도 증가: {currentSpeed} → {newSpeed}");
            
            // Beef 오브젝트 제거
            DespawnFoodObject(beefNetworkId, "Beef");
            
            // 새 음식 생성
            SpawnNewRandomFood();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] Beef 처리 중 오류: {ex.Message}");
        }
    }
}