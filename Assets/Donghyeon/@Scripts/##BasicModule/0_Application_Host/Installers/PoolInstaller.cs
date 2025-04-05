using VContainer;
// using Unity.Assets.Scripts.UnityServices.Lobbies;
// using Unity.Assets.Scripts.Utils;
// using  Unity.Assets.Scripts.Pooling;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers
{
    public class PoolInstaller : IModuleInstaller
    {
        [Inject] private DebugClassFacade _debugClassFacade;

        public ModuleType ModuleType => ModuleType.Pool;
        public void Install(IContainerBuilder builder)
        {
            _debugClassFacade?.LogInfo(GetType().Name, "풀 모듈 설치 시작");
            builder.Register<PoolManager>(Lifetime.Singleton);
            _debugClassFacade?.LogInfo(GetType().Name, "풀 모듈 설치 완료");
        }
    }
} 