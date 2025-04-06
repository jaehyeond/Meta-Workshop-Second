using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using VContainer;
using VContainer.Unity;

// PlayerSnakeController가 NetworkBehaviour를 상속하는지 확인하세요.
public class PlayerSnakeController : NetworkBehaviour
{
    #region Dependencies
    // GameManager는 Owner Client에서만 필요하므로 OnNetworkSpawn에서 Resolve
    private GameManager _gameManager;
    #endregion

    #region Settings
    [Header("Movement Settings")]
    [SerializeField] private float _initialSnakeSpeed = 5f; // Inspector에서 초기 속도 설정
    #endregion

    #region Core Components
    [Header("Core Components")]
    [SerializeField] private Snake _snake; // 실제 스네이크 로직 담당
    #endregion

    #region Network Variables
    // 서버 -> 클라이언트로 동기화될 변수들
    // 권한: 서버만 쓰기 가능, 모든 클라이언트 읽기 가능
    private readonly NetworkVariable<int> _networkScore = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _networkSize = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<NetworkString> _networkPlayerId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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
    #endregion

    #region Initialization
    private void InitializeServerState()
    {
        Debug.Log($"[{GetType().Name}] 서버: 스네이크(OwnerClientId: {OwnerClientId}) 스폰됨. 초기 데이터 설정.");

        // 스네이크 헤드 속도 초기화
        if (_snake != null && _snake.Head != null)
        {
            _snake.Head.Construct(_initialSnakeSpeed);
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

        // NetworkVariable 값 설정 (클라이언트로 동기화됨)
        _networkPlayerId.Value = playerId;
        _networkScore.Value = initialScore;
        _networkSize.Value = initialSize;

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
    }

    private void UnsubscribeFromNetworkVariables()
    {
        Debug.Log($"[{GetType().Name}] 클라이언트: NetworkVariable 변경 구독 해지 시작");
        // Null check for safety during teardown
        if (_networkScore != null) _networkScore.OnValueChanged -= OnScoreChanged;
        if (_networkSize != null) _networkSize.OnValueChanged -= OnSizeChanged;
        if (_networkPlayerId != null) _networkPlayerId.OnValueChanged -= OnPlayerIdChanged;
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