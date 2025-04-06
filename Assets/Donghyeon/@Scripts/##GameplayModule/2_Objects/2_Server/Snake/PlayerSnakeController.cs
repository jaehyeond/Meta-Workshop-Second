using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using System.Linq;
using Unity.Assets.Scripts.Resource;

// PlayerSnakeController가 NetworkBehaviour를 상속하는지 확인하세요.
public class PlayerSnakeController : NetworkBehaviour
{
    #region Dependencies
    // GameManager는 Owner Client에서만 필요하므로 OnNetworkSpawn에서 Resolve
    private GameManager _gameManager;
    [Inject] private ResourceManager _resourceManager;
    #endregion

    #region Settings
    [Header("Movement Settings")]
    [SerializeField] private float _initialSnakeSpeed = 5f; // Inspector에서 초기 속도 설정
    
    [Header("2048 Snake Settings")]
    [SerializeField] private GameObject _bodySegmentPrefab; // Body 세그먼트 프리팹
    [SerializeField] private int _valueIncrement = 1; // 값 증가량
    [SerializeField] private float _segmentSpacing = 1f; // 세그먼트 간 간격
    #endregion

    #region Core Components
    [Header("Core Components")]
    [SerializeField] private Snake _snake; // 실제 스네이크 로직 담당
    #endregion

    #region Runtime Variables
    private List<GameObject> _bodySegments = new List<GameObject>();
    private List<SnakeBodySegment> _bodySegmentComponents = new List<SnakeBodySegment>();
    private Queue<Vector3> _moveHistory = new Queue<Vector3>(); // 이동 기록을 저장
    #endregion
    
