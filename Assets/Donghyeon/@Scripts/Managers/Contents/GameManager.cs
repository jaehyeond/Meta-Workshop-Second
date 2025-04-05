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

	private float _foodSpawnTimer = 0f;
	private float _foodSpawnInterval = 3f;
	private float _gameAreaSize = 20f;
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
	public void InitSnakeGame()
	{
		// 게임 초기화
		InitGame();
		
		// 뱀 생성 - 오브젝트 매니저를 통해 생성
		Managers.Object.Spawn<SnakeController>(Vector3.zero, 1); // Snake DataId 1번 사용
		
		// 초기 음식 생성
		for (int i = 0; i < 5; i++)
			SpawnFood();
	}

	public void SpawnFood()
	{
		// 가중치 기반 음식 ID 선택
		int foodId = GetRandomWeightedFoodId();
		
		// 랜덤 위치 계산
		Vector3 position = new Vector3(
			Random.Range(-_gameAreaSize/2, _gameAreaSize/2),
			0.5f, // 높이
			Random.Range(-_gameAreaSize/2, _gameAreaSize/2)
		);
		
		// 음식 생성
		Managers.Object.Spawn<Food>(position, foodId);
	}

	private int GetRandomWeightedFoodId()
	{
		List<Data.FoodData> foods = new List<Data.FoodData>(Managers.Data.FoodDic.Values);
		float totalWeight = 0;
		
		foreach (var food in foods)
			totalWeight += food.SpawnWeight;
		
		float randomValue = Random.Range(0, totalWeight);
		float weightSum = 0;
		
		foreach (var food in foods)
		{
			weightSum += food.SpawnWeight;
			if (randomValue <= weightSum)
				return food.DataId;
		}
		
		return foods[0].DataId; // 기본값
	}

	// Update 메서드에 음식 생성 로직 추가
	private void Update()
	{
		_foodSpawnTimer += Time.deltaTime;
		
		if (_foodSpawnTimer >= _foodSpawnInterval)
		{
			_foodSpawnTimer = 0;
			SpawnFood();
		}
	}
	#endregion
}
