using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Unity.Assets.Scripts.Objects;

public static class Extension
{

    private static string LoadPrefab(string mapName)
    {
        Debug.Log($"[MapSpawnerFacade] 맵 로드 시도: '{mapName}'");
        
        // 맵 프리팹 로드 (확장자 처리)
        string resourceKey = mapName.EndsWith(".prefab") ? mapName.Replace(".prefab", "") : mapName;
        return resourceKey;
    }


	public static void BindEvent(this GameObject go, Action<PointerEventData> action = null, Define.EUIEvent type = Define.EUIEvent.Click)
	{
		UI_Base.BindEvent(go, action, type);
	}

	// public static bool IsValid(this GameObject go)
	// {
	// 	return go != null && go.activeSelf;
	// }

	public static bool IsValid(this BaseObject bo)
	{
		if (bo == null || bo.isActiveAndEnabled == false)
			return false;

		Creature creature = bo as Creature;
		if (creature != null)
			return creature.CreatureState != ECreatureState.Dead;

		return true;
	}

	// public static void MakeMask(this ref LayerMask mask, List<Define.ELayer> list)
	// {
	// 	foreach (Define.ELayer layer in list)
	// 		mask |= (1 << (int)layer);
	// }

	// public static void AddLayer(this ref LayerMask mask, Define.ELayer layer)
	// {
	// 	mask |= (1 << (int)layer);
	// }

	// public static void RemoveLayer(this ref LayerMask mask, Define.ELayer layer)
	// {
	// 	mask &= ~(1 << (int)layer);
	// }

	// public static void DestroyChilds(this GameObject go)
	// {
	// 	foreach (Transform child in go.transform)
	// 		Managers.Resource.Destroy(child.gameObject);
	// }

	public static void Shuffle<T>(this IList<T> list)
	{
		int n = list.Count;

		while (n > 1)
		{
			n--;
			int k = UnityEngine.Random.Range(0, n + 1);
			(list[k], list[n]) = (list[n], list[k]); //swap
		}
	}
}
