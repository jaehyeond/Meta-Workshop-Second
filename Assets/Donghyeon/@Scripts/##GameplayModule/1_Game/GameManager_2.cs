
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static Define;

//게임 전체의 상태를 관리하는데 일반게임안에서 의 상태가아닌  이용시간, 게임을통해 얻은 유저에 SAVE될 데이터 등 게임 전체의 상태를 관리하는 매니저
// 뭐 있을까 이용시간, damage, 판 수 ,  dataID,  경험치  , 
// inputClientServer 

[Serializable]
public class GameSaveData
{
	public int Wood = 0;
	public int Mineral = 0;
	public int Meat = 0;
	public int Gold = 0;

	public List<HeroSaveData> Heroes = new List<HeroSaveData>(); // 이용자 DB에 넣을 데이터 
}

[Serializable]
public class HeroSaveData
{
	public int DataId = 0;
	public int Level = 1;
	public int Exp = 0;
	public HeroOwningState OwningState = HeroOwningState.Unowned; // 이용자가 기존에 영웅을 가지고잇엇는지 없엇는지 그리고 근데 ??? 흠... 디펜스에 가미해야하나 영웅만의 데이터터
}

public enum HeroOwningState
{
	Unowned,
	Owned,
	Picked,
}



public class GameManager2 : NetworkBehaviour
{
	GameSaveData _saveData = new GameSaveData();
	public GameSaveData SaveData { get { return _saveData; } set { _saveData = value; } }


	public int Wood
	{
		get { return _saveData.Wood; }
		private set
		{
			_saveData.Wood = value;
		}
	}

	public int Mineral
	{
		get { return _saveData.Mineral; }
		private set
		{
			_saveData.Mineral = value;
		}
	}

	public int Meat
	{
		get { return _saveData.Meat; }
		private set
		{
			_saveData.Meat = value;
		}
	}

	public int Gold
	{
		get { return _saveData.Gold; }
		private set
		{
			_saveData.Gold = value;
		}
	}

	public List<HeroSaveData> AllHeroes { get { return _saveData.Heroes; } }
	public int TotalHeroCount { get { return _saveData.Heroes.Count; } }
	public int UnownedHeroCount { get { return _saveData.Heroes.Where(h => h.OwningState == HeroOwningState.Unowned).Count(); } }
	public int OwnedHeroCount { get { return _saveData.Heroes.Where(h => h.OwningState == HeroOwningState.Owned).Count(); } }
	public int PickedHeroCount { get { return _saveData.Heroes.Where(h => h.OwningState == HeroOwningState.Picked).Count(); } }


    #region Action
    public event Action<Vector2> OnMoveDirChanged;
    public event Action<Define.EJoystickState> OnJoystickStateChanged;
    public event Action<bool> OnInputModeChanged;
    #endregion


    #region Hero
    private Vector2 _moveDir;
    public Vector2 MoveDir
    {
        get { return _moveDir; }
        set
        {
            _moveDir = value;
            OnMoveDirChanged?.Invoke(value);
        }
    }

    private Define.EJoystickState _joystickState;
    public Define.EJoystickState JoystickState
    {
        get { return _joystickState; }
        set
        {
            _joystickState = value;
            OnJoystickStateChanged?.Invoke(_joystickState);
        }
    }
    
    // 클릭 모드 상태
    private bool _isClickMode = true;
    public bool IsClickMode
    {
        get { return _isClickMode; }
        set 
        { 
            _isClickMode = value;
            // UI 컨트롤러에게 모드 변경 알림
            if (OnInputModeChanged != null)
                OnInputModeChanged.Invoke(_isClickMode);
        }
    }
    
    // 플레이어 위치 반환 (클릭 방향 계산에 사용)
    public Vector3 GetPlayerPosition()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            return player.transform.position;
        return Vector3.zero;
    }
    #endregion


}

