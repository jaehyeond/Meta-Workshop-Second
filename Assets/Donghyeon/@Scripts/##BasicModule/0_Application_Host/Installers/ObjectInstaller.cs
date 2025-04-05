using VContainer;
// using Unity.Assets.Scripts.UnityServices.Lobbies;
// using Unity.Assets.Scripts.Utils;
// using  Unity.Assets.Scripts.Pooling;
using Unity.Assets.Scripts.Objects;
using Unity.Assets.Scripts.Resource;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers
{
    public class ObjectInstaller : IModuleInstaller
    {
        [Inject] private DebugClassFacade _debugClassFacade;

        public ModuleType ModuleType => ModuleType.Object;
        public void Install(IContainerBuilder builder)
        {
            _debugClassFacade?.LogInfo(GetType().Name, "오브젝트 모듈 설치 시작");

            builder.Register<ObjectManager>(Lifetime.Singleton);
            // builder.Register<INetworkMediator, NetworkMediator>(Lifetime.Singleton);
            builder.Register<NetworkMediator>(Lifetime.Singleton).As<INetworkMediator>();

            _debugClassFacade?.LogInfo(GetType().Name, "오브젝트 모듈 설치 완료");
        }
    }
} 