    #region Network Variables
    // 서버 -> 클라이언트로 동기화될 변수들
    // 권한: 서버만 쓰기 가능, 모든 클라이언트 읽기 가능
    private readonly NetworkVariable<int> _networkScore = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _networkSize = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<NetworkString> _networkPlayerId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _networkHeadValue = new(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    #endregion

    #region Unity Lifecycle
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
            InitializeOwnerClient();
        }
        else if (IsClient) // Owner가 아닌 클라이언트
        {
            InitializeRemoteClient();
        }

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsClient)
        {
            UnsubscribeFromNetworkVariables();
        }

        if (IsOwner)
        {
            UnsubscribeFromGameManagerEvents();
        }

        // 서버 측 정리 로직 (필요 시)
        // if (IsServer) { /* ... */ }

        Debug.Log($"[{GetType().Name}] OnNetworkDespawn 호출됨! NetworkObjectId: {NetworkObjectId}, OwnerClientId: {OwnerClientId}");
    }
    
    private void Update()
    {
        if (IsServer)
        {
            // 매 프레임마다 헤드 위치 기록 (서버에서만)
            StorePositionHistory();
            
            // 서버에서만 세그먼트 위치 업데이트
            UpdateBodySegmentsPositions();
        }
    }
    #endregion

    #region Initialization
    private void InitializeServerState()
    {
        Debug.Log($"[{GetType().Name}] 서버: 스네이크(OwnerClientId: {OwnerClientId}) 스폰됨. 초기 데이터 설정.");

        // 스네이크 헤드 속도 초기화
        if (_snake != null && _snake.Head != null)
        {
            _snake.Head.Construct(_initialSnakeSpeed);
            _snake.Head.SetValue(2); // 초기 값 설정
            Debug.Log($"[{GetType().Name}] 서버: 스네이크 헤드 속도 초기화 완료 ({_initialSnakeSpeed})");
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] 서버: Snake 또는 SnakeHead 참조가 null입니다!");
        }

        // 플레이어 데이터 로드 (예시)
        string playerId = "Player_" + OwnerClientId;
        int initialScore = 0;
        int initialSize = 1;
        int initialHeadValue = 2;

        // NetworkVariable 값 설정 (클라이언트로 동기화됨)
        _networkPlayerId.Value = playerId;
        _networkScore.Value = initialScore;
        _networkSize.Value = initialSize;
        _networkHeadValue.Value = initialHeadValue;

        // 서버 측 다른 시스템 업데이트 (예: 리더보드)
        // _leaderboardService?.UpdateLeader(playerId, initialScore);
    }

    private void InitializeOwnerClient()
    {
        Debug.Log($"[{GetType().Name}] Owner 클라이언트 초기화 시작.");
        StartCoroutine(FollowPlayerWithCamera());
        ResolveAndSubscribeToGameManager();
    }

    private void InitializeRemoteClient()
    {
        Debug.Log($"[{GetType().Name}] 원격 클라이언트 초기화 시작 (다른 플레이어의 스네이크).");
        // 원격 플레이어 관련 설정 (예: 입력 비활성화, 시각적 보간 활성화)
        // GetComponent<PlayerInputHandler>()?.enabled = false;
        // GetComponent<RemoteSnakeVisualSmoother>()?.enabled = true;
    }

    private IEnumerator FollowPlayerWithCamera()
    {
        Debug.Log($"[{GetType().Name}] 카메라 추적 코루틴 시작. CameraProvider 대기.");

        float waitTime = 0f;
        const float maxWaitTime = 5f; // 최대 대기 시간

        while (CameraProvider.Instance == null && waitTime < maxWaitTime)
        {
            yield return null;
            waitTime += Time.deltaTime;
        }

        if (CameraProvider.Instance != null)
        {
            try
            {
                CameraProvider.Instance.Follow(this.transform);
                Debug.Log($"[{GetType().Name}] 카메라가 플레이어를 따라갑니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] CameraProvider.Follow 호출 중 오류: {ex.Message}\n{ex.StackTrace}");
            }
        }
        else
        {
             Debug.LogWarning($"[{GetType().Name}] CameraProvider.Instance를 {maxWaitTime}초 내에 찾지 못했습니다.");
        }
    }

    private void ResolveAndSubscribeToGameManager()
    {
        _gameManager = FindObjectOfType<LifetimeScope>()?.Container.Resolve<GameManager>();
        if (_gameManager != null)
        {
            _gameManager.OnMoveDirChanged += HandleMoveDirChanged;
            Debug.Log($"[{GetType().Name}] GameManager 이벤트 구독 완료.");
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] GameManager를 찾거나 Resolve할 수 없습니다!");
        }
    }
    #endregion

    #region Network Variable Handling
    private void SubscribeToNetworkVariables()
    {
        Debug.Log($"[{GetType().Name}] 클라이언트: NetworkVariable 변경 구독 시작");
        _networkScore.OnValueChanged += OnScoreChanged;
        _networkSize.OnValueChanged += OnSizeChanged;
        _networkPlayerId.OnValueChanged += OnPlayerIdChanged;
        _networkHeadValue.OnValueChanged += OnHeadValueChanged;
    }

    private void UnsubscribeFromNetworkVariables()
    {
        Debug.Log($"[{GetType().Name}] 클라이언트: NetworkVariable 변경 구독 해지 시작");
        // Null check for safety during teardown
        if (_networkScore != null) _networkScore.OnValueChanged -= OnScoreChanged;
        if (_networkSize != null) _networkSize.OnValueChanged -= OnSizeChanged;
        if (_networkPlayerId != null) _networkPlayerId.OnValueChanged -= OnPlayerIdChanged;
        if (_networkHeadValue != null) _networkHeadValue.OnValueChanged -= OnHeadValueChanged;
    }

    private void UnsubscribeFromGameManagerEvents()
    {
        if (_gameManager != null)
        {
            _gameManager.OnMoveDirChanged -= HandleMoveDirChanged;
            Debug.Log($"[{GetType().Name}] GameManager 이벤트 구독 해지 완료.");
        }
    }

    // --- 콜백 메서드 ---
    private void OnScoreChanged(int previousValue, int newValue)
    {
        Debug.Log($"[{GetType().Name}] 점수 변경 감지: {previousValue} -> {newValue}");
        UpdateScoreUI(newValue);
        // 클라이언트 측 리더보드 UI 업데이트 등
    }

    private void OnSizeChanged(int previousValue, int newValue)
    {
        Debug.Log($"[{GetType().Name}] 크기 변경 감지: {previousValue} -> {newValue}");
        UpdateSnakeBodySize(newValue);
    }

    private void OnPlayerIdChanged(NetworkString previousValue, NetworkString newValue)
    {
        Debug.Log($"[{GetType().Name}] Player ID 변경 감지: {previousValue} -> {newValue}");
        UpdateUniqueIdComponent(newValue);
    }
    
    private void OnHeadValueChanged(int previousValue, int newValue)
    {
        Debug.Log($"[{GetType().Name}] 헤드 값 변경 감지: {previousValue} -> {newValue}");
        UpdateHeadValueDisplay(newValue);
        
        // 서버에서만 처리 (세그먼트 추가, 점수 업데이트 등)
        if (IsServer)
        {
            // 값이 2의 제곱수인지 확인 (로그 2가 정수인지)
            float log2Value = Mathf.Log(newValue, 2);
            if (Mathf.Approximately(log2Value, Mathf.Round(log2Value)))
            {
                // 2의 제곱수이면 세그먼트 추가
                AddBodySegment();
                
                // 점수 업데이트 (새로운 값만큼 점수 추가)
                _networkScore.Value += newValue;
            }
        }
    }
    #endregion

    // --- 실제 로직 수행 함수 (예시) ---
    private void UpdateScoreUI(int score)
    {
        // 점수 관련 UI 업데이트 로직
        // Debug.Log($"UI 업데이트: 점수 = {score}");
    }

    private void UpdateSnakeBodySize(int newSize)
    {
        // _snake 또는 _snakeBody 컴포넌트를 사용하여 실제 지렁이 몸통 크기 조절
        // Debug.Log($"스네이크 몸통 크기 조절: {newSize}");
        // _snake?.Body.SetSize(newSize); // 예시적인 호출
    }

     private void UpdateUniqueIdComponent(NetworkString playerId)
     {
        // if (_uniqueIdComponent != null)
        // {
        //     _uniqueIdComponent.Value = playerId;
        //      Debug.Log($"UniqueId 컴포넌트 업데이트: {playerId}");
        // }
     }
     
     private void UpdateHeadValueDisplay(int value)
     {
         if (_snake != null && _snake.Head != null)
         {
             _snake.Head.SetValue(value);
         }
     }

    // --- 입력 처리 및 서버 RPC ---
    private void HandleMoveDirChanged(Vector2 moveDirection)
    {
        // 로컬 플레이어만 입력 처리 및 서버로 전송
        if (!IsOwner) return;

        // Debug.Log($"[PlayerSnakeController] 이동 방향 변경 감지: {moveDirection}");

        // _snake?.SetInputDirection(moveDirection); // 로컬에서 즉시 반영 (선택 사항)

        // 서버로 이동 방향 전송
        UpdateMoveDirectionServerRpc(moveDirection);
    }
    
    #region 2048 Snake Game Logic
    /// <summary>
    /// Snake 헤드의 값을 증가시킵니다.
    /// </summary>
    public void IncreaseHeadValue(int increment)
    {
        if (!IsServer) return;
        
        _networkHeadValue.Value += increment;
    }
    
    /// <summary>
    /// 새로운 Body 세그먼트를 추가합니다.
    /// </summary>
    private void AddBodySegment()
    {
        if (!IsServer) return;
        
        // Body 세그먼트 프리팹이 없으면 로드
        if (_bodySegmentPrefab == null)
        {
            _bodySegmentPrefab = _resourceManager.Load<GameObject>("Prefabs/Snake/Body Detail");
            if (_bodySegmentPrefab == null)
            {
                Debug.LogError("Body 세그먼트 프리팹을 로드할 수 없습니다!");
                return;
            }
        }
        
        // 세그먼트 스폰 위치 계산 (마지막 세그먼트 뒤 또는 헤드 뒤)
        Vector3 spawnPosition;
        if (_bodySegments.Count > 0)
        {
            // 마지막 세그먼트 위치 가져오기
            GameObject lastSegment = _bodySegments[_bodySegments.Count - 1];
            spawnPosition = lastSegment.transform.position - lastSegment.transform.forward * _segmentSpacing;
        }
        else
        {
            // 헤드 뒤에 생성
            spawnPosition = _snake.Head.transform.position - _snake.Head.transform.forward * _segmentSpacing;
        }
        
        // 세그먼트 생성 및 설정
        GameObject segment = Instantiate(_bodySegmentPrefab, spawnPosition, Quaternion.identity);
        segment.transform.parent = transform; // 부모를 PlayerSnakeController로 설정
        
        // NetworkObject 컴포넌트가 있으면 스폰
        NetworkObject networkObject = segment.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
        }
        
        // 세그먼트 컴포넌트 설정
        SnakeBodySegment segmentComponent = segment.GetComponent<SnakeBodySegment>();
        if (segmentComponent != null)
        {
            // 세그먼트 값 설정
            int segmentValue = _networkHeadValue.Value;
            segmentComponent.SetValue(segmentValue);
            
            _bodySegmentComponents.Add(segmentComponent);
        }
        
        // 목록에 추가
        _bodySegments.Add(segment);
        
        // 네트워크 사이즈 업데이트
        _networkSize.Value = _bodySegments.Count + 1; // +1은 헤드
    }
    
    /// <summary>
    /// 헤드 위치 기록을 저장합니다.
    /// </summary>
    private void StorePositionHistory()
    {
        if (_snake != null && _snake.Head != null)
        {
            _moveHistory.Enqueue(_snake.Head.transform.position);
            
            // 기록이 너무 많아지지 않도록 제한
            int maxHistorySize = 100 + _bodySegments.Count * 10; // 충분한 히스토리 저장
            while (_moveHistory.Count > maxHistorySize)
            {
                _moveHistory.Dequeue();
            }
        }
    }
    
    /// <summary>
    /// 세그먼트 위치를 업데이트합니다.
    /// </summary>
    private void UpdateBodySegmentsPositions()
    {
        if (_bodySegments.Count == 0) return;
        
        // 히스토리가 충분히 있는지 확인
        if (_moveHistory.Count < _bodySegments.Count * 10)
        {
            return; // 히스토리가 충분하지 않으면 스킵
        }
        
        // 각 세그먼트마다 적절한 지연 후의 위치 할당
        for (int i = 0; i < _bodySegments.Count; i++)
        {
            try
            {
                GameObject segment = _bodySegments[i];
                if (segment == null) continue;
                
                // 히스토리에서 해당 세그먼트에 맞는 위치 가져오기
                int delay = (i + 1) * 10; // 각 세그먼트마다 지연 적용
                
                // 히스토리에서 적절한 위치 가져오기
                Vector3[] historyArray = _moveHistory.ToArray();
                int targetIndex = Mathf.Max(0, historyArray.Length - delay);
                Vector3 targetPosition = historyArray[targetIndex];
                
                // 부드러운 이동을 위한 Lerp 적용
                segment.transform.position = Vector3.Lerp(segment.transform.position, targetPosition, Time.deltaTime * _initialSnakeSpeed);
                
                // 다음 위치를 향해 회전 (이전 위치와 현재 위치 사이의 방향 계산)
                if (targetIndex > 0)
                {
                    Vector3 prevPosition = historyArray[Mathf.Max(0, targetIndex - 1)];
                    Vector3 direction = targetPosition - prevPosition;
                    
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        segment.transform.rotation = Quaternion.LookRotation(direction);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"세그먼트 위치 업데이트 중 오류: {ex.Message}");
            }
        }
    }
    #endregion

    #region Server RPCs
    [ServerRpc]
    private void UpdateMoveDirectionServerRpc(Vector2 moveDirection)
    {
        if (!IsServer) return;

        if (_snake != null && _snake.Head != null)
        {
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                // 1. 서버에서 목표 방향 계산 및 설정 (즉시 회전은 선택적)
                Vector3 currentPosition = _snake.Head.transform.position;
                Vector3 targetDirection = new Vector3(moveDirection.x, 0, moveDirection.y).normalized;
                Vector3 targetPosition = currentPosition + targetDirection;
                // 서버 스네이크도 즉시 방향을 설정하거나, Slerp를 사용하도록 놔둘 수 있음
                _snake.Head.LookAt(targetPosition); // SnakeHead에 있는 메소드 이름 사용

                Vector3 moveForward = _snake.Head.transform.forward; // 실제 설정된 방향

                // 3. 모든 클라이언트에게 목표 방향 전송
                UpdateDirectionClientRpc(moveForward);
            }
            // else // 입력 없을 때 처리: 움직이지 않으므로 RPC 불필요 or 멈춤 RPC
            // {
            // }
        }
        else
        {
             Debug.LogError($"[{GetType().Name}] 서버: RPC 실행 중 Snake 또는 SnakeHead 참조가 null입니다!");
        }
    }

    // --- 다른 기존 ServerRpc들 (UpdateScoreServerRpc, UpdateSizeServerRpc) ---
    [ServerRpc]
    public void UpdateScoreServerRpc(int amount)
    {
        if (!IsServer) return;
        _networkScore.Value += amount;
    }

     [ServerRpc]
     public void UpdateSizeServerRpc(int newSize)
     {
         if (!IsServer) return;
         _networkSize.Value = newSize;
     }
     
     [ServerRpc]
     public void IncreaseHeadValueServerRpc(int increment)
     {
         if (!IsServer) return;
         _networkHeadValue.Value += increment;
     }
    #endregion

    #region Client RPCs
    // 기존 MoveSnakeClientRpc 제거 또는 주석 처리
    // [ClientRpc]
    // private void MoveSnakeClientRpc(Vector3 direction, float speed, float deltaTime)
    // { ... }

    [ClientRpc]
    private void UpdateDirectionClientRpc(Vector3 direction)
    {
        // 서버 자신은 이미 방향을 설정했으므로 무시
        if (IsServer) return;

        if (_snake != null && _snake.Head != null)
        {
            // 클라이언트에서 목표 방향 설정 (실제 회전은 Update에서 Slerp로 처리)
            _snake.Head.SetTargetDirectionFromServer(direction);
        }

    }
    #endregion

    // --- NetworkString 정의 (NetworkVariable에서 사용) ---
    // 별도 파일로 빼거나 PlayerSnakeController 내부에 둘 수 있음
    public struct NetworkString : INetworkSerializable, IEquatable<NetworkString>
    {
        private Unity.Collections.FixedString64Bytes _value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _value);
        }

        public override string ToString() => _value.ToString();
        public static implicit operator string(NetworkString s) => s.ToString();
        public static implicit operator NetworkString(string s) => new NetworkString { _value = new Unity.Collections.FixedString64Bytes(s) };

        public bool Equals(NetworkString other) => _value.Equals(other._value);
    }
}