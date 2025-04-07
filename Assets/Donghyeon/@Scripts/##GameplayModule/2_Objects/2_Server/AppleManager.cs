using UnityEngine;
using Unity.Netcode;
using VContainer;
using Unity.Assets.Scripts.Resource;  // 이 줄을 추가해야 합니다

/// <summary>
/// Apple 오브젝트 생성 및 관리를 담당하는 클래스
/// </summary>
public class AppleManager : NetworkBehaviour
{
    [SerializeField] private int _initialAppleCount = 1;  // 초기 생성할 사과 개수
    [SerializeField] private float _spawnAreaRadius = 10f;  // 스폰 영역 반경
    [SerializeField] private float _spawnHeight = 0.5f;  // 스폰 높이
    
    [Inject] private ResourceManager _resourceManager;  // 리소스 매니저 주입
    
    private GameObject _applePrefab;  // 사과 프리팹
    
    /// <summary>
    /// 네트워크 스폰 시 초기화 작업을 수행합니다.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsServer) return;
        
        // 사과 프리팹 로드
        _applePrefab = _resourceManager.Instantiate("Apple");

        if (_applePrefab == null)
        {
            Debug.LogError("Apple 프리팹을 로드할 수 없습니다!");
            return;
        }
        
        // 초기 사과 생성
        for (int i = 0; i < _initialAppleCount; i++)
        {
            SpawnAppleServerRpc();
        }
    }
    
    /// <summary>
    /// 서버에서 사과를 생성하는 RPC 메서드
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SpawnAppleServerRpc()
    {
        if (!IsServer) return;
        
        Vector3 spawnPosition = GetRandomPosition();
        GameObject appleObject = Instantiate(_applePrefab, spawnPosition, Quaternion.identity);
        
        // 네트워크 스폰
        NetworkObject networkObject = appleObject.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
        }
        else
        {
            Debug.LogError("Apple 오브젝트에 NetworkObject 컴포넌트가 없습니다!");
            Destroy(appleObject);
        }
    }
    
    /// <summary>
    /// 랜덤한 스폰 위치를 반환합니다.
    /// </summary>
    private Vector3 GetRandomPosition()
    {
        // XZ 평면에서 랜덤한 위치 계산
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(0f, _spawnAreaRadius);
        
        float x = Mathf.Cos(angle) * distance;
        float z = Mathf.Sin(angle) * distance;
        
        return new Vector3(x, _spawnHeight, z);
    }
} 