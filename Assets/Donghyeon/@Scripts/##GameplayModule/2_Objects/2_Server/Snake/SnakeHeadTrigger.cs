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
        if (IsServer)
        {
            _appleManager = FindObjectOfType<LifetimeScope>()?.Container.Resolve<AppleManager>();
        }
    }

    private void FixedUpdate()
    {
        // 충돌 감지는 소유자 클라이언트에서만 수행
        if (!IsOwner) return;

        if (_playerSnakeController?._snake?.Head == null || _mouthCollider == null)
        {
            // 참조 누락 확인 (Owner에서만)
            // Debug.LogWarning($"[{GetType().Name}] Owner: PlayerSnakeController, Snake, or Collider reference missing.");
            return;
        }
        DetectCollisions();
    }

    private void DetectCollisions()
    {
        var hits = Physics.OverlapSphereNonAlloc(
            _mouthCollider.transform.position,
            _mouthCollider.radius,
            _colliders,
            _targetMask,
            QueryTriggerInteraction.Collide);

        for (var i = 0; i < hits; i++)
        {
            // 충돌 처리 (Owner에서 호출됨)
            ProcessCollision(_colliders[i]);
            _colliders[i] = null; // 배열 재사용을 위해 클리어
        }
    }

    // 감지된 충돌 처리 (Owner에서 실행됨)
    private void ProcessCollision(Component target)
    {
        if (_playerSnakeController == null) return; // PlayerSnakeController 참조 확인

        // 1. Apple과 충돌 처리
        if (target.TryGetComponent(out Apple apple))
        {
            Debug.Log($"[{GetType().Name} Owner] Apple 충돌 감지. 서버에 알림 전송.");
            // 서버에 사과 먹었음을 알리는 RPC 호출 (PlayerSnakeController에 추가될 예정)
            // _playerSnakeController.NotifyAppleEatenServerRpc(apple.ValueIncrement);
            // 중요: 클라이언트에서는 사과를 직접 비활성화하거나 파괴하지 않습니다.
            //       서버가 RPC를 통해 상태를 변경하고, NetworkObject가 Despawn되면 클라이언트에서도 사라집니다.
        }
        // 2. 다른 스네이크 머리/몸통과 충돌 처리 (필요시 여기에 로직 추가 - Owner에서 감지)
        // else if (target.TryGetComponent(out SnakeHead otherHead)) { ... }
        // else if (target.TryGetComponent(out SnakeBodySegment otherSegment)) { ... }
        // 3. 그 외 충돌 처리 (벽 등)
        // else { ... }
    }

    // HandleAppleCollision 메서드는 PlayerSnakeController의 ServerRpc로 로직이 이동되므로 제거하거나 주석 처리
    /*
    public void HandleAppleCollision(Apple apple)
    {
        // ... 기존 서버 로직 ...
    }
    */

     // HandleFatalCollision 등 다른 충돌 처리 메서드도 필요시 Owner 감지 -> ServerRpc 요청 방식으로 수정
     /*
    public void HandleFatalCollision(GameObject collidedObject)
    {
       // if (!IsServer) return; // 제거 또는 IsOwner로 변경 고려
       // Debug.LogWarning($"[{GetType().Name}] 치명적 충돌 감지...");
       // _playerSnakeController.NotifyFatalCollisionServerRpc(collidedObject); // 예시 RPC 호출
    }
    */

    // OnTriggerEnter는 물리 이벤트이므로 모든 클라이언트/서버에서 호출될 수 있음.
    // FixedUpdate의 OverlapSphere로 감지하므로 이 메서드는 불필요하거나,
    // 다른 용도로 사용한다면 IsOwner 또는 IsServer 체크 필요.
    /*
    private void OnTriggerEnter(Collider other)
    {
        // ...
    }
    */
} 