using Unity.Netcode;
using VContainer;
// using Unity.Assets.Scripts.Infrastructure;
using VContainer.Unity;                         // VContainer를 Unity 컴포넌트에서 사용하기 위한 확장
using Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers;
using Unity.Assets.Scripts.Network;


namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers
{
    public class NetworkInstaller : IModuleInstaller
    {
        [Inject] private DebugClassFacade _debugClassFacade;

        public ModuleType ModuleType => ModuleType.Network;

        private readonly NetworkManager m_NetworkManager;
        private readonly ConnectionManager m_ConnectionManager;
        private readonly UpdateRunner m_UpdateRunner;

        public NetworkInstaller(NetworkManager networkManager, 
                              ConnectionManager connectionManager,
                              UpdateRunner updateRunner)
        {
            m_NetworkManager = networkManager;
            m_ConnectionManager = connectionManager;
            m_UpdateRunner = updateRunner;
        }

        public void Install(IContainerBuilder builder)
        {
            _debugClassFacade?.LogInfo(GetType().Name, "네트워크 모듈 설치 시작");
            
            builder.RegisterComponent(m_UpdateRunner);
            builder.RegisterComponent(m_ConnectionManager);
            builder.RegisterComponent(m_NetworkManager);

            builder.RegisterInstance(new MessageChannel<ConnectStatus>()).AsImplementedInterfaces();
            builder.RegisterInstance(new MessageChannel<ReconnectMessage>()).AsImplementedInterfaces();
            builder.RegisterComponent(new NetworkedMessageChannel<ConnectionEventMessage>()).AsImplementedInterfaces();

            builder.Register<ProfileManager>(Lifetime.Singleton);

            builder.Register<NetUtils>(Lifetime.Singleton);




            _debugClassFacade?.LogInfo(GetType().Name, "네트워크 모듈 설치 완료");
        }
    }
} 