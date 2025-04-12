using UnityEngine;
using TMPro;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;

/// <summary>
/// Snake의 몸통 세그먼트를 관리하는 클래스
/// </summary>
public class SnakeBodySegment : BaseObject
{
    [SerializeField] private TextMeshPro _valueText;  // 값을 표시할 TextMeshPro
    private readonly NetworkVariable<int> _value = new(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // private int _value = 2;  // 기본값

    public int Value
    {
        get => _value.Value;
        set
        {
            _value.Value = value;
            UpdateValueDisplay();
        }
    }
    
    /// <summary>
    /// 세그먼트의 값을 설정합니다.
    /// </summary>
    public void SetValue(int value)
    {
        _value.Value = value;
        UpdateValueDisplay();
    }
    
    /// <summary>
    /// 현재 세그먼트의 값을 반환합니다.
    /// </summary>
    public int GetValue()
    {
        return _value.Value;
    }
    
    /// <summary>
    /// 값 표시를 업데이트합니다.
    /// </summary>
    private void UpdateValueDisplay()
    {
        if (_valueText != null)
        {
            _valueText.text = _value.Value.ToString();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[Client SPAWN] Segment {NetworkObjectId}: SpawnPos={transform.position}");
        // NetworkVariable 값 변경 구독 등 필요한 로직 추가
    }
} 