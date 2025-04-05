using VContainer;

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
    /// 모듈 인스톨러 인터페이스
    /// 각 모듈은 이 인터페이스를 구현하여 의존성 주입 설정을 제공합니다.
    /// </summary>
    public interface IModuleInstaller
    {
        /// <summary>
        /// 이 인스톨러가 담당하는 모듈 타입
        /// </summary>
        ModuleType ModuleType { get; }

        /// <summary>
        /// 의존성 주입 설정 메서드
        /// </summary>
        /// <param name="builder">컨테이너 빌더</param>
        void Install(IContainerBuilder builder);
    }

    
} 