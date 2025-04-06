using VContainer;
using VContainer.Unity;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers
{
    public enum ModuleState
    {
        Disabled,   // 비활성화 상태
        Enabled     // 활성화 상태
    }


    public enum ModuleType
    {
        // 기본 모듈
        Network,
        Message,
        Lobby,
        UI,
        Authentication,
        Resource,  // Resource를 GameData보다 먼저 배치
        GameData,
        Scene,
        Pool,
        Map,
        Debug,
        Object,
        ThirdParty,
        Game
        // 추가 모듈은 여기에 정의
        // 예: Analytics,
        // 예: Monetization,
    }

    /// <summary>
    /// 각 모듈의 의존성 주입 설정을 위한 인터페이스
    /// VContainer의 IInstaller를 상속하여 VContainer 시스템과 통합
    /// </summary>
    public interface IModuleInstaller : IInstaller
    {
        /// <summary>
        /// 이 인스톨러가 담당하는 모듈 타입
        /// </summary>
        ModuleType ModuleType { get; }
    }

    
} 