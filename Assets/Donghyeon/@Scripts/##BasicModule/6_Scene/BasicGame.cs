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

	// [Inject] private ServerMonster _serverMonster; // MonoBehaviour는 이런 방식으로 주입받을 수 없습니다.
	
    // VContainer.IObjectResolver 추가
    [Inject] private VContainer.IObjectResolver _container;
    [Inject] private BasicGameState _basicGameState;
    [Inject] private ResourceManager _resourceManager;
    [Inject] private UIManager _uiManager;
    [Inject] private ObjectManagerFacade _objectManagerFacade;
    [Inject] private MapSpawnerFacade _mapSpawnerFacade;
    
    // AppleManager 참조 추가
    private AppleManager _appleManager;
    
	// greenslime 몬스터 ID
	
	// 스폰된 몬스터 관리 리스트

    // OnGridSpawned 이벤트가 너무 일찍 호출되는 것을 방지하기 위한 플래그
    private bool _isInitialized = false;
	
    private bool _isEventsSubscribed = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        SceneType = EScene.BasicGame;
 
      	_uiManager.ShowBaseUI<UI_Joystick>();

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
        
        // AppleManager 생성 및 초기화
        InitializeAppleManager();
      
        return true;
    }

    // AppleManager 초기화 메서드
    private void InitializeAppleManager()
    {
        if (_resourceManager == null)
        {
            Debug.LogError("[BasicGameScene] ResourceManager가 null입니다.");
            return;
        }
        
        // AppleManager 프리팹 로드 및 생성
        GameObject appleManagerPrefab = _resourceManager.Load<GameObject>("Prefabs/InGame/AppleManager");
        if (appleManagerPrefab == null)
        {
            Debug.LogError("[BasicGameScene] AppleManager 프리팹을 찾을 수 없습니다.");
            
            // 프리팹이 없으면 빈 오브젝트 생성
            GameObject newObj = new GameObject("AppleManager");
            _appleManager = newObj.AddComponent<AppleManager>();
        }
        else
        {
            // 프리팹 인스턴스화
            GameObject instance = Instantiate(appleManagerPrefab);
            instance.name = "AppleManager";
            _appleManager = instance.GetComponent<AppleManager>();
        }
        
        // VContainer를 통한 의존성 주입
        if (_appleManager != null && _container != null)
        {
            _container.Inject(_appleManager);
            
            // NetworkObject 확인 및 스폰
            NetworkObject netObj = _appleManager.GetComponent<NetworkObject>();
            if (netObj != null && NetworkManager.Singleton.IsServer)
            {
                netObj.Spawn();
                Debug.Log("[BasicGameScene] AppleManager 네트워크 스폰 완료");
            }
        }
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
        _appleManager = null;
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

    }

    private void UnsubscribeEvents()
    {

    }

 


}

}