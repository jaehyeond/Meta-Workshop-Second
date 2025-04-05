using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Assets.Scripts.Scene;
using VContainer;

public abstract class BaseScene : InitBase
{
    [Inject] public SceneManagerEx _sceneManager;

	public EScene SceneType { get; protected set; } = EScene.Unknown;

	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		Object obj = GameObject.FindAnyObjectByType(typeof(EventSystem));
		if (obj == null)
		{
			GameObject go = new GameObject() { name = "@EventSystem" };
			go.AddComponent<EventSystem>();
			go.AddComponent<StandaloneInputModule>();
		}

		return true;
	}

	public abstract void Clear();
}
