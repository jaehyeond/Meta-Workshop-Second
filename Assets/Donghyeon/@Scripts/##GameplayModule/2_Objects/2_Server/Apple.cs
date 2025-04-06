using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 2048 Snake 게임에서 Snake가 먹을 아이템
/// </summary>
public class Apple : NetworkBehaviour
{
    [SerializeField] private int _valueIncrement = 1;  // 먹었을 때 증가할 값
    [SerializeField] private float _rotationSpeed = 30f;  // 회전 속도
    [SerializeField] private Vector3 _rotationAxis = Vector3.up;  // 회전축
    
    private bool _isCollided = false;  // 충돌 중복 처리 방지
    
    /// <summary>
    /// 매 프레임마다 사과를 회전시킵니다.
    /// </summary>
    private void Update()
    {
        transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// Snake와 충돌했을 때 처리합니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (_isCollided) return;
        
        // 서버에서만 처리
        if (!IsServer) return;
        
        PlayerSnakeController snakeController = other.GetComponent<PlayerSnakeController>();
        if (snakeController != null)
        {
            _isCollided = true;
            
            // Snake의 값 증가
            snakeController.IncreaseHeadValue(_valueIncrement);
            
            // 새 사과 생성 요청
            RequestNewApple();
            
            // 현재 사과 파괴
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// AppleManager에게 새로운 사과를 스폰하도록 요청합니다.
    /// </summary>
    private void RequestNewApple()
    {
        AppleManager appleManager = FindObjectOfType<AppleManager>();
        if (appleManager != null)
        {
            appleManager.SpawnAppleServerRpc();
        }
    }
} 