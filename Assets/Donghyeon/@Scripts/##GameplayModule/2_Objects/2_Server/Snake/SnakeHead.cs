using UnityEngine;
using Unity.Netcode;

public class SnakeHead : MonoBehaviour
{
    private float _speed = 5f;
    private Quaternion _targetRotation;
    [SerializeField] private float _rotationSpeed = 10f; // 회전 속도 (Inspector에서 조절 가능)

    // Public getter for speed
    public float Speed => _speed;

    public void Construct(float speed) {
        _speed = speed;
        _targetRotation = transform.rotation; // 초기 목표 회전값 설정
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
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _rotationSpeed);

        MoveHead();
    }

    // 이동 메소드 (Update에서 호출되므로 deltaTime 사용)
    public void MoveHead()
    {
        var timeStep = Time.deltaTime * _speed;
        transform.Translate(transform.forward * timeStep, Space.World);
    }
}
