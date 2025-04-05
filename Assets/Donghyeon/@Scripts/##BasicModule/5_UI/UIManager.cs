using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;
using Unity.Assets.Scripts.Resource;

public class UIManager
{

	[Inject] private DebugClassFacade _debugFacade;
	[Inject] private ResourceManager _resourceManager;
	[Inject] public IObjectResolver _container;

	// 싱글톤 인스턴스
	// private static UIManager s_Instance;
	// public static UIManager Instance
	// {
	// 	get
	// 	{
	// 		if (s_Instance == null)
	// 		{
	// 			s_Instance = new UIManager();		
	// 		}
	// 		return s_Instance;
	// 	}
	// }

	private int _order = 10;

	private Stack<UI_Popup> _popupStack = new Stack<UI_Popup>();

	private UI_Scene _sceneUI = null;
	public UI_Scene SceneUI
	{
		set { _sceneUI = value; }
		get { return _sceneUI; }
	}

	public GameObject Root
	{
		get
		{
			GameObject root = GameObject.Find("@UI_Root");
			if (root == null)
				root = new GameObject { name = "@UI_Root" };
			return root;
		}
	}


	public virtual void SubscribeEvents()
	{
		_debugFacade.LogInfo(GetType().Name, "이벤트 구독 시작");

	}

	public virtual void UnsubscribeEvents()
	{
		_debugFacade.LogInfo(GetType().Name, "이벤트 구독 해제");

	}
	



	public void SetCanvas(GameObject go, bool sort = true, int sortOrder = 0)
	{
		Canvas canvas = Util.GetOrAddComponent<Canvas>(go);
		if (canvas == null)
		{
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.overrideSorting = true;
		}

		CanvasScaler cs = Util.GetOrAddComponent<CanvasScaler>(go);
		if (cs != null)
		{
			cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			cs.referenceResolution = new Vector2(1080, 1920);
		}

		Util.GetOrAddComponent<GraphicRaycaster>(go);

		if (sort)
		{
			canvas.sortingOrder = _order;
			_order++;
		}
		else
		{
			canvas.sortingOrder = sortOrder;
		}
	}

	public T GetSceneUI<T>() where T : UI_Base
	{
		return _sceneUI as T;
	}

	public T MakeWorldSpaceUI<T>(Transform parent = null, string name = null) where T : UI_Base
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = _resourceManager.Instantiate($"{name}");
		if (parent != null)
			go.transform.SetParent(parent);

		Canvas canvas = Util.GetOrAddComponent<Canvas>(go);
		canvas.renderMode = RenderMode.WorldSpace;
		canvas.worldCamera = Camera.main;

		return Util.GetOrAddComponent<T>(go);
	}

	public T MakeSubItem<T>(Transform parent = null, string name = null, bool pooling = true) where T : UI_Base
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = _resourceManager.Instantiate(name, parent, pooling);
		go.transform.SetParent(parent);

		return Util.GetOrAddComponent<T>(go);
	}


	public T ShowBaseUI<T>(string name = null) where T : UI_Base
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		_debugFacade.LogInfo(GetType().Name, $"UI 표시: {name}");

		GameObject go = _resourceManager.Instantiate(name);
		T baseUI = Util.GetOrAddComponent<T>(go);

		go.transform.SetParent(Root.transform);

		return baseUI;
	}

	public T ShowSceneUI<T>(string name = null) where T : UI_Scene
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go =  _resourceManager.Instantiate(name);
		T sceneUI = Util.GetOrAddComponent<T>(go);
		_sceneUI = sceneUI;

		go.transform.SetParent(Root.transform);

		return sceneUI;
	}

	public T ShowPopupUI<T>(string name = null) where T : UI_Popup
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = _resourceManager.Instantiate(name);
		T popup = Util.GetOrAddComponent<T>(go);
		
		// VContainer를 통해 의존성 주입
		_container.Inject(popup);
		
		_popupStack.Push(popup);
		go.transform.SetParent(Root.transform);

		return popup;
	}

	public void ClosePopupUI(UI_Popup popup)
	{
		if (_popupStack.Count == 0)
			return;

		if (_popupStack.Peek() != popup)
		{
			Debug.Log("Close Popup Failed!");
			return;
		}

		ClosePopupUI();
	}

	public void ClosePopupUI()
	{
		if (_popupStack.Count == 0)
			return;

		UI_Popup popup = _popupStack.Pop();
		_resourceManager.Destroy(popup.gameObject);
		_order--;
	}

	public void CloseAllPopupUI()
	{
		while (_popupStack.Count > 0)
			ClosePopupUI();
	}

	public int GetPopupCount()
	{
		return _popupStack.Count;
	}

	public void Clear()
	{
		CloseAllPopupUI();
		_sceneUI = null;
	}


	// UIManager 클래스에 아래 메서드 추가
	public T FindPopup<T>() where T : UI_Popup
	{
		// Stack은 직접 순회가 불가능하므로 배열로 변환
		UI_Popup[] popups = _popupStack.ToArray();
		
		// 열려있는 모든 팝업 중에서 T 타입의 팝업 찾기
		foreach (UI_Popup popup in popups)
		{
			if (popup is T typedPopup)
			{
				return typedPopup;
			}
		}
		
		return null;
	}

	// FindPopup 메서드의 오버로드 버전 - 이름으로 찾기
	public UI_Popup FindPopupByName(string popupName)
	{
		// Stack은 직접 순회가 불가능하므로 배열로 변환
		UI_Popup[] popups = _popupStack.ToArray();
		
		// 열려있는 모든 팝업 중에서 이름이 일치하는 팝업 찾기
		foreach (UI_Popup popup in popups)
		{
			if (popup.gameObject.name.Contains(popupName))
			{
				return popup;
			}
		}
		
		return null;
	}

}
