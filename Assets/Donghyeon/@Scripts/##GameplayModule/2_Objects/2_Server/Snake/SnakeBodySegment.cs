using UnityEngine;
using TMPro;

/// <summary>
/// Snake의 몸통 세그먼트를 관리하는 클래스
/// </summary>
public class SnakeBodySegment : MonoBehaviour
{
    [SerializeField] private TextMeshPro _valueText;  // 값을 표시할 TextMeshPro
    
    private int _value = 2;  // 기본값
    
    /// <summary>
    /// 세그먼트의 값을 설정합니다.
    /// </summary>
    public void SetValue(int value)
    {
        _value = value;
        UpdateValueDisplay();
    }
    
    /// <summary>
    /// 현재 세그먼트의 값을 반환합니다.
    /// </summary>
    public int GetValue()
    {
        return _value;
    }
    
    /// <summary>
    /// 값 표시를 업데이트합니다.
    /// </summary>
    private void UpdateValueDisplay()
    {
        if (_valueText != null)
        {
            _valueText.text = _value.ToString();
        }
    }
} 