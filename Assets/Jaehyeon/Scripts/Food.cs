using UnityEngine;
using static Define;

public class Food : BaseObject
{
    [SerializeField] private TextMesh _numberText;
    [SerializeField] private MeshRenderer _renderer;
    
    public int Value { get; private set; }
    
    public override bool Init()
    {
        ObjectType = EObjectType.Food;
        return true;
    }
    
    public void SetInfo(int foodId)
    {
        Data.FoodData data = Managers.Data.FoodDic[foodId];
        Value = data.Value;
        
        // 텍스트 표시
        if (_numberText != null)
            _numberText.text = Value.ToString();
        
        // 색상 설정
        if (_renderer != null)
        {
            Color newColor;
            if (ColorUtility.TryParseHtmlString(data.Color, out newColor))
                _renderer.material.color = newColor;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
