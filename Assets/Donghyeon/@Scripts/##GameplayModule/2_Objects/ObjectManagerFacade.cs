using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;
using VContainer;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;
using System;
using Unity.Assets.Scripts.Resource;
using Unity.Assets.Scripts.Scene;
using Unity.Netcode.Components;
using System.Linq;
using Unity.Assets.Scripts.Data;


public class ObjectManagerFacade : NetworkBehaviour
{   
    // protected로 되어 있지만 외부에서 직접 설정할 수 있도록 유지
    // [Inject] public ObjectManager _objectManager;
    [Inject] private NetworkManager _networkManager;
    [Inject] public NetUtils _netUtils;
    [Inject] private INetworkMediator _networkMediator;
    [Inject] public ResourceManager _resourceManager;

    [Inject] private DebugClassFacade _debugClassFacade;
    [Inject] private IObjectResolver Container;


    private Coroutine _spawnMonsterCoroutine;
    private GameObject ObjectSpawner_O;
    RateLimitCooldown m_RateLimitQuery;
    private bool _isDestroyed = false;

    // 기본 생성자 추가
  
    public void Awake()
    {
        _debugClassFacade?.LogInfo(GetType().Name, "[ObjectManagerFacade] Awake 호출됨");
    }
  
    public void Initialize()
    {
        _isDestroyed = false;
        _debugClassFacade?.LogInfo(GetType().Name, "[ObjectManagerFacade] 초기화됨");
    }
    
    public void Load()
    {
        if (_isDestroyed) return;
        
        // _networkMediator.RegisterHandler(NetworkEventType.NetworkSpawned, OnNetworkObjectSpawned);
        ObjectSpawner_O = this.gameObject;
        m_RateLimitQuery = new RateLimitCooldown(3f);
        _debugClassFacade?.LogInfo(GetType().Name, "[ObjectManagerFacade] 로드됨");
    }
 
    private void OnDestroy()
    {
        _isDestroyed = true;
        if (_spawnMonsterCoroutine != null)
        {
            StopCoroutine(_spawnMonsterCoroutine);
            _spawnMonsterCoroutine = null;
        }
        _debugClassFacade?.LogInfo(GetType().Name, "[ObjectManagerFacade] 파괴됨");
    }


    // 아이템 스폰 요청 메서드 (게임 내 다른 곳에서 호출)
    // 이 메서드 자체는 클라이언트에서도 호출될 수 있지만, 실제 스폰은 서버에서 이루어져야 함
    public void RequestSpawnItem(string itemName, Vector3 position)
    {
        if (IsServer)
        {
            // 서버라면 즉시 스폰 실행
            SpawnItemInternal(itemName, position);
        }
        else
        {
            // 클라이언트라면 서버에 스폰 요청
            RequestSpawnItemServerRpc(itemName, position);
        }
    }

    [ServerRpc(RequireOwnership = false)] // 모든 클라이언트가 호출 가능
    private void RequestSpawnItemServerRpc(string itemName, Vector3 position, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"서버: 클라이언트 {rpcParams.Receive.SenderClientId}로부터 아이템 '{itemName}' 스폰 요청 받음");
        SpawnItemInternal(itemName, position);
    }

    // 실제 스폰 로직 (서버에서만 실행됨)
    private void SpawnItemInternal(string itemName, Vector3 position)
    {
        if (!IsServer) return; // 이중 확인

        try
        {
            // 1. 리소스 매니저를 통해 아이템 프리팹 로드 (비동기 로딩 권장)
            GameObject itemPrefab = _resourceManager.Load<GameObject>($"Prefabs/Items/{itemName}"); // 경로 예시

            if (itemPrefab == null)
            {
                Debug.LogError($"[ObjectManagerFacade] 아이템 프리팹 로드 실패: {itemName}");
                return;
            }

            // 2. 프리팹 인스턴스화
            GameObject itemInstance = Instantiate(itemPrefab, position, Quaternion.identity);

            // 3. NetworkObject 컴포넌트 가져오기
            NetworkObject networkObject = itemInstance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError($"[ObjectManagerFacade] 아이템 프리팹에 NetworkObject가 없습니다: {itemName}");
                Destroy(itemInstance); // 불필요한 인스턴스 제거
                return;
            }

            // 4. (선택사항) 스폰 전 초기 설정 (예: 아이템 종류, 값 등)
            // ItemController itemController = itemInstance.GetComponent<ItemController>();
            // if (itemController != null)
            // {
                // itemController.InitializeData(...);
            // }

            // 5. 네트워크 스폰!
            // networkObject.Spawn(true); // true: 씬 전환 시 파괴

            Debug.Log($"[ObjectManagerFacade] 아이템 스폰 성공: {itemName} at {position}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ObjectManagerFacade] 아이템 스폰 중 오류: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // 아이템 제거 로직도 유사하게 구현 가능 (RequestDespawn -> ServerRpc -> DespawnInternal)
    public void RequestDespawnObject(ulong networkObjectId)
    {
         if (IsServer)
         {
             DespawnObjectInternal(networkObjectId);
         }
         else
         {
              RequestDespawnObjectServerRpc(networkObjectId);
         }
    }

     [ServerRpc(RequireOwnership = false)]
     private void RequestDespawnObjectServerRpc(ulong networkObjectId)
     {
         DespawnObjectInternal(networkObjectId);
     }

     private void DespawnObjectInternal(ulong networkObjectId)
     {
         if (!IsServer) return;

         if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
         {
              if (networkObject != null) // 추가 확인
              {
                  networkObject.Despawn(true); // true: 즉시 파괴
                  Debug.Log($"[ObjectManagerFacade] 네트워크 오브젝트 Despawn 성공: ID={networkObjectId}");
              }
         }
         else
         {
             Debug.LogWarning($"[ObjectManagerFacade] Despawn 하려는 네트워크 오브젝트를 찾을 수 없음: ID={networkObjectId}");
         }
     }
}


