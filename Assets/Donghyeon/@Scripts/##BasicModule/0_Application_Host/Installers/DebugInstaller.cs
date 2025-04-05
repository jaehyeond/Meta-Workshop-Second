using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 디버깅 관련 클래스들을 DI 컨테이너에 등록하는 Installer
/// </summary>
/// 
namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers
{
    public class DebugInstaller : IModuleInstaller
    {
        public ModuleType ModuleType => ModuleType.Debug;

        public void Install(IContainerBuilder builder)
        {
            // DebugManager를 싱글톤으로 등록
            builder.Register<DebugManager>(Lifetime.Singleton);
            
            // DebugClassFacade를 싱글톤으로 등록하고 AsSelf()를 추가하여 명시적으로 타입을 지정
            builder.Register<DebugClassFacade>(Lifetime.Singleton);
        }
        
    }

}