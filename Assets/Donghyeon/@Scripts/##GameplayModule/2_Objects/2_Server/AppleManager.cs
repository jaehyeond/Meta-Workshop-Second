using UnityEngine;
using Unity.Netcode;
using VContainer;
using Unity.Assets.Scripts.Resource;

/// <summary>
/// AppleManager 클래스 - Apple 생성 및 관리
/// </summary>
public class AppleManager : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _initialAppleCount = 1; // 게임 시작 시 생성할 Apple 수
    [SerializeField] private float _spawnAreaRadius = 10f; // 스폰 영역 반경
    [SerializeField] private float _spawnHeight = 0.5f; // 스폰 높이
    
    [Inject] private ResourceManager _resourceManager; // 리소스 매니저
    
    private string _applePrefabPath = "Prefabs/Snake/Apple"; // Apple 프리팹 경로
    private GameObject _applePrefab;
    
    // 네트워크 스폰 시 호출
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 서버에서만 실행
        if (IsServer)
        {
            // Apple 프리팹 로드
            _applePrefab = _resourceManager.Load<GameObject>(_applePrefabPath);
            
            if (_applePrefab == null)
            {
                Debug.LogError("[AppleManager] Apple 프리팹을 로드할 수 없습니다: " + _applePrefabPath);
                return;
            }
            
            // 초기 Apple 생성
            for (int i = 0; i < _initialAppleCount; i++)
            {
                SpawnAppleServerRpc();
            }
        }
    }
    
    // Apple 스폰 (서버 전용)
    [ServerRpc(RequireOwnership = false)]
    public void SpawnAppleServerRpc()
    {
        if (!IsServer) return;
        
        if (_applePrefab == null)
        {
            Debug.LogError("[AppleManager] Apple 프리팹이 null입니다.");
            return;
        }
        
        Vector3 spawnPosition = GetRandomPosition();
        GameObject apple = Instantiate(_applePrefab, spawnPosition, Quaternion.identity);
        
        NetworkObject netObj = apple.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(); // 네트워크에 스폰
            Debug.Log($"[AppleManager] Apple 스폰 완료: {spawnPosition}");
        }
        else
        {
            Debug.LogError("[AppleManager] 생성된 Apple에 NetworkObject 컴포넌트가 없습니다.");
            Destroy(apple);
        }
    }
    
    // 랜덤 위치 생성
    private Vector3 GetRandomPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(0f, _spawnAreaRadius);
        
        float x = radius * Mathf.Cos(angle);
        float z = radius * Mathf.Sin(angle);
        
        return new Vector3(x, _spawnHeight, z);
    }
} 