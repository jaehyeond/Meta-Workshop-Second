using VContainer;
// using Unity.Assets.Scripts.Gameplay.UI;
using VContainer.Unity;
using Unity.Assets.Scripts.UI;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers{
    public class UIInstaller : IModuleInstaller
    {
        [Inject] private DebugClassFacade _debugClassFacade;

        public ModuleType ModuleType => ModuleType.UI;

        public void Install(IContainerBuilder builder)
        {
            _debugClassFacade?.LogInfo(GetType().Name, "UI 모듈 설치 시작");

            builder.Register<UIManager>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<UI_StartUpScene>();

            _debugClassFacade?.LogInfo(GetType().Name, "UI 모듈 설치 완료");
        }
    }
} 