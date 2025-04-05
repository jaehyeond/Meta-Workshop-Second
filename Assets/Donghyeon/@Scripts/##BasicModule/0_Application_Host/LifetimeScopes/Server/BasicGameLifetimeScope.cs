using VContainer;
using VContainer.Unity;
using Unity.Assets.Scripts.Scene;
using Unity.Assets.Scripts.UI;
using UnityEngine;
using Unity.Assets.Scripts.Resource;
using System;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;
using Unity.Assets.Scripts.Data;

public class BasicGameLifetimeScope : LifetimeScope
{
    [Inject] private DebugClassFacade _debugClassFacade;
  

    protected override void Configure(IContainerBuilder builder)
    {
        _debugClassFacade?.LogInfo(GetType().Name, "BasicGameLifetimeScope Configure 시작");
        base.Configure(builder);
      
        // 부모 스코프에서 ResourceManager 참조 (새 인스턴스 생성 방지)
        ResourceManager resourceManager = null;
        if (Parent != null && Parent.Container.Resolve<ResourceManager>() != null)
        {
            resourceManager = Parent.Container.Resolve<ResourceManager>();
            builder.RegisterInstance(resourceManager);
            Debug.Log("[BasicGameLifetimeScope] 부모 스코프에서 ResourceManager 참조 성공");
        }
        else
        {
            // 부모 스코프에서 ResourceManager를 찾을 수 없는 경우 새로 생성
            Debug.LogWarning("[BasicGameLifetimeScope] 부모 스코프에서 ResourceManager를 찾을 수 없어 새로 생성합니다.");
            builder.Register<ResourceManager>(Lifetime.Singleton);
        }

        NetUtils netUtils = null;
        if (Parent != null && Parent.Container.Resolve<NetUtils>() != null)
        {
            netUtils = Parent.Container.Resolve<NetUtils>();
            builder.RegisterInstance(netUtils);
            Debug.Log("[BasicGameLifetimeScope] 부모 스코프에서 NetUtils 참조 성공");
        }

        // UIManager 등록
        builder.Register<UIManager>(Lifetime.Singleton);
        Debug.Log("[BasicGameLifetimeScope] UIManager 등록 완료");

        // 기본 매니저 등록
        builder.Register<ObjectManager>(Lifetime.Singleton);
        builder.Register<MapManager>(Lifetime.Singleton);

        builder.RegisterComponentInHierarchy<BasicGameState>();

        builder.RegisterComponentInHierarchy<MapSpawnerFacade>();

        builder.RegisterComponentInHierarchy<ObjectManagerFacade>();


 
        // 씬 관련 컴포넌트 등록
        _debugClassFacade?.LogInfo(GetType().Name, "BasicGameScene 등록 시도");
        builder.RegisterComponentInHierarchy<BasicGameScene>();
       
        _debugClassFacade?.LogInfo(GetType().Name, "UI_BasicGame 등록 시도");
        builder.RegisterComponentInHierarchy<UI_BasicGame>();

        builder.RegisterBuildCallback(container => {
            var mainCamera = GameObject.Find("Main Camera");
            if (mainCamera != null) {
                var cameraScript = mainCamera.GetComponent<Camera_BasicGame>();
                if (cameraScript == null) {
                    cameraScript = mainCamera.AddComponent<Camera_BasicGame>();
                }
                container.Inject(cameraScript);
                Debug.Log("[BasicGameLifetimeScope] Camera_BasicGame 컴포넌트가 Main Camera에 추가되었습니다.");
            } else {
                Debug.LogError("[BasicGameLifetimeScope] Main Camera를 찾을 수 없습니다!");
            }
        });

        // 컨테이너 빌드 후 초기화를 수행하는 콜백 등록
        builder.RegisterBuildCallback(container => {
            try {
                Debug.Log("[BasicGameLifetimeScope] 의존성 객체 초기화 시작");

                try {
                    var basicGameState = container.Resolve<BasicGameState>();
                    basicGameState.Initialize();

                    var mapSpawnerFacadeRef = container.Resolve<MapSpawnerFacade>();
                    mapSpawnerFacadeRef.Initialize();

                    var objectManagerFacadeRef = container.Resolve<ObjectManagerFacade>();
                    objectManagerFacadeRef.Initialize();

                }
                catch (Exception e) {
                    Debug.LogError($"[BasicGameLifetimeScope] BasicGameState 참조 또는 초기화 중 오류: {e.Message}");
                }
                
                Debug.Log("[BasicGameLifetimeScope] 모든 컴포넌트 초기화 완료");
            } 
            catch (Exception ex) {
                Debug.LogError($"[BasicGameLifetimeScope] 초기화 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        });

        _debugClassFacade?.LogInfo(GetType().Name, "BasicGameLifetimeScope Configure 완료");
    }
}