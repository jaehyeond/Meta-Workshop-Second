// using UnityEngine;
// using Unity.Netcode;
// using VContainer;
// using Unity.Assets.Scripts.Resource;  // 이 줄을 추가해야 합니다
// using Unity.Assets.Scripts.Objects; // BaseObject 타입 참조를 위해 추가
// using VContainer.Unity;

// /// <summary>
// /// Apple 오브젝트 생성 및 관리를 담당하는 클래스
// /// </summary>
// public class AppleManager : MonoBehaviour
// {
//     [SerializeField] private int _initialAppleCount = 100;  // 초기 생성할 사과 개수
//     [SerializeField] private float _spawnAreaRadius = 10f;  // 스폰 영역 반경
//     [SerializeField] private float _spawnHeight = 0.0f;  // 스폰 높이

//     // 스폰할 프리팹 이름들을 인스펙터에서 설정할 수 있는 배열 추가
//     [SerializeField] private string[] _spawnablePrefabNames = { "Apple", "Beer", "Beef", "Candy" };

//     [Inject] private ObjectManager _objectManager;

//     public void SpawnInitialApples()
//     {
//         Debug.Log($"[{GetType().Name}] Spawning initial {_initialAppleCount} objects...");
//         for (int i = 0; i < _initialAppleCount; i++)
//         {
//             // 프리팹 목록에서 랜덤하게 하나를 선택하여 스폰
//             string randomPrefab = _spawnablePrefabNames[Random.Range(0, _spawnablePrefabNames.Length)];
//             SpawnObject(randomPrefab);
//         }
//     }

//     public void SpawnApple()
//     {
//         // 프리팹 목록에서 랜덤하게 하나를 선택하여 스폰
//         string randomPrefab = _spawnablePrefabNames[Random.Range(0, _spawnablePrefabNames.Length)];
//         SpawnObject(randomPrefab);
//     }

//     public void SpawnObject(string prefabName)
//     {
//         Debug.Log($"[{GetType().Name}] Spawning {prefabName}...");
//         Vector3 spawnPosition = GetRandomPosition();

//         _objectManager.Spawn<BaseObject>(spawnPosition, prefabName: prefabName);
//     }

//     private Vector3 GetRandomPosition()
//     {
//         // XZ 평면에서 랜덤한 위치 계산
//         float angle = Random.Range(0f, Mathf.PI * 2f);
//         float distance = Random.Range(0f, _spawnAreaRadius);
        
//         float x = Mathf.Cos(angle) * distance;
//         float z = Mathf.Sin(angle) * distance;
        
//         return new Vector3(x, _spawnHeight, z);
//     }
//    // Apple 파괴를 위한 ClientRpc 추가

   
 
// } 

using UnityEngine;
using Unity.Netcode;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Unity.Assets.Scripts.Objects;
using VContainer.Unity;
using System.Collections.Generic;
using System.Linq;

public class AppleManager : MonoBehaviour
{
    [SerializeField] private int _initialAppleCount = 100;
    [SerializeField] public float _spawnAreaRadius = 10f;
    [SerializeField] private float _spawnHeight = 0.0f;
    [SerializeField] private string[] _spawnablePrefabNames = { "Apple", "Beer", "Beef", "Candy" };
    
    // 스폰된 모든 음식 객체 추적
    private List<BaseObject> _spawnedFood = new List<BaseObject>();
    
    [Inject] private ObjectManager _objectManager;
    
    // UI 관련 참조
    [SerializeField] private RectTransform _minimapContainer;
    [SerializeField] private GameObject _foodIndicatorPrefab;
    
    // 미니맵 스케일 (월드 좌표 -> 미니맵 좌표 변환용)
    [SerializeField] private float _minimapScale = 0.1f;
    
    // 음식 타입별 색상 (선택 사항)
    [System.Serializable]
    public class FoodTypeColor
    {
        public string prefabName;
        public Color color = Color.white;
    }
    
    [SerializeField] private List<FoodTypeColor> _foodColors = new List<FoodTypeColor>();
    private Dictionary<string, Color> _foodColorMap = new Dictionary<string, Color>();
    
    private void Awake()
    {
        // 색상 맵 초기화
        foreach (var foodColor in _foodColors)
        {
            _foodColorMap[foodColor.prefabName] = foodColor.color;
        }
    }
    
    public void SpawnInitialApples()
    {
        Debug.Log($"[{GetType().Name}] Spawning initial {_initialAppleCount} objects...");
        for (int i = 0; i < _initialAppleCount; i++)
        {
            string randomPrefab = _spawnablePrefabNames[Random.Range(0, _spawnablePrefabNames.Length)];
            SpawnObject(randomPrefab);
        }
    }
    
    public void SpawnApple()
    {
        string randomPrefab = _spawnablePrefabNames[Random.Range(0, _spawnablePrefabNames.Length)];
        SpawnObject(randomPrefab);
    }
    
    public BaseObject SpawnObject(string prefabName)
    {
        Debug.Log($"[{GetType().Name}] Spawning {prefabName}...");
        Vector3 spawnPosition = GetRandomPosition();
        
        BaseObject newFood = _objectManager.Spawn<BaseObject>(spawnPosition, prefabName: prefabName);
        
        if (newFood != null)
        {
            // 새 음식 객체 추적 리스트에 추가
            _spawnedFood.Add(newFood);
            
            // UI 표시 메서드 호출 (구현 방식에 따라 다름)
            if (IsServer)
            {
                UpdateFoodUI();
            }
        }
        
        return newFood;
    }
    
    public void RemoveFood(BaseObject food)
    {
        if (_spawnedFood.Contains(food))
        {
            _spawnedFood.Remove(food);
            
            // UI 업데이트
            if (IsServer)
            {
                UpdateFoodUI();
            }
        }
    }
    
    private Vector3 GetRandomPosition()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(0f, _spawnAreaRadius);
        
        float x = Mathf.Cos(angle) * distance;
        float z = Mathf.Sin(angle) * distance;
        
        return new Vector3(x, _spawnHeight, z);
    }
    
    // 서버에서 실행되는지 확인
    private bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    
    // UI 업데이트 메서드 (미니맵 방식의 예)
    private void UpdateFoodUI()
    {
        // 이 메서드는 실제 구현에 따라 다를 수 있음
        // 예: 미니맵 업데이트, 월드 스페이스 UI 배치 등
    }
    
    // 음식 위치의 미니맵 좌표 계산 (미니맵 구현 시 사용)
    public Vector2 WorldToMinimapPosition(Vector3 worldPos)
    {
        // 예시: 간단한 비례 변환
        return new Vector2(worldPos.x * _minimapScale, worldPos.z * _minimapScale);
    }

        public List<BaseObject> GetSpawnedFood()
    {
        // 삭제된 항목을 필터링한 새 리스트 반환
        return _spawnedFood.Where(food => food != null).ToList();
    }
}