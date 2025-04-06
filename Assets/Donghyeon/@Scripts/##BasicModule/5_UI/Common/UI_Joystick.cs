using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using VContainer.Unity;
using static Define;

public class UI_Joystick : UI_Base
{
    enum GameObjects
    {
        JoystickBG,
        JoystickCursor,
    }
    private GameManager _gameManager;

    private GameObject _background;
    private GameObject _cursor;
    private float _radius;
    private Vector2 _touchPos;
    private bool _isClickMode = false;  // 기본은 조이스틱 모드

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        _gameManager = FindObjectOfType<LifetimeScope>()?.Container.Resolve<GameManager>();

        if (_gameManager == null)
        {
            Debug.LogError("UI_Joystick에서 GameManager를 Resolve할 수 없습니다!");
            // 또는 ProjectContext에서 직접 Resolve 시도
            // _gameManager = ProjectContext.Instance.Container.Resolve<GameManager>();
        }

        
        BindObjects(typeof(GameObjects));
        _background = GetObject((int)GameObjects.JoystickBG);
        _cursor = GetObject((int)GameObjects.JoystickCursor);
        _radius = _background.GetComponent<RectTransform>().sizeDelta.y / 5;

        gameObject.BindEvent(OnPointerDown, type: Define.EUIEvent.PointerDown);
        gameObject.BindEvent(OnPointerUp, type: Define.EUIEvent.PointerUp);
        gameObject.BindEvent(OnDrag, type: Define.EUIEvent.Drag);
        gameObject.BindEvent(OnClick, type: Define.EUIEvent.Click);
        
        return true;
    }

    // 모드 전환 함수
    public void ToggleInputMode(bool clickMode)
    {
        _isClickMode = clickMode;
    }

    #region Event
    public void OnPointerDown(PointerEventData eventData)
    {
        _touchPos = eventData.position;
        
        if (!_isClickMode)
        {
            // 조이스틱 모드
            _background.transform.position = eventData.position;
            _cursor.transform.position = eventData.position;
        }
        if(_gameManager == null)
        {
            Debug.LogError("GameManager가 존재하지 않습니다.");
        }
         _gameManager.JoystickState = EJoystickState.PointerDown;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isClickMode)
        {
            // 조이스틱 모드
            _cursor.transform.position = _touchPos;
        }
        
        _gameManager.MoveDir = Vector2.zero;
        _gameManager.JoystickState = EJoystickState.PointerUp;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isClickMode)
            return;  // 클릭 모드에서는 드래그 무시

        // 조이스틱 모드 드래그 처리
        Vector2 touchDir = (eventData.position - _touchPos);
        float moveDist = Mathf.Min(touchDir.magnitude, _radius);
        Vector2 moveDir = touchDir.normalized;
        Vector2 newPosition = _touchPos + moveDir * moveDist;
        _cursor.transform.position = newPosition;
        _gameManager.MoveDir = moveDir;
        _gameManager.JoystickState = EJoystickState.Drag;
    }

    public void OnClick(PointerEventData eventData)
    {
        if (!_isClickMode)
            return;  // 조이스틱 모드에서는 클릭 무시

        // 클릭 모드: 클릭 위치로 이동 방향 계산
        // Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(_gameManager.GetPlayerPosition());
        // Vector2 clickDir = (eventData.position - playerScreenPos).normalized;
        
        // _gameManager.MoveDir = clickDir;
        // _gameManager.JoystickState = EJoystickState.Drag;  // 기존 상태 재활용
    }
    #endregion
}
