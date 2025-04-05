// Assets/Jaehyeon/Scripts/SnakeBody.cs
using UnityEngine;
using static Define;

public class SnakeBody : BaseObject
{
    [SerializeField] private TextMesh _numberText;
    [SerializeField] private MeshRenderer _renderer;
    
    private int _value;
    
    public override bool Init()
    {
        ObjectType = EObjectType.SnakeBody;
        return true;
    }
    
    public void SetValue(int value)
    {
        _value = value;
        
        // 숫자 텍스트 표시
        if (_numberText != null)
            _numberText.text = _value.ToString();
        
        // 색상 및 크기 설정
        Data.SnakeData snakeData = Managers.Data.SnakeDic[1]; // 기본 Snake 데이터 사용
        // Data.SnakeColorData colorData = GetColorDataForValue(_value, snakeData);
        
        // if (colorData != null && _renderer != null)
        // {
        //     Color newColor;
        //     if (ColorUtility.TryParseHtmlString(colorData.Color, out newColor))
        //         _renderer.material.color = newColor;
            
        //     // 몸통은 헤드보다 약간 작게
        //     transform.localScale = Vector3.one * colorData.SizeMultiplier * 0.8f;
        // }
    }
    
    // private Data.SnakeColorData GetColorDataForValue(int value, Data.SnakeData snakeData)
    // {
    //     Data.SnakeColorData bestMatch = null;
        
    //     foreach (var colorData in snakeData.ColorSettings)
    //     {
    //         if (colorData.Number <= value && (bestMatch == null || colorData.Number > bestMatch.Number))
    //             bestMatch = colorData;
    //     }
        
    //     return bestMatch;
    // }
}