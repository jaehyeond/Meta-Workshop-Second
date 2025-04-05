using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

public class UI_Upgrade_Popup : UI_Popup
{	

	enum GameObjects
	{
		// CloseArea,

	}

	enum Texts
	{

	}

	enum Buttons
	{
		Close_B,
	}



	const int MAX_ITEM_COUNT = 100;

	// Awake에서 자동으로 Init이 호출되지 않도록 InitBase의 Awake 로직을 무시하고 Init이 직접 호출될 때만 초기화하도록 수정
	private void Awake()
	{
		// 부모 클래스의 Awake에서 자동으로 Init을 호출하는 것을 방지
		// Init은 DI가 완료된 후에 ShowPopupUI에서 SetInfo가 호출될 때 실행됨
	}

	public override bool Init()
	{
		// 이미 초기화된 경우 중복 초기화 방지
		if (_init)
			return false;

		if (base.Init() == false)
			return false;

		gameObject.SetActive(true);

		BindObjects(typeof(GameObjects));
		BindTexts(typeof(Texts));
		BindButtons(typeof(Buttons));

		// GetObject((int)GameObjects.CloseArea).BindEvent(OnClickCloseArea);
		GetButton((int)Buttons.Close_B).gameObject.BindEvent(OnClickCloseButton);


		Refresh();

		return true;
	}

	public void SetInfo()
	{
		// DI가 완료된 후 호출되므로 여기서 초기화
		if (!_init)
		{
			Init();
		}
		Refresh();
	}

	void Refresh()
	{
		if (_init == false)
			return;
			
		// uiManager가 null인지 확인
		if (uiManager == null)
		{
			Debug.LogError($"<color=red>[{GetType().Name}] uiManager가 null입니다. DI가 제대로 설정되지 않았습니다.</color>");
			return;
		}
			
		// if()
		// GetText((int)Texts.EquippedHeroesCountText).text = $"{Managers.Game.PickedHeroCount} / ??";
		// GetText((int)Texts.WaitingHeroesCountText).text = $"{Managers.Game.OwnedHeroCount} / ??";
		// GetText((int)Texts.UnownedHeroesCountText).text = $"{Managers.Game.UnownedHeroCount} / ??";

		// Refresh_Hero(_equippedHeroes, HeroOwningState.Picked);
		// Refresh_Hero(_waitingHeroes, HeroOwningState.Owned);
		// Refresh_Hero(_unownedHeroes, HeroOwningState.Unowned);
	}

	// void Refresh_Hero(List<UI_HeroesList_HeroItem> list, HeroOwningState owningState)
	// {
	// 	List<HeroSaveData> heroes = Managers.Game.AllHeroes.Where(h => h.OwningState == owningState).ToList();

	// 	for (int i = 0; i < list.Count; i++)
	// 	{
	// 		if (i < heroes.Count)
	// 		{
	// 			HeroSaveData hero = heroes[i];
	// 			list[i].SetInfo(hero.DataId);
	// 			list[i].gameObject.SetActive(true);
	// 		}
	// 		else
	// 		{
	// 			list[i].gameObject.SetActive(false);
	// 		}
	// 	}
	// }

	void OnClickCloseArea(PointerEventData evt)
	{
		uiManager.ClosePopupUI(this);
	}

	void OnClickCloseButton(PointerEventData evt)
	{
		uiManager.ClosePopupUI(this);
	}
}
