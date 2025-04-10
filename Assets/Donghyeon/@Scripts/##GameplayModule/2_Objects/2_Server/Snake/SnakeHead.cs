using UnityEngine;
using Unity.Netcode;
using TMPro;

public class SnakeHead : MonoBehaviour
{
    // PlayerSnakeController에서 이동 관련 설정 가져옴
    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f; // 이동 속도
    [SerializeField] private float _rotationSpeed = 180f; // 회전 속도 (기존 PlayerSnakeController 값 사용)
    [SerializeField] private float _movementSmoothing = 0.05f; // 이동 부드러움 (필요 시 사용)

    [Header("Display")]
    [SerializeField] private TextMeshPro _valueText; // 값을 표시할 TextMeshPro 컴포넌트

    private Quaternion _targetRotation; // 목표 회전값
    private int _currentValue = 2; // 기본 값은 2로 시작
    private bool _isMoving = true; // 이동 활성화 여부 (필요 시 추가)

    // Public getter for speed
    public float Speed => _speed;

    // Public getter for current value
    public int Value => _currentValue;

    // 생성자 역할 메서드 (속도 및 초기 상태 설정)
    public void Construct(float initialSpeed)
    {
        _speed = initialSpeed;
        _targetRotation = transform.rotation; // 초기 목표 회전값 설정
        UpdateValueDisplay(); // 초기 값 표시 업데이트
        _isMoving = true; // 초기 이동 상태 활성화
    }

    // 목표 지점을 바라보도록 즉시 회전 (서버 초기화 등)
    public void LookAt(Vector3 target)
    {
        var direction = target - transform.position;
        if (direction.sqrMagnitude < 0.001f)
            return;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        _targetRotation = transform.rotation; // 목표 회전값도 즉시 업데이트
    }

    // 서버로부터 받은 목표 방향으로 부드럽게 회전하도록 설정
    public void SetTargetDirectionFromServer(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.001f)
        {
            _targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    // 현재 회전값을 목표 회전값으로 초기화 (예: 충돌 후)
    public void ResetRotation() =>
        _targetRotation = transform.rotation;

    private void Update()
    {
        // 목표 회전값으로 부드럽게 회전
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _rotationSpeed);

        // 이동 활성화 상태일 때만 전진
        if (_isMoving)
        {
            MoveForward();
        }
    }

    // 전방으로 이동하는 로직 (Update에서 호출)
    private void MoveForward()
    {
        // Time.deltaTime을 사용하여 프레임 속도에 관계없이 일정한 속도로 이동
        var timeStep = Time.deltaTime * _speed;
        transform.Translate(transform.forward * timeStep, Space.World);
    }

    // 이동 상태 제어 (예: 게임 시작/종료 시)
    public void SetMovement(bool canMove)
    {
        _isMoving = canMove;
    }

    // 값을 증가시키는 메서드 (이전 UpdateValue)
    public void AddValue(int increment)
    {
        _currentValue += increment;
        UpdateValueDisplay();
    }

    // 특정 값으로 설정하는 메서드
    public void SetValue(int value)
    {
        _currentValue = value;
        UpdateValueDisplay();
    }

    // 현재 값으로 텍스트 표시를 업데이트
    private void UpdateValueDisplay()
    {
        if (_valueText != null)
        {
            _valueText.text = _currentValue.ToString();
        }
    }
}
