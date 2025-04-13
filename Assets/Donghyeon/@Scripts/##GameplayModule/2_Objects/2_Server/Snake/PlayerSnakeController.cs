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

    #region Runtime Variables
    // SnakeBodyHandler 중복 초기화 방지 플래그
    private bool _isBodyHandlerInitialized = false;

    private List<SnakeBodySegment> _bodySegmentComponents = new List<SnakeBodySegment>();
    /// <summary>스네이크의 이동 기록을 저장하는 큐</summary>
   
    #endregion
    
    #region Network Variables
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
        // 서버에서만 NetworkVariable 초기화 및 권한 있는 SnakeBodyHandler 초기화
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

        // 중복 호출 방지를 위해 값 변경 확인 (선택 사항)
        if (_networkPlayerId.Value != playerId) _networkPlayerId.Value = playerId;
        if (_networkScore.Value != initialScore) _networkScore.Value = initialScore;
        if (_networkBodyCount.Value != initialSize) _networkBodyCount.Value = initialSize;

        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] NetworkVariables Set: PlayerID={_networkPlayerId.Value}, Score={_networkScore.Value}, BodyCount={_networkBodyCount.Value}");

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
        if (_snake == null || _snake.Head == null) return;

        // 서버 로직
        if (IsServer)
        {
            // 서버에서 위치 업데이트
            if (_snakeBodyHandler != null)
            {
                _snakeBodyHandler.UpdateBodySegmentsPositions();
                // 모든 클라이언트에게 현재 상태 전송
                SyncSnakeStateClientRpc(
                    _snake.Head.transform.position,
                    _snake.Head.transform.rotation
                );
            }
        }

        // 클라이언트의 입력 처리 로직
        if (IsOwner)
        {
            Vector2 moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                HandleMoveDirChanged(moveDirection);
            }
        }
    }

    [ClientRpc]
    private void SyncSnakeStateClientRpc(Vector3 headPosition, Quaternion headRotation)
    {
        if (IsServer) return;

        if (_snake != null && _snake.Head != null)
        {
            // 클라이언트의 헤드 위치 업데이트
            _snake.Head.transform.position = headPosition;
            _snake.Head.transform.rotation = headRotation;

            // 바디 세그먼트 업데이트
            if (_snakeBodyHandler != null)
            {
                _snakeBodyHandler.UpdateBodySegmentsPositions();
            }
        }
    }


 
    [ServerRpc]
    public void NotifyAppleEatenServerRpc(int appleValue, ulong appleNetworkId)
    {
        // 이 RPC는 소유자 클라이언트가 호출
        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] NotifyAppleEatenServerRpc 수신: Value=+{appleValue}, AppleID={appleNetworkId}");
        
        _networkScore.Value += appleValue; 
        _snake._networkHeadValue.Value += appleValue; // Snake의 _networkHeadValue 사용
        try 
        {
            // 호스트가 먹은 사과인 경우
            if (IsHost || IsServer)
            {
                // SpawnedObjects에서 사과 찾기
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(appleNetworkId, out NetworkObject appleNetObj))
                {
                    if (appleNetObj.TryGetComponent<Apple>(out var apple))
                    {
                        Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 서버에서 사과 제거: AppleID={appleNetworkId}");
                        _objectManager.Despawn(apple);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] 사과 제거 중 오류: {ex.Message}");
        }
        
        // 새 사과 생성 (어떤 경우든 항상 새 사과 생성)
        try
        {
            Vector3 randomPosition = Util.GetRandomPosition();
            randomPosition.y = 0f;
            var newApple = _objectManager.Spawn<BaseObject>(randomPosition, Quaternion.identity, prefabName: "Apple");
            Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] 새 사과 생성됨: 위치={randomPosition}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Server ID:{NetworkObjectId}] 새 사과 생성 중 오류: {ex.Message}");
        }
    }


    [ServerRpc]
    public void NotifyHeadValueChangedServerRpc(int previousValue, int newValue)
    {
        // 이 RPC는 서버 내부 로직이나 다른 이벤트에 의해 호출될 수 있음
         Debug.Log($"[{GetType().Name} Server ID:{NetworkObjectId}] NotifyHeadValueChangedServerRpc 수신: {previousValue} -> {newValue}");
        AddBodySegmentOnServer();
        UpdateBodyValuesOnServer(newValue);
    }

    #region Movement and Body Management

    private void HandleMoveDirChanged(Vector2 moveDirection)
    {
        if (IsOwner)
        {
            UpdateMoveDirectionServerRpc(moveDirection);
        }
    }


    [ServerRpc]
    private void UpdateMoveDirectionServerRpc(Vector2 moveDirection)
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Vector3 currentPosition = _snake.Head.transform.position;
            Vector3 targetDirection = new Vector3(moveDirection.x, 0, moveDirection.y).normalized;
            Vector3 targetPosition = currentPosition + targetDirection;
            _snake.Head.LookAt(targetPosition);
            Vector3 newForwardDirection = _snake.Head.transform.forward;
            UpdateDirectionClientRpc(newForwardDirection);
        }
    }


    [ClientRpc]
    private void UpdateDirectionClientRpc(Vector3 direction)
    {
        if (IsServer) return;
        _snake.SetHeadTargetDirection(direction);
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
            // 서버에서 세그먼트 생성
            BaseObject segment = _objectManager.Spawn<BaseObject>(spawnPosition, spawnRotation, prefabName: "Body Detail");
            SnakeBodySegment segmentComponent = segment.GetComponent<SnakeBodySegment>();

            if (segmentComponent != null)
            {
                int segmentValue = _snakeBodyHandler.CalculateSegmentValue();
                segmentComponent.SetValue(segmentValue);
            }
            
            segment.transform.SetParent(_snake.transform, true);
            
            _snakeBodyHandler.AddBodySegment(segment, segmentComponent);
            _networkBodyCount.Value++; // 네트워크 변수 업데이트
                
                
            Debug.Log($"[{GetType().Name} Server - ID:{NetworkObjectId}] 세그먼트 추가됨: 위치={spawnPosition}, 회전={spawnRotation}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name} Server - ID:{NetworkObjectId}] 세그먼트 생성 중 오류: {ex.Message}\n{ex.StackTrace}");
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
                BaseObject bodySegment = _snakeBodyHandler.GetBodySegment(i);
                if (bodySegment != null)
                {
                    var segment = bodySegment.GetComponent<SnakeBodySegment>();
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

}