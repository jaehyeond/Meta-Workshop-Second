using System;
using UnityEngine;

public class JoystickInputProvider : MonoBehaviour, ISnakeInputProvider
{
    [SerializeField] private FixedJoystick _joystick; // 인스펙터에서 할당
    private Vector2 _direction;

    public Vector2 MovementDirection => _direction;
    
    public event Action<Vector2> OnMovementDirectionChanged;

    private void Update()
    {
        Vector2 newDirection = new Vector2(_joystick.Horizontal, _joystick.Vertical);
        
        // 방향이 변경되었는지 확인
        if (Vector2.Distance(newDirection, _direction) > 0.01f)
        {
            _direction = newDirection;
            OnMovementDirectionChanged?.Invoke(_direction);
        }
    }
} 