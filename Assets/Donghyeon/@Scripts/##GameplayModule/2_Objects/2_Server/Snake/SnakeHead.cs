using UnityEngine;
using TMPro;

public class SnakeHead : MonoBehaviour
{
    [SerializeField] private TextMeshPro valueText;
    private float _speed;
    private Quaternion _targetRotation;
    private int _currentValue = 0;

    public void Construct(float speed) => 
        _speed = speed;

    public void LookAt(Vector3 target)
    {
        var direction = target - transform.position;
        _targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = _targetRotation;
    }

    public void ResetRotation() => 
        _targetRotation = transform.rotation;

    private void Update()
    {
        MoveHead();
    }

    private void MoveHead()
    {
        var timeStep = Time.deltaTime * _speed;
        transform.Translate(transform.forward * timeStep, Space.World);
    }
    
    // 값 업데이트 메서드 추가
    public void UpdateValue(int value)
    {
        _currentValue = value;
        
        // TextMeshPro가 있으면 값 표시
        if (valueText != null)
        {
            valueText.text = value > 0 ? value.ToString() : "";
        }
    }
    
    // 현재 값 반환
    public int GetValue()
    {
        return _currentValue;
    }
}
