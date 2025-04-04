using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class GameSaveData
{
	public int Wood = 0;
	public int Mineral = 0;
	public int Meat = 0;
	public int Gold = 0;

	public List<HeroSaveData> Heroes = new List<HeroSaveData>();
}

[Serializable]
public class HeroSaveData
{
	public int DataId = 0;
	public int Level = 1;
	public int Exp = 0;
	public HeroOwningState OwningState = HeroOwningState.Unowned;
}

public enum HeroOwningState
{
	Unowned,
	Owned,
	Picked,
}

public class GameManager
{
	#region GameData
	GameSaveData _saveData = new GameSaveData();
	public GameSaveData SaveData { get { return _saveData; } set { _saveData = value; } }




	#endregion




	#region Save & Load	
	public string Path { get { return Application.persistentDataPath + "/SaveData.json"; } }

	public void InitGame()
	{
		if (File.Exists(Path))
			return;

		var heroes = Managers.Data.HeroDic.Values.ToList();
		foreach (HeroData hero in heroes)
		{
			HeroSaveData saveData = new HeroSaveData()
			{
				DataId = hero.DataId,
			};

			SaveData.Heroes.Add(saveData);
		}

		// TEMP
		SaveData.Heroes[0].OwningState = HeroOwningState.Picked;
		SaveData.Heroes[1].OwningState = HeroOwningState.Owned;
	}

	public void SaveGame()
	{
		string jsonStr = JsonUtility.ToJson(Managers.Game.SaveData);
		File.WriteAllText(Path, jsonStr);
		Debug.Log($"Save Game Completed : {Path}");
	}

	public bool LoadGame()
	{
		if (File.Exists(Path) == false)
			return false;

		string fileStr = File.ReadAllText(Path);
		GameSaveData data = JsonUtility.FromJson<GameSaveData>(fileStr);

		if (data != null)
			Managers.Game.SaveData = data;

		Debug.Log($"Save Game Loaded : {Path}");
		return true;
	}
	#endregion

	#region Action
	#endregion
}
