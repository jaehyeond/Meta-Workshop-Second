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











}
