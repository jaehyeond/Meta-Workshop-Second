using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Unity.Assets.Scripts.Objects;

public class FoodMapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AppleManager _appleManager;
    [SerializeField] private RectTransform _minimapPanel;
    [SerializeField] private GameObject _indicatorPrefab;
    
    [Header("Settings")]
    [SerializeField] private float _updateInterval = 0.5f;
    [SerializeField] private bool _showFoodLabels = true;
    
    // 음식 유형별 색상 설정
    [System.Serializable]
    public class FoodTypeInfo
    {
        public string typeName;
        public Color color = Color.white;
        public Sprite icon;
    }
    
    [SerializeField] private List<FoodTypeInfo> _foodTypes = new List<FoodTypeInfo>();
    private Dictionary<string, FoodTypeInfo> _foodTypeMap = new Dictionary<string, FoodTypeInfo>();
    
    // 지표 UI 요소를 관리하기 위한 Dictionary
    private Dictionary<BaseObject, GameObject> _foodIndicators = new Dictionary<BaseObject, GameObject>();
    
    private float _timer;
    
    private void Awake()
    {
        // 음식 유형 맵 초기화
        foreach (var foodType in _foodTypes)
        {
            _foodTypeMap[foodType.typeName] = foodType;
        }
        
        // AppleManager 참조 검사
        if (_appleManager == null)
        {
            _appleManager = FindObjectOfType<AppleManager>();
            if (_appleManager == null)
            {
                Debug.LogError("FoodMapUI: AppleManager 참조를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }
    }
    
    private void Update()
    {
        // 일정 간격으로 UI 업데이트
        _timer += Time.deltaTime;
        if (_timer >= _updateInterval)
        {
            _timer = 0f;
            UpdateMinimapUI();
        }
    }
    
    private void UpdateMinimapUI()
    {
        // 모든 스폰된 음식 가져오기
        List<BaseObject> spawnedFood = _appleManager.GetSpawnedFood();
        
        // 현재 표시된 지표와 실제 음식 동기화
        
        // 1. 사라진 음식의 지표 제거
        List<BaseObject> foodToRemove = new List<BaseObject>();
        foreach (var kvp in _foodIndicators)
        {
            if (!spawnedFood.Contains(kvp.Key) || kvp.Key == null)
            {
                Destroy(kvp.Value);
                foodToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var food in foodToRemove)
        {
            _foodIndicators.Remove(food);
        }
        
        // 2. 새 음식에 지표 추가 및 기존 지표 위치 업데이트
        foreach (var food in spawnedFood)
        {
            if (food == null) continue;
            
            // 음식 유형 결정
            string foodType = DetermineFoodType(food);
            
            // 지표가 이미 있는지 확인
            if (_foodIndicators.TryGetValue(food, out GameObject indicator))
            {
                // 기존 지표 위치 업데이트
                UpdateIndicatorPosition(indicator, food);
            }
            else
            {
                // 새 지표 생성
                CreateFoodIndicator(food, foodType);
            }
        }
    }
    
    private string DetermineFoodType(BaseObject food)
    {
        // 음식 이름에서 유형 추출 (클론 제거)
        string name = food.name.Replace("(Clone)", "").Trim();
        
        // 기본 유형 검사
        if (name.Contains("Apple")) return "Apple";
        if (name.Contains("Beef")) return "Beef";
        if (name.Contains("Beer")) return "Beer";
        if (name.Contains("Candy")) return "Candy";
        
        return "Unknown";
    }
    
    private void CreateFoodIndicator(BaseObject food, string foodType)
    {
        // 지표 생성
        GameObject indicator = Instantiate(_indicatorPrefab, _minimapPanel);
        
        // 위치 설정
        UpdateIndicatorPosition(indicator, food);
        
        // 색상 및 아이콘 설정
        if (_foodTypeMap.TryGetValue(foodType, out FoodTypeInfo info))
        {
            Image image = indicator.GetComponent<Image>();
            if (image != null)
            {
                image.color = info.color;
                if (info.icon != null)
                {
                    image.sprite = info.icon;
                }
            }
            
            // 라벨 설정 (옵션)
            if (_showFoodLabels)
            {
                TextMeshProUGUI label = indicator.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = foodType;
                }
            }
        }
        
        // 딕셔너리에 추가
        _foodIndicators[food] = indicator;
    }
    
    private void UpdateIndicatorPosition(GameObject indicator, BaseObject food)
    {
        // 월드 좌표를 미니맵 좌표로 변환
        Vector2 minimapPos = WorldToMinimapPosition(food.transform.position);
        
        // 미니맵 내에 표시
        RectTransform rt = indicator.GetComponent<RectTransform>();
        rt.anchoredPosition = minimapPos;
    }
    
    private Vector2 WorldToMinimapPosition(Vector3 worldPos)
    {
        // 월드 좌표를 미니맵 범위로 스케일링
        float minimapWidth = _minimapPanel.rect.width;
        float minimapHeight = _minimapPanel.rect.height;
        
        // 스폰 영역의 지름을 미니맵 크기에 매핑
        float x = (worldPos.x + _appleManager._spawnAreaRadius) / (_appleManager._spawnAreaRadius * 2) * minimapWidth;
        float y = (worldPos.z + _appleManager._spawnAreaRadius) / (_appleManager._spawnAreaRadius * 2) * minimapHeight;
        
        return new Vector2(x, y);
    }
}