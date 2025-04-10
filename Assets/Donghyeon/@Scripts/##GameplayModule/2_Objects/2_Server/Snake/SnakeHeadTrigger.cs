using UnityEngine;
using Unity.Netcode;
using VContainer;
using VContainer.Unity;

public class SnakeHeadTrigger : NetworkBehaviour // NetworkBehaviour 상속 (IsServer 사용 위함)
{
    [Header("References")]
    [SerializeField] private PlayerSnakeController _playerSnakeController; // 필수: PlayerSnakeController 참조
    [SerializeField] private SphereCollider _mouthCollider; // 필수: 충돌 감지용 Collider

    [Header("Settings")]
    [SerializeField, Range(0, 180)] private float _deathAngle = 100f; // 다른 스네이크와 충돌 시 죽음 판정 각도
    [SerializeField] private LayerMask _targetMask; // 충돌 감지 대상 레이어 마스크

    // 충돌 결과를 저장할 배열 (크기는 예상 최대 동시 충돌 수에 맞게 조절 가능)
    private readonly Collider[] _colliders = new Collider[5];
   
    private AppleManager _appleManager;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            Initialize();
        }
    }
    private void Initialize()
    {
        _appleManager = FindObjectOfType<LifetimeScope>()?.Container.Resolve<AppleManager>();
    }


    private void FixedUpdate()
    {
        // FixedUpdate 호출 확인
        // Debug.Log($"[{GetType().Name}] FixedUpdate - Is Server: {IsServer}"); 

        if (!IsServer) return;

        if (_playerSnakeController?._snake?.Head == null)
        {
            // 참조 누락 확인
            Debug.LogWarning($"[{GetType().Name}] PlayerSnakeController or Snake reference missing.");
            return;
        }
        // DetectCollisions 호출 직전 확인
        Debug.Log($"[{GetType().Name}] Calling DetectCollisions...");
        DetectCollisions();
    }

    private void DetectCollisions()
    {
        // NonAlloc 버전 사용으로 가비지 생성 최소화
        var hits = Physics.OverlapSphereNonAlloc(
            _mouthCollider.transform.position,
            _mouthCollider.radius,
            _colliders,
            _targetMask,
            QueryTriggerInteraction.Collide); // 트리거와의 충돌도 감지

        for (var i = 0; i < hits; i++)
        {
            ProcessCollision(_colliders[i]);
            _colliders[i] = null;
        }
    }

    // 감지된 충돌 처리 (서버에서 실행됨)
    private void ProcessCollision(Component target)
    {
        // PlayerSnakeController 및 Snake 참조 확인
        if (_playerSnakeController?._snake?.Head == null)
        {
            Debug.LogError($"[{GetType().Name}] PlayerSnakeController 또는 Snake 참조가 설정되지 않았습니다!");
            return;
        }
        SnakeHead currentHead = _playerSnakeController._snake.Head;

        // 1. Apple과 충돌 처리
        if (target.TryGetComponent(out Apple apple))
        {
            Debug.Log($"[{GetType().Name}] Apple 충돌 감지");

            HandleAppleCollision(apple);
        }
        else if (target.TryGetComponent(out SnakeHead otherHead))
        {            
            if (otherHead == currentHead) return;

            var angle = Vector3.Angle(currentHead.transform.forward, otherHead.transform.forward);
        }
            // 설정된 각도 이상이면 치명적 충돌로 간주
        //     if (angle > _deathAngle)
        //     {
        //         _playerSnakeController.HandleFatalCollision(target.gameObject);
        //     }
        //     else
        //     {
        //         // 비치명적 충돌 처리 (예: 밀어내기, 경고 등) - 필요 시 구현
        //          // _playerSnakeController.HandleNonFatalCollision(target.gameObject);
        //     }
        // }
        // // 3. 그 외 (_targetMask에 포함된 다른 Collider) 충돌 처리 (예: 벽, 장애물)
        // else
        // {
        //     _playerSnakeController.HandleFatalCollision(target.gameObject);
        // }
    }


        /// <summary>
    /// SnakeHeadTrigger에서 호출되어 Apple과의 충돌을 처리합니다. (서버 전용)
    /// </summary>
    /// <param name="apple">충돌한 Apple 컴포넌트</param>
    public void HandleAppleCollision(Apple apple)
    {
        if (!IsServer) return; // 서버에서만 실행

        int valueIncrement = apple.ValueIncrement; // 수정: 실제 Apple의 값 사용

        Debug.Log($"[{GetType().Name}] Apple 충돌 처리 (서버): 값 +{valueIncrement}");

        // 헤드 값 증가 TODO
        if (IsServer)
        {
            _playerSnakeController._networkHeadValue.Value += valueIncrement;
        }

        NetworkObject appleNetObj = apple.GetComponent<NetworkObject>();
        if (appleNetObj != null)
        {
            apple.DespawnApple(apple);
            // _playerSnakeController.DestroyObjectClientRpc(appleNetObj.NetworkObjectId);
            // appleNetObj.Despawn(true);
        }
       _appleManager.SpawnApple();

    }

    public void HandleFatalCollision(GameObject collidedObject)
    {
        if (!IsServer) return; // 서버에서만 실행

        Debug.LogWarning($"[{GetType().Name}] 치명적 충돌 감지 (서버): 충돌 대상 = {collidedObject.name}");

        // TODO: 게임 규칙에 따른 추가 처리 (예: 상대방 점수 증가 등)
        // if (collidedObject.TryGetComponent<PlayerSnakeController>(out var otherSnake)) { ... }

        // 스네이크 죽음 처리
        // Die();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // 사과와 충돌
        if (other.CompareTag("Apple"))
        {
            Debug.Log($"[{GetType().Name}] OnTriggerEnter - 사과와 충돌 감지");

            try
            {
                // AppleManager 확인
                if (_appleManager == null)
                {
                    var lifetimeScope = FindObjectOfType<LifetimeScope>();
                    if (lifetimeScope != null)
                    {
                        _appleManager = lifetimeScope.Container.Resolve<AppleManager>();
                        Debug.Log($"[{GetType().Name}] AppleManager 재설정 완료");
                    }
                }

                // Apple 컴포넌트 확인
                Apple apple = other.GetComponent<Apple>();
                if (apple != null)
                {
                    // 기존의 HandleAppleCollision 메서드 호출
                    HandleAppleCollision(apple);
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] 충돌 객체에 Apple 컴포넌트가 없습니다!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] 사과 충돌 처리 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }
        // 다른 스네이크와 충돌 처리는 여기에 구현 (필요시)
    }
} 
