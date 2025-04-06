using System;
using System.Linq; // LastOrDefault를 사용하기 위해 추가
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// PlayerSnakeController가 NetworkBehaviour를 상속하는지 확인하세요.
public class PlayerSnakeController : NetworkBehaviour
{
    // --- 컴포넌트 참조 ---
    [Header("Core Components")]
    [SerializeField] private Snake _snake; // 실제 스네이크 로직 담당 (움직임, 외형 등)
    [SerializeField] private GameObject _bodyDetailPrefab; // Body 세그먼트 프리팹
    
    // --- 2048 게임 관련 변수 ---
    [Header("2048 Game Settings")]
    [SerializeField] private int _valueIncrement = 2; // Apple 획득 시 증가하는 값 (기본값: 2)
    [SerializeField] private float _segmentSpacing = 1f; // 세그먼트 간 간격

    // --- 네트워크 동기화 변수 ---
    // 서버 -> 클라이언트로 동기화될 변수들 (예시)
    // 권한 설정: 서버만 쓰기 가능 (Server), 모든 클라이언트 읽기 가능 (Client)
    private NetworkVariable<int> _networkScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _networkSize = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<NetworkString> _networkPlayerId = new NetworkVariable<NetworkString>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // 고유 ID 동기화용
    
    // 2048 게임을 위한 네트워크 변수 추가
    private NetworkVariable<int> _networkHeadValue = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
            _networkHeadValue.OnValueChanged += OnHeadValueChanged; // 2048 게임용 값 변경 구독

             UpdateScoreUI(_networkScore.Value);
             UpdateSnakeBodySize(_networkSize.Value);
             UpdateUniqueIdComponent(_networkPlayerId.Value);
             UpdateSnakeHeadValue(_networkHeadValue.Value); // 초기 Head 값 설정
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
            int initialHeadValue = 0; // 2048 게임 초기 값

            // NetworkVariable 값 설정 (클라이언트로 동기화됨)
            _networkPlayerId.Value = playerId;
            _networkScore.Value = initialScore;
            _networkSize.Value = initialSize;
            _networkHeadValue.Value = initialHeadValue;

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
        }
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
     
    // 2048 게임용 Snake Head 값 업데이트
    private void UpdateSnakeHeadValue(int value)
    {
        if (_snake != null)
        {
            _snake.UpdateHeadValue(value);
            Debug.Log($"Snake Head 값 업데이트: {value}");
        }
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
            _networkHeadValue.OnValueChanged -= OnHeadValueChanged; // 2048 게임용 구독 해제
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
     
    // 2048 게임용 - Apple 획득 시 Head 값 증가 (서버 전용)
    [ServerRpc(RequireOwnership = false)] // Apple이 서버에서 직접 호출할 수 있도록 RequireOwnership = false
    public void IncreaseHeadValueServerRpc(int increment)
    {
        if (!IsServer) return;
        
        // 현재 값에서 증가
        int currentValue = _networkHeadValue.Value;
        int newValue = currentValue + increment;
        _networkHeadValue.Value = newValue;
        
        Debug.Log($"[PlayerSnakeController] Head 값 증가: {currentValue} -> {newValue}");
        
        // 2의 제곱수에 도달하면 Body 세그먼트 추가
        if (IsPowerOfTwo(newValue) && newValue > 0)
        {
            // 이전 값(현재 값의 1/2)으로 새 세그먼트 추가
            AddBodySegmentServerRpc(newValue / 2);
            Debug.Log($"[PlayerSnakeController] 2의 제곱수({newValue}) 도달 - 몸통 세그먼트 추가");
        }
        
        // 점수도 함께 증가
        _networkScore.Value += increment;
    }
    
    // Body 세그먼트 추가 (서버 전용)
    [ServerRpc]
    private void AddBodySegmentServerRpc(int value)
    {
        if (!IsServer) return;
        
        // 새 세그먼트 스폰 위치 계산 (마지막 세그먼트 뒤에 배치)
        Vector3 spawnPosition;
        
        if (_snake.Body.Size > 0)
        {
            // 마지막 세그먼트 위치 기준으로 설정
            // IEnumerable에서 마지막 요소 가져오기
            Vector3 lastPosition = Vector3.zero;
            foreach (Vector3 pos in _snake.GetBodyDetailPositions())
            {
                lastPosition = pos; // 마지막 요소 저장
            }
            spawnPosition = lastPosition - _snake.transform.forward * _segmentSpacing;
        }
        else
        {
            // 첫 세그먼트는 헤드 뒤에 배치
            spawnPosition = _snake.Head.transform.position - _snake.Head.transform.forward * _segmentSpacing;
        }
        
        // 세그먼트 인스턴스 생성
        GameObject newSegment = Instantiate(_bodyDetailPrefab, spawnPosition, _snake.transform.rotation);
        NetworkObject netObj = newSegment.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(); // 네트워크에 스폰
            
            // Snake에 세그먼트 추가 및 값 설정
            _snake.AddDetailWithValue(newSegment, value);
            
            // 크기 증가 네트워크 동기화
            _networkSize.Value++;
            
            // 클라이언트에 세그먼트 값 동기화
            SyncSegmentValueClientRpc(_networkSize.Value - 1, value);
        }
    }
    
    // 세그먼트 값 클라이언트 동기화
    [ClientRpc]
    private void SyncSegmentValueClientRpc(int segmentIndex, int value)
    {
        Debug.Log($"[PlayerSnakeController] 세그먼트 동기화: 인덱스 {segmentIndex}, 값 {value}");
        
        // 클라이언트에서는 값만 설정
        // 실제 세그먼트 추가는 _networkSize 변경 이벤트에서 이루어짐
    }
    
    // 충돌 감지 (Apple 등)
    private void OnTriggerEnter(Collider other)
    {
        // 서버에서만 처리
        if (!IsServer) return;
        
        // Apple인지 확인
        if (other.CompareTag("Apple"))
        {
            Debug.Log($"[PlayerSnakeController] Apple과 충돌 감지!");
            
            // Head 값 증가
            IncreaseHeadValueServerRpc(_valueIncrement);
            
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
    
    // 2의 거듭제곱인지 확인
    private bool IsPowerOfTwo(int x)
    {
        return x != 0 && (x & (x - 1)) == 0;
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