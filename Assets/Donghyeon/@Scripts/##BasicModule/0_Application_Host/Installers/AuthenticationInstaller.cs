// using Unity.Assets.Scripts.UnityServices.Auth;
// using Unity.Assets.Scripts.Gameplay.Configuration;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Unity.Assets.Scripts.Auth;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers
{
    public class AuthenticationInstaller : IModuleInstaller
    {
        [Inject] private DebugClassFacade _debugClassFacade;
        public ModuleType ModuleType => ModuleType.Authentication;
        public void Install(IContainerBuilder builder)
        {
            _debugClassFacade?.LogInfo(GetType().Name, "인증 모듈 설치 시작");

            // AuthManager 등록
            builder.Register<AuthManager>(Lifetime.Singleton);
            
            // AuthInitializer 등록 (MonoBehaviour)
            // builder.RegisterComponentInHierarchy<AuthInitializer>();
            
            // builder.Register<AuthenticationServiceWrapper>(Lifetime.Singleton);
            // builder.Register<NameGenerationData>(Lifetime.Singleton);
         


            _debugClassFacade?.LogInfo(GetType().Name, "인증 모듈 설치 완료");
        }
    }
} 