using VContainer;
// using Unity.Assets.Scripts.UnityServices.Lobbies;
// using Unity.Assets.Scripts.Utils;
using Unity.Assets.Scripts.Resource;
using VContainer.Unity;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers
{
    public class MapInstaller : IModuleInstaller
    {
        [Inject] private DebugClassFacade _debugClassFacade;

        public ModuleType ModuleType => ModuleType.Map;
        public void Install(IContainerBuilder builder)
        {
            _debugClassFacade?.LogInfo(GetType().Name, "맵 모듈 설치 시작");

            // MapManager 등록 (싱글톤으로)
            builder.Register<MapManager>(Lifetime.Singleton);
            
            // MapSpawnerFacade는 BasicGameLifetimeScope에서 프리팹 인스턴스로 등록되므로 여기서는 제거
            // builder.Register<MapSpawnerFacade>(Lifetime.Singleton);

            _debugClassFacade?.LogInfo(GetType().Name, "맵 모듈 설치 완료");
        }
    }
} 