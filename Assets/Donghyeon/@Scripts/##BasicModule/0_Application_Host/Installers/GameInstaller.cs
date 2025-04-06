using VContainer;
// using Unity.Assets.Scripts.Gameplay.UI;
using VContainer.Unity;
using Unity.Assets.Scripts.UI;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers{
    public class GameInstaller : IModuleInstaller
    {
        [Inject] private DebugClassFacade _debugClassFacade;

        public ModuleType ModuleType => ModuleType.Game;

        public void Install(IContainerBuilder builder)
        {
            Debug.Log($"[GameInstaller] Install 메서드 시작 (Builder: {builder.GetHashCode()})"); // 로그 추가
            _debugClassFacade?.LogInfo(GetType().Name, "game 모듈 설치 시작");

            Debug.Log("[GameInstaller] CameraProvider 등록 시도"); // 로그 추가
            // builder.Register<CameraProvider>(Lifetime.Singleton);
            // GameInstaller.cs 또는 ProjectContext의 Installer
            builder.Register<CameraProvider>(Lifetime.Singleton)
            .AsSelf() // 자기 자신 타입으로 등록
            .AsImplementedInterfaces(); // 필요한 인터페이스가 있다면 추가
            Debug.Log("[GameInstaller] CameraProvider 등록 완료"); // 로그 추가

            // builder.AddSingleton(typeof(Game));
            // builder.AddSingleton(typeof(InputService));
            // builder.AddSingleton(typeof(StaticDataService));

            // builder.AddSingleton(typeof(UIFactory));
            // builder.AddSingleton(typeof(LeaderboardService));
            // builder.AddSingleton(typeof(SnakesFactory));
            // builder.AddSingleton(typeof(AppleFactory));
            // builder.AddSingleton(typeof(VfxFactory));
            // builder.AddSingleton(typeof(SnakesDestruction));
            
            // UIManager 등록
            // builder.Register<GameManager>(Lifetime.Singleton);
;
        }
    }
} 