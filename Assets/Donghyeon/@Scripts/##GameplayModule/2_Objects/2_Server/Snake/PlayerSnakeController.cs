using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using System.Linq;
using Unity.Assets.Scripts.Resource;

public class PlayerSnakeController : NetworkBehaviour
{
    #region Dependencies
    private GameManager _gameManager;
    private AppleManager _appleManager;
    private ResourceManager _resourceManager;
    private SnakeBodyHandler _snakeBodyHandler;
    private SnakeNetworkHandler _snakeNetworkHandler;
    #endregion

    #region Settings
    [Header("Movement Settings")]
    [SerializeField] private float _initialSnakeSpeed = 5f; // Inspector에서 초기 속도 설정
    
    [Header("2048 Snake Settings")]
    [SerializeField] private float _segmentSpacing = 0.17f; // 더 가깝게 설정 (0.2에서 0.15로)
    [SerializeField] private float _segmentFollowSpeed = 8f; // 따라오는 속도 증가 (12에서 15로)
    #endregion

    #region Core Components
    [Header("Core Components")]
    [SerializeField] public Snake _snake; // 실제 스네이크 로직 담당
    #endregion

    #region Runtime Variables
    private List<GameObject> _bodySegments = new List<GameObject>();
    private List<SnakeBodySegment> _bodySegmentComponents = new List<SnakeBodySegment>();
    private Queue<Vector3> _moveHistory = new Queue<Vector3>(); // 이동 기록을 저장
    private Vector3 _firstSegmentVelocity = Vector3.zero; // 첫 번째 세그먼트 SmoothDamp용
    private List<Vector3> _segmentVelocities = new List<Vector3>(); // 각 세그먼트 SmoothDamp용
    #endregion
    
    #region Network Variables
    // 서버 -> 클라이언트로 동기화될 변수들
    // 권한: 서버만 쓰기 가능, 모든 클라이언트 읽기 가능
    private readonly NetworkVariable<int> _networkScore = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _networkSize = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<NetworkString> _networkPlayerId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> _networkHeadValue = new(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    #endregion

    #region Serialized Fields
    [Header("Snake Settings")]
    [SerializeField] private float _movementSpeed = 3f;
    [SerializeField] private float _rotationSpeed = 180f;
    [SerializeField] private float _movementSmoothing = 0.05f; // 이동 부드러움 정도
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

        // SnakeBodyHandler 초기화
        _snakeBodyHandler = GetComponent<SnakeBodyHandler>();
        if (_snakeBodyHandler == null)
        {
            _snakeBodyHandler = gameObject.AddComponent<SnakeBodyHandler>();
        }
        _snakeBodyHandler.Initialize(_snake);

        // SnakeNetworkHandler 초기화
        _snakeNetworkHandler = GetComponent<SnakeNetworkHandler>();
        if (_snakeNetworkHandler == null)
        {
            _snakeNetworkHandler = gameObject.AddComponent<SnakeNetworkHandler>();
        }
        
        // 플레이어 데이터 로드
        string playerId = "Player_" + OwnerClientId;
        int initialScore = 0;
        int initialSize = 1;
        int initialHeadValue = 2;

        _snakeNetworkHandler.InitializeNetworkVariables(playerId, initialScore, initialSize, initialHeadValue);

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
                AddBodySegmentServerRpc();
                _networkScore.Value += newValue;
            }
            
