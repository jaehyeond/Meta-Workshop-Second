using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using System.Linq;
using Unity.Assets.Scripts.Resource;

/// <summary>
/// 플레이어 스네이크의 네트워크 동기화와 제어를 담당하는 메인 컨트롤러
/// NetworkBehaviour를 상속받아 네트워크 기능을 구현
/// </summary>
public class PlayerSnakeController : NetworkBehaviour
{
    #region Dependencies
    /// <summary>게임 매니저 - 게임 상태 및 이벤트 관리</summary>
    private GameManager _gameManager;
    /// <summary>사과 매니저 - 사과 생성 및 충돌 처리</summary>
    private AppleManager _appleManager;
    /// <summary>리소스 매니저 - 프리팹 및 에셋 로드</summary>
    private ResourceManager _resourceManager;
    /// <summary>스네이크 몸체 핸들러 - 몸체 세그먼트 관리</summary>
    private SnakeBodyHandler _snakeBodyHandler;
    #endregion

    #region Settings
    [Header("Movement Settings")]
    /// <summary>스네이크의 초기 이동 속도</summary>
    [SerializeField] private float _initialSnakeSpeed = 5f;
    /// <summary>스네이크의 기본 이동 속도</summary>
    [SerializeField] private float _movementSpeed = 3f;
    /// <summary>스네이크의 회전 속도 (도/초)</summary>
    [SerializeField] private float _rotationSpeed = 180f;
    /// <summary>이동 시 부드러운 움직임을 위한 보간 계수</summary>
    [SerializeField] private float _movementSmoothing = 0.05f;
    #endregion

    #region Core Components
    [Header("Core Components")]
    /// <summary>실제 스네이크 로직을 담당하는 Snake 컴포넌트</summary>
    [SerializeField] public Snake _snake;
    #endregion

    #region Runtime Variables
    /// <summary>스네이크 몸체 세그먼트 GameObject 리스트</summary>
    private List<GameObject> _bodySegments = new List<GameObject>();
    /// <summary>스네이크 몸체 세그먼트 컴포넌트 리스트</summary>
    private List<SnakeBodySegment> _bodySegmentComponents = new List<SnakeBodySegment>();
    /// <summary>스네이크의 이동 기록을 저장하는 큐</summary>
    private Queue<Vector3> _moveHistory = new Queue<Vector3>();
    /// <summary>첫 번째 세그먼트의 속도 벡터 (SmoothDamp용)</summary>
    private Vector3 _firstSegmentVelocity = Vector3.zero;
    /// <summary>각 세그먼트의 속도 벡터 리스트 (SmoothDamp용)</summary>
    private List<Vector3> _segmentVelocities = new List<Vector3>();
    #endregion
    
    #region Network Variables
    /// <summary>네트워크를 통해 동기화되는 플레이어 점수</summary>
    private readonly NetworkVariable<int> _networkScore = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>네트워크를 통해 동기화되는 스네이크 크기</summary>
    private readonly NetworkVariable<int> _networkSize = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>네트워크를 통해 동기화되는 플레이어 ID</summary>
    private readonly NetworkVariable<NetworkString> _networkPlayerId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>네트워크를 통해 동기화되는 스네이크 헤드 값</summary>
    public NetworkVariable<int> _networkHeadValue = new(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// 네트워크 스폰 시 호출되는 메서드
    /// 서버/클라이언트/오너에 따라 다른 초기화 수행
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            InitializeServerState();
        }

        if (IsClient)
        {
            SubscribeToNetworkVariables();
        }

