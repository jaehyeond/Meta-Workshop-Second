using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Apple 클래스 - 2048 Snake 게임의 아이템
/// </summary>
public class Apple : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _valueIncrement = 2; // Snake가 획득 시 증가할 값
    [SerializeField] private float _rotationSpeed = 50f; // 회전 속도
    [SerializeField] private Vector3 _rotationAxis = Vector3.up; // 회전 축
    
    private bool _isCollided = false; // 충돌 중복 처리 방지용
    
    // Update에서 회전 효과 적용
    private void Update()
    {
        if (!IsSpawned) return;
        
        // 애플 회전 효과
        transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime);
    }
    
    // 충돌 감지
    private void OnTriggerEnter(Collider other)
    {
        // 서버에서만 처리 & 이미 충돌 처리된 경우 무시
        if (!IsServer || _isCollided) return;
        
        Debug.Log($"[Apple] 충돌 감지: {other.name}");
        
        // PlayerSnakeController 확인
        PlayerSnakeController snakeController = other.GetComponent<PlayerSnakeController>();
        if (snakeController != null)
        {
            _isCollided = true;
            
            // Snake의 Head 값 증가 메서드 호출
            snakeController.IncreaseHeadValueServerRpc(_valueIncrement);
            
            // 새 Apple 생성 요청
            RequestNewApple();
            
            // 현재 Apple 제거
            GetComponent<NetworkObject>().Despawn();
        }
    }
    
    // 새 Apple 생성 요청
    private void RequestNewApple()
    {
        AppleManager appleManager = FindObjectOfType<AppleManager>();
        if (appleManager != null)
        {
            appleManager.SpawnAppleServerRpc();
        }
        else
        {
            Debug.LogWarning("[Apple] AppleManager를 찾을 수 없습니다.");
        }
    }
} 