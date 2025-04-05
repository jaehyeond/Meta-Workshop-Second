using VContainer;
using VContainer.Unity;
using Unity.Assets.Scripts.Scene;
using Unity.Assets.Scripts.UI;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Unity.Netcode;

public class MainMenuLifetimeScope : LifetimeScope
{
    [Inject] private DebugClassFacade _debugClassFacade;

    [Inject] private NetUtils _netUtils;

    [Inject] private NetworkManager _networkManager;
    protected override void Configure(IContainerBuilder builder)
    {
       _debugClassFacade?.LogInfo(GetType().Name, "MainMenuLifetimeScope Configure 시작");
       
       // 부모 스코프의 설정을 상속
       base.Configure(builder);
       

       GameObject LobbyUIMediator_O = GameObject.Find("LobbyUIMediator");

       if (LobbyUIMediator_O != null)
       {
           var LobbyUIMediator = LobbyUIMediator_O.GetComponent<LobbyUIMediator>();
           if (LobbyUIMediator != null)
           {
               builder.RegisterInstance(LobbyUIMediator);
               Debug.Log("[BasicGameLifetimeScope] 기존 LobbyUIMediator 재사용");
           }
       }
       else
       {
              builder.RegisterComponentOnNewGameObject<LobbyUIMediator>(
               Lifetime.Singleton, 
               "LobbyUIMediator");
           Debug.Log("[BasicGameLifetimeScope] 새로운 LobbyUIMediator 생성");
       }

       
       builder.RegisterBuildCallback(container => {
           try {
               // MapSpawnerFacade 초기화
               var LobbyUIMediator = container.Resolve<LobbyUIMediator>();
               GameObject LobbyUIMediator_O =  LobbyUIMediator.gameObject;
               DontDestroyOnLoad(LobbyUIMediator_O);
            }
           catch (Exception e)
           {
               Debug.LogError($"오브젝트 설정 중 오류 발생: {e.Message}\n{e.StackTrace}");
           }
       });




       // MainMenu 씬에서만 사용할 컴포넌트 등록
       _debugClassFacade?.LogInfo(GetType().Name, "MainMenuScene 등록 시도");
       builder.RegisterComponentInHierarchy<MainMenuScene>();
       
       _debugClassFacade?.LogInfo(GetType().Name, "UI_MainMenu_Matching 등록 시도");
       builder.RegisterComponentInHierarchy<UI_MainMenu>();
       
       _debugClassFacade?.LogInfo(GetType().Name, "Configure 완료");
    }


// 네트워크 프리팹 등록 메소드 추가
    private void RegisterNetworkPrefabs()
    {
        try
        {
            // NetworkManager 찾기
            NetworkManager networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("[MainMenuLifetimeScope] NetworkManager를 찾을 수 없습니다.");
                return;
            }
            
            // 이미 시작된 경우 경고
            if (networkManager.IsListening)
            {
                Debug.LogWarning("[MainMenuLifetimeScope] NetworkManager가 이미 시작되었습니다. 프리팹 등록이 무시될 수 있습니다.");
            }
            
            // MapSpawner 프리팹 생성 및 등록
            RegisterMapSpawnerPrefab(networkManager);
            
            // MonsterSpawner 프리팹 생성 및 등록
            RegisterMonsterSpawnerPrefab(networkManager);
            
            Debug.Log("[MainMenuLifetimeScope] 네트워크 프리팹 등록 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MainMenuLifetimeScope] 네트워크 프리팹 등록 중 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }

    private void RegisterMapSpawnerPrefab(NetworkManager networkManager)
    {
        // 먼저 기존에 생성된 프리팹이 있는지 확인
        GameObject existingMapSpawner = GameObject.Find("MapSpawner");
        
        if (existingMapSpawner != null)
        {
            // 기존 오브젝트에 NetworkObject 컴포넌트가 있는지 확인
            if (existingMapSpawner.GetComponent<NetworkObject>() == null)
            {
                existingMapSpawner.AddComponent<NetworkObject>();
            }
            
            // NetworkConfig에 등록
            RegisterPrefabIfNeeded(networkManager, existingMapSpawner);
        }
        else
        {
            // 새로운 MapSpawner 프리팹 생성
            GameObject mapSpawnerObj = new GameObject("MapSpawner");
            mapSpawnerObj.AddComponent<NetworkObject>();
            mapSpawnerObj.AddComponent<MapSpawnerFacade>();
            
            // 씬에 유지시키기
            DontDestroyOnLoad(mapSpawnerObj);
            
            // NetworkConfig에 등록
            RegisterPrefabIfNeeded(networkManager, mapSpawnerObj);
        }
    }

    private void RegisterMonsterSpawnerPrefab(NetworkManager networkManager)
    {
        // 먼저 기존에 생성된 프리팹이 있는지 확인
        GameObject existingMonsterSpawner = GameObject.Find("MonsterSpawner");
        
        if (existingMonsterSpawner != null)
        {
            // 기존 오브젝트에 NetworkObject 컴포넌트가 있는지 확인
            if (existingMonsterSpawner.GetComponent<NetworkObject>() == null)
            {
                existingMonsterSpawner.AddComponent<NetworkObject>();
            }
            
            // NetworkConfig에 등록
            RegisterPrefabIfNeeded(networkManager, existingMonsterSpawner);
        }
        else
        {
            // 새로운 MonsterSpawner 프리팹 생성
            GameObject monsterSpawnerObj = new GameObject("MonsterSpawner");
            monsterSpawnerObj.AddComponent<NetworkObject>();
            monsterSpawnerObj.AddComponent<ObjectManagerFacade>();
            
            // 씬에 유지시키기
            DontDestroyOnLoad(monsterSpawnerObj);
            
            // NetworkConfig에 등록
            RegisterPrefabIfNeeded(networkManager, monsterSpawnerObj);
        }
    }

    // 프리팹 중복 등록 방지를 위한 헬퍼 메소드
    private void RegisterPrefabIfNeeded(NetworkManager networkManager, GameObject prefab)
    {
        // 이미 등록된 프리팹인지 확인
        bool alreadyRegistered = false;
        foreach (var existingPrefab in networkManager.NetworkConfig.Prefabs.Prefabs)
        {
            if (existingPrefab.Prefab == prefab)
            {
                alreadyRegistered = true;
                break;
            }
        }
        
        if (!alreadyRegistered)
        {
            networkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
            Debug.Log($"[MainMenuLifetimeScope] 네트워크 프리팹 등록: {prefab.name}");
        }
        else
        {
            Debug.Log($"[MainMenuLifetimeScope] 프리팹 {prefab.name}은 이미 등록되어 있습니다.");
        }
    }








}
