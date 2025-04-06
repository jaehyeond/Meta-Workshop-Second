using System;
using System.Linq; // LastOrDefault를 사용하기 위해 추가
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VContainer; // LifetimeScope 및 Container 관련
using Unity.Assets.Scripts.Resource; // ResourceManager에 접근하기 위해 추가
using Unity.Assets.Scripts.UI; // UI 네임스페이스 추가
using Unity.Assets.Scripts.GameManager; // GameManager 네임스페이스 추가

// 조이스틱 컨트롤을 위한 인터페이스
public interface ISnakeInputProvider
{
    Vector2 MovementDirection { get; }
    event Action<Vector2> OnMovementDirectionChanged;
}

// 조이스틱 입력 제공자 클래스 (같은 파일에 추가)
public class JoystickInputProvider : MonoBehaviour, ISnakeInputProvider
{
    [SerializeField] private UI_Joystick _joystick; // 인스펙터에서 할당
    private Vector2 _direction;

    public Vector2 MovementDirection => _direction;
    
    public event Action<Vector2> OnMovementDirectionChanged;

    private void Update()
    {
        if (_joystick == null) return; // 조이스틱이 없으면 무시
        
        // 조이스틱의 방향 가져오기
        Vector2 newDirection = Vector2.zero;
        
        if (_joystick != null && _joystick.GetComponent<UI_Joystick>() != null)
        {
            // UI_Joystick의 구현에 따라 방향을 가져오는 방식 조정
            var joystick = _joystick.GetComponent<UI_Joystick>();
            newDirection = joystick.GetJoystickDirection();
        }
        
        // 방향이 변경되었는지 확인
        if (Vector2.Distance(newDirection, _direction) > 0.01f)
        {
            _direction = newDirection;
            OnMovementDirectionChanged?.Invoke(_direction);
        }
    }
}

// PlayerSnakeController가 NetworkBehaviour를 상속하는지 확인하세요.
public class PlayerSnakeController : NetworkBehaviour
{
    #region Dependencies
    // GameManager는 Owner Client에서만 필요하므로 OnNetworkSpawn에서 Resolve
    [Inject] private GameManager _gameManager;
    [Inject] private ResourceManager _resourceManager;
    
    // 조이스틱 입력 제공자
    [SerializeField] private bool _useJoystickInput = true;
    private ISnakeInputProvider _inputProvider;
    #endregion

    #region Settings
    [Header("Movement Settings")]
    [SerializeField] private float _initialSnakeSpeed = 5f; // Inspector에서 초기 속도 설정
    #endregion

    #region Core Components
    [Header("Core Components")]
    [SerializeField] private Snake _snake; // 실제 스네이크 로직 담당 (움직임, 외형 등)
    [SerializeField] private GameObject _bodyDetailPrefab; // Body 세그먼트 프리팹
    
    // --- 2048 게임 관련 변수 ---
    [Header("2048 Game Settings")]
    [SerializeField] private int _valueIncrement = 2; // 각 사과마다 증가할 값
    [SerializeField] private float _segmentSpacing = 0.5f; // 세그먼트 간 간격
    private List<Vector3> _bodyPositions = new List<Vector3>(); // 몸통 위치 기록
    private List<GameObject> _bodySegments = new List<GameObject>(); // 몸통 세그먼트 객체

