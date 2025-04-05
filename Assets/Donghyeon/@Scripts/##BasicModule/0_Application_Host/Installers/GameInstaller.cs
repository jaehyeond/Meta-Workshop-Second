using VContainer;
// using Unity.Assets.Scripts.Gameplay.UI;
using VContainer.Unity;
using Unity.Assets.Scripts.UI;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers{
    public class GameInstaller : IModuleInstaller
    {
        [Inject] private DebugClassFacade _debugClassFacade;

        public ModuleType ModuleType => ModuleType.Game;

        public void Install(IContainerBuilder builder)
        {
            _debugClassFacade?.LogInfo(GetType().Name, "game 모듈 설치 시작");

            // UIManager 등록
            // builder.Register<GameManager>(Lifetime.Singleton);
;
        }
    }
} 