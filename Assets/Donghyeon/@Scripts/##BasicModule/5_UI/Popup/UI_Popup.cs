using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Popup : UI_Base
{    

	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		if (uiManager != null)
		{
			uiManager.SetCanvas(gameObject, true);
		}
		else
		{
			Debug.LogWarning($"<color=yellow>[{GetType().Name}] uiManager가 null입니다. DI가 제대로 설정되지 않았을 수 있습니다.</color>");
		}

		return true;
	}

	public virtual void ClosePopupUI()
	{
		uiManager.ClosePopupUI(this);
	}
}