    // --- 네트워크 동기화 변수 ---
    // 서버 -> 클라이언트로 동기화될 변수들 (예시)
    // 권한 설정: 서버만 쓰기 가능 (Server), 모든 클라이언트 읽기 가능 (Client)
    private NetworkVariable<int> _networkScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _networkSize = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<NetworkString> _networkPlayerId = new NetworkVariable<NetworkString>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // 고유 ID 동기화용
    private NetworkVariable<int> _networkHeadValue = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // 2048 게임용

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            InitializeServerState();
        }

        if (IsClient)
        {
            Debug.Log($"[PlayerSnakeController] 클라이언트: NetworkVariable 변경 구독");
            _networkScore.OnValueChanged += OnScoreChanged;
            _networkSize.OnValueChanged += OnSizeChanged;
            _networkPlayerId.OnValueChanged += OnPlayerIdChanged;
            _networkHeadValue.OnValueChanged += OnHeadValueChanged;

             UpdateScoreUI(_networkScore.Value);
             UpdateSnakeBodySize(_networkSize.Value);
             UpdateUniqueIdComponent(_networkPlayerId.Value);
        }

        // --- Owner 클라이언트 전용 설정 (Coroutine으로 분리) ---
        if (IsOwner)
        {
            InitializeOwnerClient();
            
            // 조이스틱 입력 설정
            if (_useJoystickInput)
            {
                SetupJoystickInput();
            }
        }
        else
        {
            InitializeRemoteClient();
        }

        // 서버 측 정리 로직 (필요 시)
        // if (IsServer) { /* ... */ }

        Debug.Log($"[{GetType().Name}] OnNetworkSpawn 호출됨! NetworkObjectId: {NetworkObjectId}, OwnerClientId: {OwnerClientId}");
    }
    #endregion

    #region Joystick Input
    private void SetupJoystickInput()
    {
        // 조이스틱 입력 프로바이더를 찾거나 생성
        _inputProvider = FindObjectOfType<JoystickInputProvider>();
        
        if (_inputProvider == null)
        {
            Debug.LogWarning("JoystickInputProvider를 찾을 수 없습니다. 새로 생성합니다.");
            
            // 프로바이더가 없으면 생성
            GameObject joystickProviderGO = new GameObject("JoystickInputProvider");
            _inputProvider = joystickProviderGO.AddComponent<JoystickInputProvider>();
        }
        
        // 조이스틱 입력 이벤트 구독
        _inputProvider.OnMovementDirectionChanged += HandleMoveDirChanged;
        Debug.Log("조이스틱 입력 프로바이더에 구독 완료");
    }
    
    // 조이스틱 입력 처리 메서드
    private void HandleMoveDirChanged(Vector2 direction)
    {
        if (!IsOwner || _snake == null || _snake.Head == null) return;
        
        // 스네이크 헤드 방향 변경
        if (direction.magnitude > 0.1f) // 최소 입력 임계값
        {
            // 방향 변경 메서드 호출
            Vector3 dir3D = new Vector3(direction.x, 0, direction.y);
            _snake.LookAt(_snake.Head.transform.position + dir3D);
            
            // 서버에 방향 변경 알림 (필요시)
            UpdateDirectionServerRpc(dir3D);
        }
    }
    
    // 서버에 방향 변경 알림 (선택 사항)
    [ServerRpc]
    private void UpdateDirectionServerRpc(Vector3 direction)
    {
        if (!IsServer) return;
        
        // 서버 측에서 방향 변경 처리 (필요시)
        if (_snake != null && _snake.Head != null)
        {
            _snake.LookAt(_snake.Head.transform.position + direction);
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
            Debug.Log($"[{GetType().Name}] 서버: 스네이크 헤드 속도 초기화 완료 ({_initialSnakeSpeed})");
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] 서버: Snake 또는 SnakeHead 참조가 null입니다!");
        }

        // --- 서버/호스트 전용 설정 ---
        if (IsServer)
        {
            Debug.Log($"[PlayerSnakeController] 서버/호스트: 스네이크(OwnerClientId: {OwnerClientId}) 스폰됨. 초기 데이터 설정.");

            // SessionManager 등에서 플레이어 데이터 로드
            string playerId = "Player_" + OwnerClientId; // 예시: SessionManager에서 가져와야 함
            int initialScore = 0; // 예시: SessionManager에서 가져오거나 기본값
            int initialSize = 1; // 예시

            // NetworkVariable 값 설정 (클라이언트로 동기화됨)
            _networkPlayerId.Value = playerId;
            _networkScore.Value = initialScore;
            _networkSize.Value = initialSize;
            _networkHeadValue.Value = 0; // 초기 헤드 값 0으로 설정
            
            // 서버 측 다른 시스템 업데이트 (예: 리더보드)
            // _leaderboardService?.UpdateLeader(playerId, initialScore);
        }
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
        Debug.Log($"[{GetType().Name}] 카메라 추적 코루틴 시작.");

        // 카메라 위치 조정 (필요시 구현)
        // CameraManager 등을 통해 카메라를 플레이어에게 고정
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"[{GetType().Name}] 카메라 설정 완료.");
    }
    
    private void ResolveAndSubscribeToGameManager()
    {
        try {
            // Donghyeon의 GameManager 이벤트 구독
            if (_gameManager != null)
            {
                _gameManager.OnMoveDirChanged += HandleGameManagerMoveDirChanged;
                Debug.Log($"[{GetType().Name}] GameManager 이벤트 구독 완료.");
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] GameManager가 null입니다!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] GameManager 이벤트 구독 중 오류: {ex.Message}");
        }
    }
    
    // GameManager의 방향 변경 이벤트 처리
    private void HandleGameManagerMoveDirChanged(Vector2 direction)
    {
        if (!IsOwner || _snake == null) return;
        
        // 조이스틱이 활성화된 경우 무시 (조이스틱이 우선)
        if (_useJoystickInput && _inputProvider != null) return;
        
        // 스네이크 방향 변경
        if (direction.magnitude > 0.1f)
        {
            Vector3 dir3D = new Vector3(direction.x, 0, direction.y);
            _snake.LookAt(_snake.Head.transform.position + dir3D);
            
            // 서버에 방향 변경 알림
            UpdateDirectionServerRpc(dir3D);
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
        // GameManager 이벤트 구독 해제
        if (_gameManager != null)
        {
            _gameManager.OnMoveDirChanged -= HandleGameManagerMoveDirChanged;
            Debug.Log($"[{GetType().Name}] GameManager 이벤트 구독 해지 완료.");
        }
        
        // 조이스틱 입력 구독 해제
        if (_inputProvider != null)
        {
            _inputProvider.OnMovementDirectionChanged -= HandleMoveDirChanged;
            Debug.Log("조이스틱 입력 프로바이더 구독 해지 완료");
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
    
    // 2048 게임용 Head 값 변경 콜백
    private void OnHeadValueChanged(int previousValue, int newValue)
    {
        Debug.Log($"[PlayerSnakeController] Head 값 변경 감지: {previousValue} -> {newValue}");
        UpdateSnakeHeadValue(newValue);
        
        // 2의 제곱수에 도달하면 몸통 세그먼트 추가
        if (IsClient && IsPowerOfTwo(newValue) && newValue > 0)
        {
            // 클라이언트에서는 단순히 시각적 처리만 수행 (실제 추가는 서버에서 함)
            Debug.Log($"[PlayerSnakeController] 2의 제곱수({newValue}) 도달 - 세그먼트 추가 준비");
            
            if (IsServer) 
            {
                AddBodySegment();
            }
        }
    }

    private void OnPlayerIdChanged(NetworkString previousValue, NetworkString newValue)
    {
        Debug.Log($"[{GetType().Name}] Player ID 변경 감지: {previousValue} -> {newValue}");
        UpdateUniqueIdComponent(newValue);
    }
    #endregion

    #region 2048 Game Logic
    // 2의 제곱수인지 확인하는 유틸리티 메서드
    private bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }
    
    // 스네이크 헤드 값 업데이트
    private void UpdateSnakeHeadValue(int newValue)
    {
        if (_snake != null && _snake.Head != null)
        {
            _snake.UpdateHeadValue(newValue);
            
            // 머리 크기도 값에 따라 조정 (Snake 클래스에서 처리)
        }
    }
    
    // 몸통 세그먼트 추가
    private void AddBodySegment()
    {
        if (!IsServer) return;

        Vector3 newSegmentPosition;
        
        // 기존 세그먼트가 있는 경우 마지막 세그먼트 뒤에 추가
        if (_bodySegments.Count > 0)
        {
            // 마지막 세그먼트의 위치 가져오기
            Vector3 lastPosition = _bodySegments[_bodySegments.Count - 1].transform.position;
            Vector3 direction = _snake.Head.transform.position - lastPosition;
            
            // 마지막 세그먼트 뒤에 새 세그먼트 위치 계산
            newSegmentPosition = lastPosition - direction.normalized * _segmentSpacing;
        }
        else // 첫 세그먼트인 경우 머리 뒤에 추가
        {
            Vector3 headDirection = _snake.Head.transform.forward;
            newSegmentPosition = _snake.Head.transform.position - headDirection * _segmentSpacing;
        }
        
        // 세그먼트 생성 및 초기화
        GameObject newSegment = Instantiate(_bodyDetailPrefab, newSegmentPosition, Quaternion.identity);
        NetworkObject netObj = newSegment.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        
        // 세그먼트 값 설정 (머리 값의 절반)
        int headValue = _networkHeadValue.Value;
        int segmentValue = headValue / 2;
        
        // 값을 가진 세그먼트 추가
        _snake.AddDetailWithValue(newSegment, segmentValue);
        
        // 목록에 추가
        _bodySegments.Add(newSegment);
        
        // 크기 업데이트
        UpdateSizeServerRpc(_bodySegments.Count + 1); // 머리 + 세그먼트 수
    }
    
    // Apple을 획득했을 때 호출되는 메서드
    public void OnAppleCollected(int value)
    {
        if (!IsServer) return;
        
        // 점수 업데이트
        UpdateScoreServerRpc(value);
        
        // 머리 값 증가
        int currentHeadValue = _networkHeadValue.Value;
        _networkHeadValue.Value = currentHeadValue + _valueIncrement;
        
        Debug.Log($"[PlayerSnakeController] Apple 획득! 헤드 값 {currentHeadValue} -> {_networkHeadValue.Value}");
    }
    
    // Apple 충돌 감지 (OnTriggerEnter에서 호출)
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        
        if (other.CompareTag("Apple"))
        {
            Debug.Log($"[PlayerSnakeController] Apple과 충돌 감지!");
            
            // Apple 획득 처리
            OnAppleCollected(_valueIncrement);
            
            // Apple 제거 (Apple 스크립트 측에서 처리하도록 설계할 수도 있음)
            NetworkObject appleNetObj = other.GetComponent<NetworkObject>();
            if (appleNetObj != null)
            {
                appleNetObj.Despawn();
                
                // 새 Apple 스폰 요청 (AppleManager가 있다면)
                AppleManager appleManager = FindObjectOfType<AppleManager>();
                appleManager?.SpawnAppleServerRpc();
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


    // --- 정리 로직 ---
    public override void OnNetworkDespawn()
    {
        // 로컬 플레이어만 입력 처리 및 서버로 전송
        if (!IsOwner) return;

        Debug.Log($"[PlayerSnakeController] OnNetworkDespawn 호출됨! NetworkObjectId: {NetworkObjectId}");
        Debug.Log($"[PlayerSnakeController] 스네이크 객체 해제 처리 필요 (OwnerClientId: {OwnerClientId})");

        // 클라이언트에서 구독 해제
        if (IsClient)
        {
            UnsubscribeFromNetworkVariables();
            UnsubscribeFromGameManagerEvents();
        }

        // 서버에서 리더보드 등 정리 (필요 시)
        // if (IsServer)
        // {
        //     _leaderboardService?.RemoveLeader(_networkPlayerId.Value); // 예시
        // }
    }

    // --- 서버에서 호출되어 점수/크기 변경하는 메서드 예시 ---
    [ServerRpc] // Owner 클라이언트만 호출 가능하도록 설정 가능: RequireOwnership = true
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

// UI_Joystick 확장 메서드 - 프로젝트에 맞게 조정할 것
public static class UI_JoystickExtensions
{
    public static Vector2 GetJoystickDirection(this UI_Joystick joystick)
    {
        // 프로젝트의 UI_Joystick 구현에 맞게 방향을 가져오는 방법을 구현
        // 여기서는 기본 벡터를 반환
        return Vector2.zero;
    }
}