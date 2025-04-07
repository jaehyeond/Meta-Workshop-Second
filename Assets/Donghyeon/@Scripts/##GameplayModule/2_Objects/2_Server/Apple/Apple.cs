using UnityEngine;
using Unity.Netcode;
using Unity.Assets.Scripts.Objects;
using VContainer;

/// <summary>
/// 2048 Snake 게임에서 Snake가 먹을 아이템
/// </summary>
public class Apple : BaseObject
{
    [SerializeField] private int _valueIncrement = 1;  // 먹었을 때 증가할 값
    [SerializeField] private float _rotationSpeed = 30f;  // 회전 속도
    [SerializeField] private Vector3 _rotationAxis = Vector3.up;  // 회전축
    
    // public 프로퍼티 추가: 외부에서 _valueIncrement 값을 읽을 수 있도록 함
    public int ValueIncrement => _valueIncrement;
    
    private bool _isCollided = false;  // 충돌 중복 처리 방지 (이제 사용되지 않을 수 있음)
    
    /// <summary>
    /// 매 프레임마다 사과를 회전시킵니다.
    /// </summary>
    private void Update()
    {
        transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime);
    }
    
    
    public void DespawnApple(Apple apple)
    {
        _objectManager.Despawn(apple);
    }
 
    /* // 주석 처리 시작: 기존 충돌 로직 비활성화
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
            // RequestNewApple();
            
            // 현재 사과 파괴
            Destroy(gameObject);
        }
    }
    */ // 주석 처리 끝
    
    // /// <summary>
    // /// AppleManager에게 새로운 사과를 스폰하도록 요청합니다.
    // /// </summary>
    // private void RequestNewApple()
    // {
    //     AppleManager appleManager = FindObjectOfType<AppleManager>();
    //     if (appleManager != null)
    //     {
    //         appleManager.SpawnAppleServerRpc();
    //     }
    // }
} 