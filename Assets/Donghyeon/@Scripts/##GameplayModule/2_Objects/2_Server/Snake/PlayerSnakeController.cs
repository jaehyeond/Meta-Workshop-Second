using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using System.Linq;

public class PlayerSnakeController : NetworkBehaviour
{
    #region Dependencies
    private GameManager _gameManager;
    private AppleManager _appleManager;
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
    [SerializeField] public Snake _snake; // 실제 스네이크 로직 담당
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
    public NetworkVariable<int> _networkHeadValue = new(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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
            StartCoroutine(FollowPlayerWithCamera());
            _gameManager = FindObjectOfType<LifetimeScope>()?.Container.Resolve<GameManager>();
            _appleManager = FindObjectOfType<LifetimeScope>()?.Container.Resolve<AppleManager>();
            if (_gameManager != null)
            {
                _gameManager.OnMoveDirChanged += HandleMoveDirChanged;
                Debug.Log($"[{GetType().Name}] GameManager 이벤트 구독 완료.");
            }
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
            _gameManager.OnMoveDirChanged -= HandleMoveDirChanged;

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



    private IEnumerator FollowPlayerWithCamera()
    {
        Debug.Log($"[{GetType().Name}] 카메라 추적 코루틴 시작. CameraProvider 및 Snake Head 대기.");

        float waitTime = 0f;
        const float maxWaitTime = 5f; // 최대 대기 시간

        // CameraProvider 인스턴스와 Snake Head가 준비될 때까지 대기
        while ((CameraProvider.Instance == null || _snake == null || _snake.Head == null) && waitTime < maxWaitTime)
        {
            yield return null; // 다음 프레임까지 대기
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
            // 타임아웃 또는 필요한 컴포넌트 누락 시 로그 출력
            if(CameraProvider.Instance == null) Debug.LogError($"[{GetType().Name}] CameraProvider.Instance를 찾지 못했습니다.");
            if(_snake == null) Debug.LogError($"[{GetType().Name}] _snake 참조가 null입니다.");
            else if (_snake.Head == null) Debug.LogError($"[{GetType().Name}] _snake.Head 참조가 null입니다.");
            
            Debug.LogError($"[{GetType().Name}] 카메라 추적 설정 실패 (대기 시간 초과 또는 컴포넌트 누락).");
        }
    }

    #endregion

    #region Network Variable Handling
    private void SubscribeToNetworkVariables()
    {
        Debug.Log($"[{GetType().Name}] 클라이언트: NetworkVariable 변경 구독 시작");

        _networkHeadValue.OnValueChanged += OnHeadValueChanged;
    }

    private void UnsubscribeFromNetworkVariables()
    {
        Debug.Log($"[{GetType().Name}] 클라이언트: NetworkVariable 변경 구독 해지 시작");

        if (_networkHeadValue != null) _networkHeadValue.OnValueChanged -= OnHeadValueChanged;
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
                AddBodySegmentServerRpc(newValue);
                _networkScore.Value += newValue;
            }
        }
    }

    [ServerRpc]
    private void AddBodySegmentServerRpc(int headValue)
    {
        if (!IsServer) return;

        // Body 세그먼트 프리팹이 없으면 로그 출력
        if (_bodySegmentPrefab == null)
        {
            Debug.LogError($"[{GetType().Name}] Body 세그먼트 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 새로운 세그먼트의 값은 헤드 값의 절반
        int segmentValue = headValue / 2;

        // 스폰 위치 계산 (마지막 세그먼트 뒤 또는 헤드 뒤)
        Vector3 spawnPosition;
        if (_bodySegments.Count > 0)
        {
            spawnPosition = _bodySegments[_bodySegments.Count - 1].transform.position - transform.forward * _segmentSpacing;
        }
        else
        {
            spawnPosition = transform.position - transform.forward * _segmentSpacing;
        }

        // 세그먼트 생성 및 NetworkObject 설정
        GameObject segment = Instantiate(_bodySegmentPrefab, spawnPosition, transform.rotation);
        NetworkObject networkObject = segment.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();

            // SnakeBodySegment 컴포넌트 설정
            if (segment.TryGetComponent(out SnakeBodySegment segmentComponent))
            {
                segmentComponent.SetValue(segmentValue);
                _bodySegmentComponents.Add(segmentComponent);
            }

            // Snake 컴포넌트에 세그먼트 추가
            if (_snake != null)
            {
                _snake.AddDetail(segment);
            }

            // 목록에 추가
            _bodySegments.Add(segment);
            
            // 네트워크 사이즈 업데이트
            _networkSize.Value = _bodySegments.Count + 1; // +1은 헤드

            Debug.Log($"[{GetType().Name}] 새로운 Body 세그먼트 추가됨 (값: {segmentValue})");
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] 세그먼트에 NetworkObject가 없습니다!");
            Destroy(segment);
        }
    }
    #endregion

   
     
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
        if (!IsOwner) return;
        UpdateMoveDirectionServerRpc(moveDirection);
    }
    
    #region 2048 Snake Game Logic

    
    /// <summary>
    /// 새로운 Body 세그먼트를 추가합니다.
    /// </summary>
    // private void AddBodySegment()
    // {
    //     if (!IsServer) return;
        
    //     // Body 세그먼트 프리팹이 없으면 로드
    //     if (_bodySegmentPrefab == null)
    //     {
    //         _bodySegmentPrefab = _resourceManager.Load<GameObject>("Prefabs/Snake/Body Detail");
    //         if (_bodySegmentPrefab == null)
    //         {
    //             Debug.LogError("Body 세그먼트 프리팹을 로드할 수 없습니다!");
    //             return;
    //         }
    //     }
        
    //     // 세그먼트 스폰 위치 계산 (마지막 세그먼트 뒤 또는 헤드 뒤)
    //     Vector3 spawnPosition;
    //     if (_bodySegments.Count > 0)
    //     {
    //         // 마지막 세그먼트 위치 가져오기
    //         GameObject lastSegment = _bodySegments[_bodySegments.Count - 1];
    //         spawnPosition = lastSegment.transform.position - lastSegment.transform.forward * _segmentSpacing;
    //     }
    //     else
    //     {
    //         // 헤드 뒤에 생성
    //         spawnPosition = _snake.Head.transform.position - _snake.Head.transform.forward * _segmentSpacing;
    //     }
        
    //     // 세그먼트 생성 및 설정
    //     GameObject segment = Instantiate(_bodySegmentPrefab, spawnPosition, Quaternion.identity);
    //     segment.transform.parent = transform; // 부모를 PlayerSnakeController로 설정
        
    //     // NetworkObject 컴포넌트가 있으면 스폰
    //     NetworkObject networkObject = segment.GetComponent<NetworkObject>();
    //     if (networkObject != null)
    //     {
    //         networkObject.Spawn();
    //     }
        
    //     // 세그먼트 컴포넌트 설정
    //     SnakeBodySegment segmentComponent = segment.GetComponent<SnakeBodySegment>();
    //     if (segmentComponent != null)
    //     {
    //         // 세그먼트 값 설정
    //         int segmentValue = _networkHeadValue.Value;
    //         segmentComponent.SetValue(segmentValue);
            
    //         _bodySegmentComponents.Add(segmentComponent);
    //     }
        
    //     // 목록에 추가
    //     _bodySegments.Add(segment);
        
    //     // 네트워크 사이즈 업데이트
    //     _networkSize.Value = _bodySegments.Count + 1; // +1은 헤드
    // }
    


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


    #region Client RPCs

    [ClientRpc]
    private void UpdateDirectionClientRpc(Vector3 direction)
    {
        // 서버 자신은 이미 방향을 설정했으므로 무시
        if (IsServer) return;

        if (_snake != null && _snake.Head != null)
        {
            _snake.Head.SetTargetDirectionFromServer(direction);
        }

    }
    #endregion


    #endregion




    /// <summary>
    /// 스네이크 죽음 처리 로직 (서버 전용)
    /// </summary>
    private void Die()
    {
        if (!IsServer) return;

        Debug.LogWarning($"[{GetType().Name}] 스네이크 사망 처리 (서버): OwnerClientId={OwnerClientId}");

        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null)
        {
             networkObject.Despawn(true); // true: 즉시 파괴
        }
        else
        {
             Destroy(gameObject);
        }

    }


}