        if (IsOwner)
        {
            StartCoroutine(FollowPlayerWithCamera());
            var lifetimeScope = FindObjectOfType<LifetimeScope>();
            if (lifetimeScope != null)
            {
                _gameManager = lifetimeScope.Container.Resolve<GameManager>();
                _appleManager = lifetimeScope.Container.Resolve<AppleManager>();
                _resourceManager = lifetimeScope.Container.Resolve<ResourceManager>();
                
                if (_gameManager != null)
                {
                    _gameManager.OnMoveDirChanged += HandleMoveDirChanged;
                    Debug.Log($"[{GetType().Name}] GameManager 이벤트 구독 완료.");
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

        if (IsClient)
        {
            UnsubscribeFromNetworkVariables();
        }

        if (IsOwner)
        {
            _gameManager.OnMoveDirChanged -= HandleMoveDirChanged;
        }
    }
    #endregion

    #region Initialization
    /// <summary>
    /// 서버 상태 초기화
    /// 스네이크 데이터 설정 및 컴포넌트 초기화
    /// </summary>
    private void InitializeServerState()
    {
        Debug.Log($"[{GetType().Name}] 서버: 스네이크(OwnerClientId: {OwnerClientId}) 스폰됨. 초기 데이터 설정.");

        // SnakeBodyHandler 초기화
        _snakeBodyHandler = GetComponent<SnakeBodyHandler>();
        if (_snakeBodyHandler == null)
        {
            _snakeBodyHandler = gameObject.AddComponent<SnakeBodyHandler>();
        }
        _snakeBodyHandler.Initialize(_snake);
        
        // 플레이어 데이터 로드
        string playerId = "Player_" + OwnerClientId;
        int initialScore = 0;
        int initialSize = 1;
        int initialHeadValue = 2;

        _networkPlayerId.Value = playerId;
        _networkScore.Value = initialScore;
        _networkSize.Value = initialSize;
        _networkHeadValue.Value = initialHeadValue;

        // 스네이크 헤드 속도 초기화
        if (_snake != null && _snake.Head != null)
        {
            _snake.Head.Construct(_initialSnakeSpeed);
            _snake.Head.SetValue(2);
            Debug.Log($"[{GetType().Name}] 서버: 스네이크 헤드 속도 초기화 완료 ({_initialSnakeSpeed})");
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] 서버: Snake 또는 SnakeHead 참조가 null입니다!");
        }
    }

    /// <summary>
    /// 카메라가 플레이어를 따라가도록 하는 코루틴
    /// 필요한 컴포넌트가 준비될 때까지 대기
    /// </summary>
    private IEnumerator FollowPlayerWithCamera()
    {
        Debug.Log($"[{GetType().Name}] 카메라 추적 코루틴 시작. CameraProvider 및 Snake Head 대기.");

        float waitTime = 0f;
        const float maxWaitTime = 5f;

        while ((CameraProvider.Instance == null || _snake == null || _snake.Head == null) && waitTime < maxWaitTime)
        {
            yield return null;
            waitTime += Time.deltaTime;
        }

        if (CameraProvider.Instance != null && _snake != null && _snake.Head != null)
        {
            try
            {
                CameraProvider.Instance.Follow(_snake.Head.transform); 
                Debug.Log($"[{GetType().Name}] 카메라가 스네이크 헤드({_snake.Head.name})를 따라갑니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] CameraProvider.Follow 호출 중 오류: {ex.Message}\\n{ex.StackTrace}");
            }
        }
        else
        {
            if(CameraProvider.Instance == null) Debug.LogError($"[{GetType().Name}] CameraProvider.Instance를 찾지 못했습니다.");
            if(_snake == null) Debug.LogError($"[{GetType().Name}] _snake 참조가 null입니다.");
            else if (_snake.Head == null) Debug.LogError($"[{GetType().Name}] _snake.Head 참조가 null입니다.");
            
            Debug.LogError($"[{GetType().Name}] 카메라 추적 설정 실패 (대기 시간 초과 또는 컴포넌트 누락).");
        }
    }
    #endregion

    #region Network Variable Handling
    /// <summary>
    /// 네트워크 변수 변경 이벤트 구독
    /// </summary>
    private void SubscribeToNetworkVariables()
    {
        Debug.Log($"[{GetType().Name}] 클라이언트: NetworkVariable 변경 구독 시작");
        _networkHeadValue.OnValueChanged += OnHeadValueChanged;
    }

    /// <summary>
    /// 네트워크 변수 변경 이벤트 구독 해지
    /// </summary>
    private void UnsubscribeFromNetworkVariables()
    {
        Debug.Log($"[{GetType().Name}] 클라이언트: NetworkVariable 변경 구독 해지 시작");
        if (_networkHeadValue != null) _networkHeadValue.OnValueChanged -= OnHeadValueChanged;
    }

    /// <summary>
    /// 헤드 값 변경 시 호출되는 콜백
    /// 서버에서 세그먼트 추가 및 값 업데이트 처리
    /// </summary>
    private void OnHeadValueChanged(int previousValue, int newValue)
    {
        Debug.Log($"[{GetType().Name}] 헤드 값 변경 감지: {previousValue} -> {newValue}");
        UpdateHeadValueDisplay(newValue);
        
        if (IsServer)
        {
            float log2Value = Mathf.Log(newValue, 2);
            if (Mathf.Approximately(log2Value, Mathf.Round(log2Value)))
            {
                AddBodySegmentServerRpc();
                _networkScore.Value += newValue;
            }
            
            UpdateBodyValues(newValue);
        }
    }
    #endregion

    #region Movement and Body Management
    /// <summary>
    /// 고정 업데이트 - 물리 기반 업데이트
    /// 서버에서만 몸체 세그먼트 위치 업데이트
    /// </summary>
    private void FixedUpdate()
    {
        if (!IsServer) return;
        
        if (_snake != null && _snake.Head != null)
        {
            _snakeBodyHandler.UpdateBodySegmentsPositions();
        }
    }

    /// <summary>
    /// 헤드 값 표시 업데이트
    /// </summary>
    private void UpdateHeadValueDisplay(int value)
    {
        if (_snake != null && _snake.Head != null)
        {
            _snake.Head.SetValue(value);
        }
    }

    /// <summary>
    /// 이동 방향 변경 이벤트 핸들러
    /// 오너 클라이언트에서만 서버로 RPC 전송
    /// </summary>
    private void HandleMoveDirChanged(Vector2 moveDirection)
    {
        if (!IsOwner) return;
        UpdateMoveDirectionServerRpc(moveDirection);
    }

    /// <summary>
    /// 서버 RPC: 이동 방향 업데이트
    /// 서버에서 스네이크 방향 변경 및 클라이언트 동기화
    /// </summary>
    [ServerRpc]
    private void UpdateMoveDirectionServerRpc(Vector2 moveDirection)
    {
        if (!IsServer) return;

        if (_snake != null && _snake.Head != null)
        {
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Vector3 currentPosition = _snake.Head.transform.position;
                Vector3 targetDirection = new Vector3(moveDirection.x, 0, moveDirection.y).normalized;
                Vector3 targetPosition = currentPosition + targetDirection;
                _snake.Head.LookAt(targetPosition);

                Vector3 moveForward = _snake.Head.transform.forward;
                UpdateDirectionClientRpc(moveForward);
            }
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] 서버: RPC 실행 중 Snake 또는 SnakeHead 참조가 null입니다!");
        }
    }

    /// <summary>
    /// 클라이언트 RPC: 방향 업데이트
    /// 서버에서 설정한 방향으로 클라이언트 동기화
    /// </summary>
    [ClientRpc]
    private void UpdateDirectionClientRpc(Vector3 direction)
    {
        if (IsServer) return;

        if (_snake != null && _snake.Head != null)
        {
            _snake.Head.SetTargetDirectionFromServer(direction);
        }
    }
    #endregion

    #region Body Segment Management
    /// <summary>
    /// 서버 RPC: 새 몸체 세그먼트 추가
    /// 서버에서 세그먼트 생성 및 네트워크 동기화
    /// </summary>
    [ServerRpc]
    private void AddBodySegmentServerRpc()
    {
        if (!IsServer) return;

        Debug.Log($"[{GetType().Name}] 서버: 새 Body 세그먼트 생성 시작");

        if (_resourceManager == null)
        {
            var lifetimeScope = FindObjectOfType<LifetimeScope>();
            if (lifetimeScope != null)
            {
                _resourceManager = lifetimeScope.Container.Resolve<ResourceManager>();
            }
            
            if (_resourceManager == null)
            {
                Debug.LogError($"[{GetType().Name}] ResourceManager를 찾을 수 없습니다!");
                return;
            }
        }

        GameObject bodySegmentPrefab = _resourceManager.Load<GameObject>("Body Detail");
        if (bodySegmentPrefab == null)
        {
            Debug.LogError($"[{GetType().Name}] Body Detail 프리팹을 로드할 수 없습니다!");
            return;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        
        if (_snakeBodyHandler.GetBodySegmentCount() > 0)
        {
            GameObject lastSegment = _snakeBodyHandler.GetLastSegment();
            spawnPosition = lastSegment.transform.position - lastSegment.transform.forward * _snakeBodyHandler.GetSegmentSpacing();
            spawnRotation = lastSegment.transform.rotation;
        }
        else
        {
            spawnPosition = _snake.Head.transform.position - _snake.Head.transform.forward * _snakeBodyHandler.GetSegmentSpacing();
            spawnRotation = _snake.Head.transform.rotation;
        }

        try
        {
            GameObject segment = Instantiate(bodySegmentPrefab, spawnPosition, spawnRotation);
            SnakeBodySegment segmentComponent = segment.GetComponent<SnakeBodySegment>();
            
            if (segmentComponent != null)
            {
                int segmentValue = CalculateSegmentValue();
                segmentComponent.SetValue(segmentValue);
            }
            
            NetworkObject networkObject = segment.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                segment.transform.SetParent(transform, true);
                
                _snakeBodyHandler.AddBodySegment(segment, segmentComponent);
                _networkScore.Value += segmentComponent?.Value ?? 2;
                
                NotifySegmentAddedClientRpc(_snakeBodyHandler.GetBodySegmentCount() - 1, segmentComponent?.Value ?? 2);
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] 세그먼트에 NetworkObject가 없습니다!");
                Destroy(segment);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] 세그먼트 생성 중 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 세그먼트 값 계산
    /// 헤드 값과 세그먼트 위치에 따라 2의 제곱수 패턴으로 계산
    /// </summary>
    private int CalculateSegmentValue()
    {
        int headValue = _networkHeadValue.Value;
        float log2HeadValue = Mathf.Log(headValue, 2);
        int headPower = (int)Mathf.Round(log2HeadValue);
        
        int segmentPower = headPower - _snakeBodyHandler.GetBodySegmentCount() - 1;
        return segmentPower > 0 ? (int)Mathf.Pow(2, segmentPower) : 2;
    }

    /// <summary>
    /// 클라이언트 RPC: 새 세그먼트 추가 알림
    /// 클라이언트에서 새 세그먼트 값 설정
    /// </summary>
    [ClientRpc]
    private void NotifySegmentAddedClientRpc(int segmentIndex, int segmentValue)
    {
        if (IsServer) return;
        
        Debug.Log($"[{GetType().Name}] 클라이언트: 새 세그먼트 추가 알림 (인덱스: {segmentIndex}, 값: {segmentValue})");
        
        if (_bodySegmentComponents.Count > segmentIndex)
        {
            var segmentComponent = _bodySegmentComponents[segmentIndex];
            if (segmentComponent != null)
            {
                segmentComponent.SetValue(segmentValue);
                Debug.Log($"[{GetType().Name}] 클라이언트: 세그먼트[{segmentIndex}] 값 설정됨: {segmentValue}");
            }
        }
    }

    /// <summary>
    /// 몸체 세그먼트 값 업데이트
    /// 헤드 값 변경 시 모든 세그먼트의 값 재계산
    /// </summary>
    private void UpdateBodyValues(int headValue)
    {
        if (_bodySegmentComponents == null || _bodySegmentComponents.Count == 0) return;

        try
        {
            float log2HeadValue = Mathf.Log(headValue, 2);
            int headPower = (int)Mathf.Round(log2HeadValue);
            
            Debug.Log($"[{GetType().Name}] 머리 값: {headValue}, 지수: {headPower}, 세그먼트 수: {_bodySegmentComponents.Count}");

            for (int i = 0; i < _bodySegmentComponents.Count; i++)
            {
                SnakeBodySegment segment = _bodySegmentComponents[i];
                if (segment == null) continue;

                int segmentPower;
                int segmentValue;
                
                if (i == _bodySegmentComponents.Count - 1)
                {
                    segmentValue = 2;
                }
                else if (i == 0)
                {
                    segmentPower = headPower - 1;
                    segmentValue = Mathf.Max(2, (int)Mathf.Pow(2, segmentPower));
                }
                else
                {
                    segmentPower = headPower - i - 1;
                    segmentValue = Mathf.Max(2, (int)Mathf.Pow(2, segmentPower));
                }

                segment.SetValue(segmentValue);
                Debug.Log($"[{GetType().Name}] 세그먼트 #{i + 1} 값 설정: {segmentValue}");
            }

            SyncBodyValuesClientRpc();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] UpdateBodyValues 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 클라이언트 RPC: 몸체 값 동기화
    /// 서버에서 계산된 값을 클라이언트에 동기화
    /// </summary>
    [ClientRpc]
    private void SyncBodyValuesClientRpc()
    {
        if (IsServer) return;
        
        if (_snake == null || _snake.Head == null || _bodySegmentComponents.Count == 0) return;
        
        int headValue = _snake.Head.Value;
        float log2 = Mathf.Log(headValue, 2);
        int headPower = (int)Mathf.Round(log2);
        
        Debug.Log($"[{GetType().Name}] 클라이언트: 헤드 값={headValue}, 2^{headPower} 패턴으로 Body 값 업데이트");
        
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
            Debug.Log($"[{GetType().Name}] 클라이언트: Body[{i}] 값={segmentValue}");
        }
    }
    #endregion
       // private void Die()
    // {
    //     if (!IsServer) return;

    //     Debug.LogWarning($"[{GetType().Name}] 스네이크 사망 처리 (서버): OwnerClientId={OwnerClientId}");

    //     NetworkObject networkObject = GetComponent<NetworkObject>();
    //     if (networkObject != null)
    //     {
    //         networkObject.Despawn(true);
    //     }
    //     else
    //     {
    //         Destroy(gameObject);
    //     }
    // }
}