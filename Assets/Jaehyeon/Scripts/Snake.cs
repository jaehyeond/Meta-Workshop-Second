using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Vector3 = UnityEngine.Vector3;

public class Snake : BaseObject
{
    [Header("Core Components")]
    [field: SerializeField] public SnakeHead Head { get; private set; }
    [field: SerializeField] public SnakeBody Body { get; private set; }
    [SerializeField] private PlayerSnakeController _playerSnakeController; // 필수: PlayerSnakeController 참조
    [SerializeField] private SphereCollider _mouthCollider; // 필수: 충돌 감지용 Collider
    private readonly Collider[] _colliders = new Collider[5];
    [SerializeField] private LayerMask _targetMask; // 충돌 감지 대상 레이어 마스크

    public NetworkVariable<int> _networkHeadValue = new(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[{GetType().Name}] _networkHeadValue.Value: {_networkHeadValue.Value}");
        Debug.Log($"[{GetType().Name}] isServer: {IsServer} isClient: {IsClient} isOwner: {IsOwner}");

        
        Debug.Log($"[{GetType().Name}] OnNetworkSpawn");
        if (IsServer)
        {
            SetInfo();
        }

        // 머리 오브젝트에 ClientNetworkTransform 컴포넌트 추가 (없는 경우)
        // if (Head != null && Head.GetComponent<Unity.Netcode.Components.ClientNetworkTransform>() == null)
        // {
        //     Head.gameObject.AddComponent<Unity.Netcode.Components.ClientNetworkTransform>();
        //     Debug.Log($"[{GetType().Name}] Head 오브젝트에 ClientNetworkTransform 컴포넌트 추가됨");
        // }

        // PlayerSnakeController 참조 확인 및 설정
        if (_playerSnakeController == null)
        {
            _playerSnakeController = GetComponentInParent<PlayerSnakeController>();
            if (_playerSnakeController == null)
            {
                Debug.LogError($"[{GetType().Name}] OnNetworkSpawn: PlayerSnakeController 참조를 찾을 수 없습니다!");
            }
            else
            {
                Debug.Log($"[{GetType().Name}] OnNetworkSpawn: PlayerSnakeController 참조를 찾았습니다.");
            }
        }

        // 클라이언트에서 값 변경 감지 구독
        if (IsClient)
        {
            // 초기 값 동기화가 필요할 수 있으므로, 구독 전에 현재 값으로 로컬 상태 업데이트 고려
            // 예: HandleHeadValueChanged_ServerSide(0, _networkHeadValue.Value); // 필요시 초기값 강제 처리
            _networkHeadValue.OnValueChanged += HandleHeadValueChanged_ServerSide;
        }

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsClient)
        {
            _networkHeadValue.OnValueChanged -= HandleHeadValueChanged_ServerSide;

        }
    }


    private void HandleHeadValueChanged_ServerSide(int previousValue, int newValue)
    {
        // _playerSnakeController가 null인지 확인
        if (_playerSnakeController == null)
        {
            Debug.LogError($"[{GetType().Name}] HandleHeadValueChanged_ServerSide: _playerSnakeController가 null입니다!");
            return; // null이면 메서드 종료
        }

        float log2Value = Mathf.Log(newValue, 2);
        if (Mathf.Approximately(log2Value, Mathf.Round(log2Value)))
        {
            // 소유자인 경우에만 ServerRpc 호출
            if (IsOwner)
            {
                _playerSnakeController.NotifyHeadValueChangedServerRpc(previousValue, newValue);
            }
            else
            {
                Debug.Log($"[{GetType().Name}] HandleHeadValueChanged_ServerSide: 소유자가 아니므로 ServerRpc를 호출하지 않습니다. 값 변경: {previousValue} -> {newValue}");
            }
        }
    }


    private void SetInfo()
    {
        Debug.Log($"[{GetType().Name}] SetInfo");
        int initialHeadValue = 2;
        float _initialSnakeSpeed = 5f;
        Head.Construct(_initialSnakeSpeed);
        _networkHeadValue.Value = initialHeadValue;

    }
    private void FixedUpdate()
    {
        // 충돌 감지는 소유자 클라이언트에서만 수행
        if (!IsOwner) return;

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
        // PlayerSnakeController 참조가 없으면 가져오기 시도 (필요하다면 Awake나 OnNetworkSpawn에서 처리하는 것이 더 효율적일 수 있음)
        if (_playerSnakeController == null)
        {
             PlayerSnakeController controller = FindObjectOfType<PlayerSnakeController>();
             _playerSnakeController = controller;
        }

        if (target.TryGetComponent(out Apple apple))
        {
            Debug.Log($"[{GetType().Name} Owner] Apple 충돌 감지. 서버에 알림 전송.");
            if (_playerSnakeController != null && apple != null)
            {
                // 1번째 인자: 사과의 값 (int)
                int appleValue = apple.ValueIncrement;
                
                // 2번째 인자: 사과의 NetworkObjectId (ulong)
                if (apple.TryGetComponent<NetworkObject>(out var appleNetObj))
                {    
                    ulong appleNetworkId = appleNetObj.NetworkObjectId;
                    
                    // 서버에 사과 먹었음을 알림 (사과 값과 NetworkObjectId 포함)
                    _playerSnakeController.NotifyAppleEatenServerRpc(appleValue, appleNetworkId);
                    
                    // 로컬에서 사과를 임시로 비활성화 (시각적 피드백을 위해)
                    apple.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] 충돌한 사과에서 NetworkObject 컴포넌트를 찾을 수 없습니다!");
                }
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] PlayerSnakeController 또는 충돌한 Apple이 null입니다!");
            }
        }
    }

    [ClientRpc]
    private void SyncHeadPositionClientRpc(Vector3 position, Quaternion rotation)
    {
        // 호스트나 소유자는 이미 직접 처리하므로 제외
        if (IsOwner || IsServer) return;
        
        // 다른 클라이언트들에서만 실행 (소유자가 아닌 경우)
        if (Head != null)
        {
            Head.transform.position = position;
            Head.transform.rotation = rotation;
        }
    }

    // private List<SnakeBodySegment> _bodySegmentComponents = new List<SnakeBodySegment>();




    public void SetHeadTargetDirection(Vector3 direction)
    {
        if (Head != null)
        {
            Head.SetTargetDirectionFromServer(direction);
        }
    }

    public void AddDetail(GameObject detail) => Body?.AddDetail(detail);
    public GameObject RemoveDetail() => Body?.RemoveLastDetail();
    public void ResetRotation() => Head?.ResetRotation();

    public IEnumerable<Vector3> GetBodyDetailPositions()
    {
        if (Head != null) yield return Head.transform.position;
        if (Body != null)
        {
            foreach (var detailPosition in Body.GetBodyDetailPositions())
                yield return detailPosition;
        }
    }
}
