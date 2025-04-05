using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Assets.Scripts.Scene;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Object = UnityEngine.Object;
using Unity.Netcode;
using Unity.Assets.Scripts.UI;
using Unity.Assets.Scripts.Objects;
using VContainer.Unity;
using System;
using static Define;


namespace Unity.Assets.Scripts.Scene
{
public class BasicGameScene : BaseScene
{
    [Inject] public MapSpawnerFacade _mapSpawnerFacade;
	[Inject] private ObjectManagerFacade _objectManagerFacade;
	// [Inject] private ServerMonster _serverMonster; // MonoBehaviour는 이런 방식으로 주입받을 수 없습니다.
	
    // VContainer.IObjectResolver 추가
    [Inject] private VContainer.IObjectResolver _container;
    [Inject] private BasicGameState _basicGameState;
	// greenslime 몬스터 ID
	public int MONSTER_ID = 202001;
	
	// 스폰된 몬스터 관리 리스트
	private List<ServerMonster> _spawnedMonsters = new List<ServerMonster>();

    // OnGridSpawned 이벤트가 너무 일찍 호출되는 것을 방지하기 위한 플래그
    private bool _isInitialized = false;
	
    private bool _isEventsSubscribed = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        SceneType = EScene.BasicGame;
 
        if (!_isEventsSubscribed)
        {
            SubscribeEvents();
            _isEventsSubscribed = true;
        }

        // if (_basicGameState != null)
        // {
        //     _basicGameState.Load();
        // }
        
        if (_objectManagerFacade != null)
        {
            _objectManagerFacade.Load();
        }
        
        if (_mapSpawnerFacade != null)
        {
            _mapSpawnerFacade.Load();
            _mapSpawnerFacade.LoadMap();
        }
      
        return true;
    }

    public override void Clear()
    {
        if (_isEventsSubscribed)
        {
            UnsubscribeEvents();
            _isEventsSubscribed = false;
        }
        
        // 참조 해제
        _objectManagerFacade = null;
        _mapSpawnerFacade = null;
        _basicGameState = null;
    }

    private void OnEnable()
    {
        // 이벤트 구독은 Init에서만 수행
    }

    private void OnDisable()
    {
        // 이벤트 해제는 Clear에서만 수행
    }

    private void SubscribeEvents()
    {
        // 이미 구독되어 있는지 확인하기 위해 먼저 해제
        UI_BasicGame.OnSummonButtonRequested += OnSummonButtonRequested;
        MapSpawnerFacade.GridSpawned += OnGridSpawned;
    }

    private void UnsubscribeEvents()
    {
        UI_BasicGame.OnSummonButtonRequested -= OnSummonButtonRequested;
        MapSpawnerFacade.GridSpawned -= OnGridSpawned;
    }

    private void OnSummonButtonRequested()
    {   
        _objectManagerFacade.Summon();
        Debug.Log("[BasicGameScene] OnSummonButtonRequested");
    }

    private void OnGridSpawned()
    {
   
        
         _objectManagerFacade.Spawn_Monster(false, 202001);
    }


}

}