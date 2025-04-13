using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Assets.Scripts.Objects;

public class SnakeHead : BaseObject
{
    private float _speed = 5f;
    private Quaternion _targetRotation;
    [SerializeField] private float _rotationSpeed = 10f; // 회전 속도 (Inspector에서 조절 가능)
    [SerializeField] private TextMeshPro _valueText; // 값을 표시할 TextMeshPro 컴포넌트
    
    private int _currentValue = 2; // 기본 값은 2로 시작

    // Public getter for speed
    public float Speed => _speed;
    
    // Public getter for current value
    public int Value => _currentValue;

    public void Construct(float speed) {
        _speed = speed;
        _targetRotation = transform.rotation; // 초기 목표 회전값 설정
        UpdateValueDisplay(); // 초기 값 표시 업데이트
    }

    public void LookAt(Vector3 target)
    {
        var direction = target - transform.position;
        if (direction.sqrMagnitude < 0.001f)
            return;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        _targetRotation = transform.rotation; // 목표 회전값도 즉시 업데이트
    }

    public void SetTargetDirectionFromServer(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.001f)
        {
            _targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    public void ResetRotation() => 
        _targetRotation = transform.rotation;

    private void Update()
    {
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.fixedDeltaTime * _rotationSpeed);
        MoveHead();
    }

    // 이동 메소드 (FixedUpdate에서 호출되므로 fixedDeltaTime 사용)
    public void MoveHead()
    {
        var timeStep = Time.fixedDeltaTime * _speed;
        transform.Translate(transform.forward * timeStep, Space.World);
    }
    
    /// <summary>
    /// Snake의 현재 값을 업데이트합니다.
    /// </summary>
    /// <param name="value">증가시킬 값</param>
    public void UpdateValue(int increment)
    {
        _currentValue += increment;
        UpdateValueDisplay();
    }
    
    /// <summary>
    /// 현재 값으로 텍스트 표시를 업데이트합니다.
    /// </summary>
    private void UpdateValueDisplay()
    {
        if (_valueText != null)
        {
            _valueText.text = _currentValue.ToString();
        }
    }
    
    /// <summary>
    /// 현재 값을 특정 값으로 설정합니다.
    /// </summary>
    public void SetValue(int value)
    {
        _currentValue = value;
        UpdateValueDisplay();
    }
}
