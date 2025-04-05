using VContainer;
using VContainer.Unity;   
// using Unity.Assets.Scripts.Infrastructure;
// using Unity.Assets.Scripts.UnityServices;
// using Unity.Assets.Scripts.UnityServices.Lobbies;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers
{
    public class MessageInstaller : IModuleInstaller
    {
        [Inject] private DebugClassFacade _debugClassFacade;

        public ModuleType ModuleType => ModuleType.Message;

        public void Install(IContainerBuilder builder)
        {
            _debugClassFacade?.LogInfo(GetType().Name, "메시지 채널 모듈 설치 시작");
            // 각종 메시지 채널 등록
            // builder.RegisterInstance(new MessageChannel<ConnectStatus>())
            //     .AsImplementedInterfaces();
            
            // builder.RegisterInstance(new MessageChannel<ConnectionEventMessage>())
            //     .AsImplementedInterfaces();
            
            // builder.RegisterInstance(new MessageChannel<ReconnectMessage>())
            //     .AsImplementedInterfaces();
            
            // builder.RegisterInstance(new MessageChannel<UnityServiceErrorMessage>())
            //     .AsImplementedInterfaces();
            
            // builder.RegisterInstance(new MessageChannel<LobbyListFetchedMessage>())
            //     .AsImplementedInterfaces();
            _debugClassFacade?.LogInfo(GetType().Name, "메시지 채널 모듈 설치 완료");
        }
    }
} 