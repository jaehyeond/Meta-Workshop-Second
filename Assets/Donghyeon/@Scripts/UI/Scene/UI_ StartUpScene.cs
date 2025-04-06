using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using static Define;

public class UI_StartUpScene : UI_Scene
{
    enum GameObjects
    {
        StartImage
    }

    enum Texts
    {
        DisplayText
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindObjects(typeof(GameObjects));
        BindTexts(typeof(Texts));

		GetObject((int)GameObjects.StartImage).BindEvent((evt) =>
		{
			Debug.Log("ChangeScene");
			Managers.Scene.LoadScene(EScene.GameScene);
		});

		GetObject((int)GameObjects.StartImage).gameObject.SetActive(false);
		GetText((int)Texts.DisplayText).text = $"StartUpScene";

		StartLoadAssets();

		// Snake 게임 초기화 메서드 호출

		return true;
    }

	void StartLoadAssets()
	{
		Managers.Resource.LoadAllAsync<Object>("PreLoad", (key, count, totalCount) =>
		{
			Debug.Log($"{key} {count}/{totalCount}");

			if (count == totalCount)
			{
				Managers.Data.Init();

				// 데이터 있는지 확인
				if (Managers.Game.LoadGame() == false)
				{
					Managers.Game.InitGame();
					Managers.Game.SaveGame();
				}

				GetObject((int)GameObjects.StartImage).gameObject.SetActive(true);
				GetText((int)Texts.DisplayText).text = "Touch To Start";
			}
		});

		InitSnakeGame();


	}

	// Snake 게임 초기화 메서드 추가
	private void InitSnakeGame()
	{
		Debug.Log("Snake 게임 초기화 시작");
		
		// 데이터가 로드되지 않았다면 강제로 초기화
		if (Managers.Data.SnakeDic == null || Managers.Data.SnakeDic.Count == 0)
		{
			Debug.LogWarning("Snake 데이터가 없어 초기화합니다");
			// Managers.
			// Managers.Data.Init();
		}
		
		// Snake Head 생성
		Vector3 startPosition = new Vector3(0, 0.5f, 0);
		SnakeController head = Managers.Object.Spawn<SnakeController>(startPosition, 1);
		
		if (head != null)
		{
			Debug.Log("Snake 생성 성공");
			// 명시적으로 SetInfo 호출
			head.SetInfo(1);
		}
		
		// 초기 Food 생성
		CreateInitialFood();
	}
	
	private void CreateInitialFood()
	{
		// 3개의 음식 생성
		for (int i = 0; i < 3; i++)
		{
			float x = Random.Range(-10f, 10f);
			float z = Random.Range(-10f, 10f);
			Vector3 foodPos = new Vector3(x, 0.5f, z);
			
			Food food = Managers.Object.Spawn<Food>(foodPos, 1);
			if (food != null)
			{
				Debug.Log($"음식 생성: 위치={foodPos}");
			}
		}
	}
}
