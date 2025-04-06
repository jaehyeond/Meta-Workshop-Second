using System;
using UnityEngine;
using Unity.Assets.Scripts.UI;

namespace Donghyeon.GameplayModule.Input
{
    // 조이스틱 컨트롤을 위한 인터페이스
    public interface ISnakeInputProvider
    {
        Vector2 MovementDirection { get; }
        event Action<Vector2> OnMovementDirectionChanged;
    }

    // 조이스틱 입력 제공자 클래스
    public class JoystickInputProvider : MonoBehaviour, ISnakeInputProvider
    {
        [SerializeField] private UI_Joystick joystick; // 인스펙터에서 할당
        private Vector2 direction;

        // UI_Joystick 자동 찾기
        private void Awake()
        {
            if (joystick == null)
            {
                joystick = FindObjectOfType<UI_Joystick>();
                if (joystick == null)
                {
                    Debug.LogWarning("UI_Joystick을 찾을 수 없습니다.");
                }
            }
        }

        public Vector2 MovementDirection => direction;
        
        public event Action<Vector2> OnMovementDirectionChanged;

        private void Update()
        {
            if (joystick == null) return; // 조이스틱이 없으면 무시
            
            // 조이스틱의 방향 가져오기
            Vector2 newDirection = Vector2.zero;
            
            if (joystick != null)
            {
                // 직접 UI_Joystick의 상태로부터 방향 계산
                newDirection = GetDirectionFromJoystick(joystick);
            }
            
            // 방향이 변경되었는지 확인
            if (Vector2.Distance(newDirection, direction) > 0.01f)
            {
                direction = newDirection;
                OnMovementDirectionChanged?.Invoke(direction);
            }
        }
        
        // UI_Joystick으로부터 직접 방향 계산
        private Vector2 GetDirectionFromJoystick(UI_Joystick joystickComponent)
        {
            // 조이스틱의 커서와 배경 위치 가져오기
            Transform bgTrans = joystickComponent.transform.Find("JoystickBG");
            Transform cursorTrans = joystickComponent.transform.Find("JoystickCursor");
            
            if (bgTrans == null || cursorTrans == null)
                return Vector2.zero;
                
            // 커서와 배경의 위치 차이로 방향 계산
            Vector2 bgPos = bgTrans.position;
            Vector2 cursorPos = cursorTrans.position;
            
            return (cursorPos - bgPos).normalized;
        }
    }

    // UI_Joystick 확장 메서드 - 직접 위치로부터 방향 계산
    public static class UI_JoystickExtensions
    {
        public static Vector2 GetJoystickDirection(this UI_Joystick joystick)
        {
            // 조이스틱의 커서와 배경을 찾아 방향 계산
            Transform bgTrans = joystick.transform.Find("JoystickBG");
            Transform cursorTrans = joystick.transform.Find("JoystickCursor");
            
            if (bgTrans == null || cursorTrans == null)
                return Vector2.zero;
                
            // 커서와 배경의 위치 차이로 방향 계산
            Vector2 bgPos = bgTrans.position;
            Vector2 cursorPos = cursorTrans.position;
            
            return (cursorPos - bgPos).normalized;
        }
    }
} 