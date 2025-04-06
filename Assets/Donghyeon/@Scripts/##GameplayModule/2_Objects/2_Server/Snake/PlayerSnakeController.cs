using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

// PlayerSnakeController가 NetworkBehaviour를 상속하는지 확인하세요.
public class PlayerSnakeController : NetworkBehaviour
{
    // --- 컴포넌트 참조 ---
    [Header("Core Components")]
    [SerializeField] private Snake _snake; // 실제 스네이크 로직 담당 (움직임, 외형 등)

    // --- 네트워크 동기화 변수 ---
    // 서버 -> 클라이언트로 동기화될 변수들 (예시)
    // 권한 설정: 서버만 쓰기 가능 (Server), 모든 클라이언트 읽기 가능 (Client)
    private NetworkVariable<int> _networkScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _networkSize = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<NetworkString> _networkPlayerId = new NetworkVariable<NetworkString>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // 고유 ID 동기화용

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[PlayerSnakeController] OnNetworkSpawn 시작! NetworkObjectId: {NetworkObjectId}");
        Debug.Log($"[PlayerSnakeController] 역할 정보: IsOwner={IsOwner}, IsServer={IsServer}, IsClient={IsClient}, OwnerClientId={OwnerClientId}");

        // --- 모든 클라이언트 공통 설정 ---
        if (IsClient)
        {
            Debug.Log($"[PlayerSnakeController] 클라이언트: NetworkVariable 변경 구독");
            _networkScore.OnValueChanged += OnScoreChanged;
            _networkSize.OnValueChanged += OnSizeChanged;
            _networkPlayerId.OnValueChanged += OnPlayerIdChanged;

             UpdateScoreUI(_networkScore.Value);
             UpdateSnakeBodySize(_networkSize.Value);
             UpdateUniqueIdComponent(_networkPlayerId.Value);
        }

        // --- Owner 클라이언트 전용 설정 (Coroutine으로 분리) ---
        if (IsOwner)
        {
            Debug.Log($"[PlayerSnakeController] 이 스네이크는 로컬 플레이어(내 것)입니다! 초기화 코루틴 시작.");
            StartCoroutine(InitializeLocalPlayer());
        }
        // --- 원격 클라이언트 전용 설정 ---
        else
        {
            Debug.Log($"[PlayerSnakeController] 이 스네이크는 다른 플레이어의 것입니다!");
            // 원격 플레이어 입력 비활성화 등
            // GetComponent<PlayerInputHandler>().enabled = false;

            // RemoteSnake의 ChangePosition 의도 반영 (NetworkTransform이 주로 처리)
            // NetworkTransform이 위치/회전을 동기화합니다.
            // 필요하다면, 시각적으로 부드러운 회전을 위해 클라이언트 측 보간 로직을 추가할 수 있습니다.
            // 예: 원격 스네이크의 회전을 부드럽게 처리하는 스크립트 활성화
            // GetComponent<RemoteSnakeVisualSmoother>()?.enabled = true; // 예시
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

            // 서버 측에서 리더보드 등 업데이트
            // _leaderboardService?.UpdateLeader(playerId, initialScore); // 예시
        }
    }

    // --- 로컬 플레이어 초기화 코루틴 ---
    private IEnumerator InitializeLocalPlayer()
    {
        Debug.Log($"[PlayerSnakeController] InitializeLocalPlayer 코루틴 시작. CameraProvider.Instance 기다림 시작.");

        // --- CameraProvider.Instance가 설정될 때까지 기다리기 (타임아웃 포함) ---
        float waitTime = 0f;
        float maxWaitTime = 5f; // 최대 5초간 기다림

        while (CameraProvider.Instance == null && waitTime < maxWaitTime)
        {
            // 아직 Instance가 null이면 다음 프레임까지 기다림
            yield return null;
            waitTime += Time.deltaTime;
        }

        // --- 기다린 후 결과 확인 ---
        if (CameraProvider.Instance == null)
        {
            Debug.LogError($"[PlayerSnakeController] {maxWaitTime}초 동안 기다렸지만 CameraProvider.Instance가 여전히 null입니다! 초기화 실패.");
            yield break; // 코루틴 종료
        }

        Debug.Log($"[PlayerSnakeController] CameraProvider.Instance 찾음 (대기 시간: {waitTime:F2}초). 카메라 설정 시도.");

        // --- 카메라 타겟 설정 ---
        try // Follow 메서드 호출 시 예외 발생 가능성 대비
        {
            CameraProvider.Instance.Follow(this.transform);
            Debug.Log($"[PlayerSnakeController] CameraProvider.Instance.Follow 호출 완료.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerSnakeController] CameraProvider.Follow 호출 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
        }


        // --- 기타 로컬 플레이어 초기화 ---
        // GetComponent<PlayerInputHandler>().enabled = true;
    }

    // --- 네트워크 변수 변경 콜백 메서드 ---
    private void OnScoreChanged(int previousValue, int newValue)
    {
        Debug.Log($"[PlayerSnakeController] 점수 변경 감지: {previousValue} -> {newValue}");
        UpdateScoreUI(newValue);

        // RemoteSnake의 ChangeScore 의도 반영 (리더보드 업데이트)
        // 클라이언트에서 리더보드 서비스 직접 업데이트는 지양. UI만 업데이트하거나, 서버 응답 대기.
        // if (!IsServer) _leaderboardService?.UpdateLeader(_networkPlayerId.Value, newValue); // 클라이언트에서 직접 호출 X
    }

    private void OnSizeChanged(int previousValue, int newValue)
    {
        Debug.Log($"[PlayerSnakeController] 크기 변경 감지: {previousValue} -> {newValue}");
        UpdateSnakeBodySize(newValue);

        // RemoteSnake의 ChangeSize/ProcessChangeSizeTo 의도 반영
        // 실제 몸통 조절은 여기서 수행
    }

     private void OnPlayerIdChanged(NetworkString previousValue, NetworkString newValue)
    {
         Debug.Log($"[PlayerSnakeController] Player ID 변경 감지: {previousValue} -> {newValue}");
         UpdateUniqueIdComponent(newValue);
         // 필요하다면 다른 로직 수행
    }

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
        base.OnNetworkDespawn();

        Debug.Log($"[PlayerSnakeController] OnNetworkDespawn 호출됨! NetworkObjectId: {NetworkObjectId}");
        Debug.Log($"[PlayerSnakeController] 스네이크 객체 해제 처리 필요 (OwnerClientId: {OwnerClientId})");

        // 클라이언트에서 구독 해제
        if (IsClient)
        {
            _networkScore.OnValueChanged -= OnScoreChanged;
            _networkSize.OnValueChanged -= OnSizeChanged;
            _networkPlayerId.OnValueChanged -= OnPlayerIdChanged;
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
        // 서버 측 리더보드 업데이트
        // _leaderboardService?.UpdateLeader(_networkPlayerId.Value, _networkScore.Value);
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