            // Body 세그먼트 값 업데이트
            UpdateBodyValues(newValue);
        }
    }
    
    // 매 프레임마다 실행되는 고정 업데이트 (물리 계산 간격으로 실행됨)
    private void FixedUpdate()
    {
        // 서버에서만 위치 기록 업데이트
        if (!IsServer) return;
        
        if (_snake != null && _snake.Head != null)
        {
            _snakeBodyHandler.UpdateBodySegmentsPositions();
        }
    }
    
    private void UpdateBodySegmentsPositions()
    {
        if (_bodySegments.Count == 0 || _snake == null || _snake.Head == null) return;

        try
        {
            // 세그먼트 개수만큼 속도 벡터 목록 크기 확인
            while (_segmentVelocities.Count < _bodySegments.Count)
            {
                _segmentVelocities.Add(Vector3.zero);
            }
            
            // 보간에 사용할 고정된 비율 (Time.smoothDeltaTime 사용하여 더 부드럽게)
            float followRatio = _segmentFollowSpeed * Time.smoothDeltaTime;
            
            // 머리 위치와 방향 캐싱 (성능 및 안정성 향상)
            Vector3 headPosition = _snake.Head.transform.position;
            Quaternion headRotation = _snake.Head.transform.rotation;
            Vector3 headForward = _snake.Head.transform.forward;
            
            // 첫 번째 세그먼트 업데이트 (머리 바로 뒤)
            if (_bodySegments.Count > 0)
            {
                GameObject firstSegment = _bodySegments[0];
                if (firstSegment == null) return;
                
                // 목표 위치 계산 (머리 바로 뒤)
                Vector3 targetPosition = headPosition - headForward * _segmentSpacing;
                
                // 현재 속도 벡터 임시 저장
                Vector3 currentVelocity = _firstSegmentVelocity;
                
                // 부드러운 보간 (Vector3.SmoothDamp 대신 Lerp 사용)
                firstSegment.transform.position = Vector3.Lerp(
                    firstSegment.transform.position,
                    targetPosition,
                    followRatio * 1.5f
                );
                
                // 현재 속도 저장 (다음 프레임에서 사용)
                _firstSegmentVelocity = currentVelocity;
                
                // 부드러운 회전 보간
                firstSegment.transform.rotation = Quaternion.Slerp(
                    firstSegment.transform.rotation,
                    headRotation,
                    followRatio * 1.5f // 회전은 약간 더 빠르게
                );
            }

            // 나머지 세그먼트 업데이트 (이전 세그먼트를 따라감)
            for (int i = 1; i < _bodySegments.Count; i++)
            {
                GameObject segment = _bodySegments[i];
                GameObject prevSegment = _bodySegments[i - 1];
                
                if (segment == null || prevSegment == null) continue;
                
                // 이전 세그먼트 정보 캐싱
                Vector3 prevPosition = prevSegment.transform.position;
                Quaternion prevRotation = prevSegment.transform.rotation;
                Vector3 prevForward = prevSegment.transform.forward;
                
                // 목표 위치 계산 (이전 세그먼트 뒤)
                Vector3 targetPosition = prevPosition - prevForward * _segmentSpacing;
                
                // 안전하게 속도 벡터에 접근
                if (i < _segmentVelocities.Count)
                {
                    // 현재 속도 벡터 가져오기
                    Vector3 currentVelocity = _segmentVelocities[i];
                    
                    // 부드러운 보간 (Vector3.SmoothDamp 대신 Lerp 사용하여 오류 방지)
                    segment.transform.position = Vector3.Lerp(
                        segment.transform.position,
                        targetPosition,
                        followRatio * 1.2f
                    );
                    
                    // 수정된 속도 벡터 저장
                    _segmentVelocities[i] = currentVelocity;
                }
                else
                {
                    // 인덱스 범위 밖이면 Lerp 사용
                    segment.transform.position = Vector3.Lerp(
                        segment.transform.position,
                        targetPosition,
                        followRatio
                    );
                }
                
                // 부드러운 회전 (고정 비율 사용)
                segment.transform.rotation = Quaternion.Slerp(
                    segment.transform.rotation,
                    prevRotation,
                    followRatio * 1.2f // 약간 더 빠른 회전
                );
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] 세그먼트 위치 업데이트 오류: {ex.Message}");
        }
    }
    
    // Body 세그먼트의 값을 업데이트하는 메서드
    private void UpdateBodyValues(int headValue)
    {
        if (_bodySegmentComponents == null || _bodySegmentComponents.Count == 0) return;

        try
        {
            // 머리 값의 지수 계산 (2의 몇 승인지)
            float log2HeadValue = Mathf.Log(headValue, 2);
            int headPower = (int)Mathf.Round(log2HeadValue);
            
            Debug.Log($"[{GetType().Name}] 머리 값: {headValue}, 지수: {headPower}, 세그먼트 수: {_bodySegmentComponents.Count}");

            // 각 세그먼트의 값 계산 및 설정
            for (int i = 0; i < _bodySegmentComponents.Count; i++)
            {
                SnakeBodySegment segment = _bodySegmentComponents[i];
                if (segment == null) continue;

                // 값 계산 - 머리 값의 2의 지수에서 위치에 따라 감소
                int segmentPower;
                int segmentValue;
                
                if (i == _bodySegmentComponents.Count - 1)
                {
                    // 마지막 세그먼트는 항상 2
                    segmentValue = 2;
                }
                else if (i == 0)
                {
                    // 첫 번째 세그먼트는 머리 값의 절반
                    segmentPower = headPower - 1;
                    segmentValue = Mathf.Max(2, (int)Mathf.Pow(2, segmentPower));
                }
                else
                {
                    // 중간 세그먼트는 2^(headPower-i-1)
                    segmentPower = headPower - i - 1;
                    segmentValue = Mathf.Max(2, (int)Mathf.Pow(2, segmentPower));
                }

                // 값 적용
                segment.SetValue(segmentValue);
                
                Debug.Log($"[{GetType().Name}] 세그먼트 #{i + 1} 값 설정: {segmentValue}");
            }

            // 클라이언트에 세그먼트 값 동기화
            SyncBodyValuesClientRpc();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] UpdateBodyValues 오류: {ex.Message}");
        }
    }
    
    // 모든 클라이언트에 Body 값 동기화 알림
    [ClientRpc]
    private void SyncBodyValuesClientRpc()
    {
        if (IsServer) return; // 서버는 이미 처리했으므로 제외
        
        if (_snake == null || _snake.Head == null || _bodySegmentComponents.Count == 0) return;
        
        // 클라이언트 측에서 동일한 로직으로 값 계산
        int headValue = _snake.Head.Value;
        float log2 = Mathf.Log(headValue, 2);
        int headPower = (int)Mathf.Round(log2);
        
        Debug.Log($"[{GetType().Name}] 클라이언트: 헤드 값={headValue}, 2^{headPower} 패턴으로 Body 값 업데이트");
        
        // 각 세그먼트 값 설정
        for (int i = 0; i < _bodySegmentComponents.Count; i++)
        {
            if (_bodySegmentComponents[i] == null) continue;
            
            // 세그먼트 값은 2^(headPower-i-1) 패턴 적용
            int segmentPower = headPower - i - 1;
            int segmentValue;
            
            // 마지막 세그먼트 또는 계산 값이 2보다 작을 경우 2로 설정
            if (i == _bodySegmentComponents.Count - 1 || segmentPower < 1)
            {
                segmentValue = 2; // 항상 마지막 또는 작은 값은 2로 설정
            }
            else
            {
                segmentValue = (int)Mathf.Pow(2, segmentPower);
            }
            
            // 값 설정
            _bodySegmentComponents[i].SetValue(segmentValue);
            Debug.Log($"[{GetType().Name}] 클라이언트: Body[{i}] 값={segmentValue}");
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
                _snakeNetworkHandler.UpdateScore(segmentComponent.Value);
                
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

    private int CalculateSegmentValue()
    {
        int headValue = _networkHeadValue.Value;
        float log2HeadValue = Mathf.Log(headValue, 2);
        int headPower = (int)Mathf.Round(log2HeadValue);
        
        int segmentPower = headPower - _snakeBodyHandler.GetBodySegmentCount() - 1;
        return segmentPower > 0 ? (int)Mathf.Pow(2, segmentPower) : 2;
    }

    [ClientRpc]
    private void NotifySegmentAddedClientRpc(int segmentIndex, int segmentValue)
    {
        if (IsServer) return; // 서버는 이미 처리함
        
        Debug.Log($"[{GetType().Name}] 클라이언트: 새 세그먼트 추가 알림 (인덱스: {segmentIndex}, 값: {segmentValue})");
        
        // 세그먼트가 생성되었는지 확인하고 값 설정
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
            // 방향 전환을 부드럽게 처리
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