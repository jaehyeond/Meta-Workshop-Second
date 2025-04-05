using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers;
using Unity.Assets.Scripts.Resource;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using Unity.Assets.Scripts.Infrastructure;
// using Unity.Assets.Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using Unity.Assets.Scripts.Network;
using System.Collections;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle
{
    /// <summary>
    /// 모듈식 애플리케이션 컨트롤러
    /// 각 모듈의 활성화 상태를 관리하고 의존성 주입을 설정합니다.
    /// 
    /// ===== 모듈 초기화 및 설치 순서 =====
    /// 
    /// 1. ModuleStateManager 초기화
    ///    - 모듈 상태 관리자를 초기화하여 어떤 모듈이 활성화/비활성화되어야 하는지 관리합니다.
    ///    - 이 단계가 필요한 이유: 모든 모듈을 항상 설치하는 것이 아니라, 활성화된 모듈만 설치하기 위함입니다.
    /// 
    /// 2. InitializeInstallerFactories 호출
    ///    - 각 모듈 타입에 대한 인스톨러 생성 함수를 등록합니다.
    ///    - 예: m_InstallerFactories[ModuleType.GameData] = () => new GameDataInstaller();
    ///    - 이 단계가 필요한 이유: 인스톨러 생성 로직을 한 곳에서 관리하여 유연성을 높이기 위함입니다.
    /// 
    /// 3. CreateModuleInstaller 메서드를 통한 인스톨러 생성
    ///    - 활성화된 모듈에 대해서만 인스톨러를 생성합니다.
    ///    - 이 단계가 필요한 이유: 모듈 타입에 따라 적절한 인스톨러를 생성하고, 오류 처리와 안전성을 제공합니다.
    /// 
    /// 4. 생성된 인스톨러를 m_Installers 리스트에 추가
    ///    - 이 단계가 필요한 이유: 생성된 모든 인스톨러를 수집하여 나중에 일괄 처리하기 위함입니다.
    /// 
    /// 5. 각 인스톨러의 Install 메서드 호출
    ///    - 수집된 모든 인스톨러에 대해 Install 메서드를 호출하여 의존성을 등록합니다.
    ///    - 이 단계가 필요한 이유: 모든 인스톨러를 일괄적으로 설치하여 의존성 주입 순서 문제를 방지합니다.
    /// </summary>
    /// 

    public class ModularApplicationController : LifetimeScope
    {
        // 모듈 상태 관리자
        private ModuleStateManager m_ModuleStateManager;
        
        private Dictionary<ModuleType, Func<IModuleInstaller>> m_InstallerFactories = new Dictionary<ModuleType, Func<IModuleInstaller>>();

        [SerializeField]
        UpdateRunner m_UpdateRunner;

        [SerializeField]
        ConnectionManager m_ConnectionManager;

        [SerializeField]
        NetworkManager m_NetworkManager;

        // [SerializeField]
        // FirebaseManager m_FirebaseManager;

        private LocalLobby m_LocalLobby;
        private LobbyServiceFacade m_LobbyServiceFacade;
        private readonly List<IModuleInstaller> m_Installers = new();
        private IDisposable m_Subscriptions;

        /// <summary>
        /// 모듈 인스톨러 팩토리 초기화
        /// 각 ModuleType에 대한 인스톨러 생성 함수를 등록합니다.
        /// </summary>
        private void InitializeInstallerFactories()
        {
            m_InstallerFactories[ModuleType.Debug] = () => 
            {
                return new DebugInstaller();
            };    
            m_InstallerFactories[ModuleType.ThirdParty] = () => 
            {
                return new FirebaseInstaller();
            };
            m_InstallerFactories[ModuleType.Scene] = () => 
            {
                return new SceneInstaller();
            };
                        // GameData 모듈 인스톨러 팩토리
            m_InstallerFactories[ModuleType.Resource] = () => 
            {
                return new ResourceInstaller();
            };
                        // Authentication 모듈 인스톨러 팩토리
            m_InstallerFactories[ModuleType.Authentication] = () => 
            {
                return new AuthenticationInstaller();
            };
            
            // Network 모듈 인스톨러 팩토리
            m_InstallerFactories[ModuleType.Network] = () => 
            {
                return new NetworkInstaller(m_NetworkManager, m_ConnectionManager, m_UpdateRunner);
                // return null;
            };
            
            // Message 모듈 인스톨러 팩토리
            m_InstallerFactories[ModuleType.Message] = () => 
            {
                //return new MessageInstaller();
                return null;
            };
            
            // Lobby 모듈 인스톨러 팩토리
            m_InstallerFactories[ModuleType.Lobby] = () => 
            {
                return new LobbyInstaller();
            };
            m_InstallerFactories[ModuleType.GameData] = () => 
            {
                return new DataInstaller();
            };
            // UI 모듈 인스톨러 팩토리
            m_InstallerFactories[ModuleType.UI] = () => 
            {
                return new UIInstaller();
            };

            m_InstallerFactories[ModuleType.Object] = () => 
            {
                return new ObjectInstaller();
            };

            m_InstallerFactories[ModuleType.Pool] = () => 
            {
                return new PoolInstaller();
            };
            m_InstallerFactories[ModuleType.Map] = () => 
            {
                return new MapInstaller();
            };

            m_InstallerFactories[ModuleType.Game] = () => 
            {
                return new GameInstaller();
            };
        }

        /// <summary>
        /// 모듈 인스톨러 생성 메서드
        /// </summary>
        /// <param name="moduleType">모듈 타입</param>
        /// <returns>생성된 인스톨러 인스턴스 또는 null</returns>
        private IModuleInstaller CreateModuleInstaller(ModuleType moduleType)
        {
            if (m_InstallerFactories.TryGetValue(moduleType, out var factory))
            {
                return factory();
            }
            return null;
        }

        /// <summary>
        /// 인스톨러 초기화 및 등록
        /// </summary>
        private void InitializeInstallers()
        {
            UnityEngine.Debug.Log("[ModularApplicationController] Installer 초기화 시작");
            
            // 인스톨러 팩토리 초기화
            InitializeInstallerFactories();
            
            // 모든 모듈 타입에 대해 처리
            foreach (ModuleType moduleType in Enum.GetValues(typeof(ModuleType)))
            {
                if (m_ModuleStateManager.IsModuleEnabled(moduleType))
                {
                    var installer = CreateModuleInstaller(moduleType);
                    if (installer != null)
                    {
                        // 인스톨러의 ModuleType 속성 확인 (디버깅용)
                        if (installer.ModuleType != moduleType)
                        {
                            // UnityEngine.Debug.LogWarning($"[ModularApplicationController] 인스톨러의 ModuleType({installer.ModuleType})이 요청한 ModuleType({moduleType})과 다릅니다.");
                        }
                        
                        m_Installers.Add(installer);
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"[ModularApplicationController] '{moduleType}' 모듈이 비활성화되어 인스톨러를 추가하지 않음");
                }
            }
            
            UnityEngine.Debug.Log("[ModularApplicationController] Installer 초기화 완료");
        }


        protected override void Configure(IContainerBuilder builder)
        {
            UnityEngine.Debug.Log("[ModularApplicationController] 시작: Configure - 의존성 주입 설정");
            base.Configure(builder);

            // 모듈 상태 관리자 초기화
            m_ModuleStateManager = ModuleStateManager.Instance;
            builder.Register<LocalLobbyUser>(Lifetime.Singleton);
            builder.Register<LocalLobby>(Lifetime.Singleton);
            builder.RegisterEntryPoint<LobbyServiceFacade>(Lifetime.Singleton).AsSelf();
            // builder.Register<FirebaseManager>(Lifetime.Singleton);
            builder.RegisterInstance(new BufferedMessageChannel<LobbyListFetchedMessage>()).AsImplementedInterfaces();

            // 인스톨러 자동 검색 (선택적으로 사용)
            // AutoDiscoverInstallers();
            
            // 필요한 경우 특정 모듈 비활성화
            // m_ModuleStateManager.SetModuleState(ModuleType.Network, ModuleState.Disabled);
            
            // 또는 특정 모듈만 활성화하고 나머지는 비활성화
            // m_ModuleStateManager.EnableOnlySpecifiedModules(ModuleType.Network, ModuleType.Message);

            InitializeInstallers();

            // 각 모듈의 Installer 실행
            foreach (var installer in m_Installers)
            {
                installer.Install(builder);
            }
            
            // ModuleStateManager를 컨테이너에 등록하여 다른 클래스에서 주입받을 수 있도록 함
            builder.RegisterInstance(m_ModuleStateManager).AsSelf();

            UnityEngine.Debug.Log("[ModularApplicationController] 종료: Configure");
        }

        private void Start()
        {
            UnityEngine.Debug.Log("[ModularApplicationController] 시작: Start");
            m_LocalLobby = Container.Resolve<LocalLobby>();
            m_LobbyServiceFacade = Container.Resolve<LobbyServiceFacade>();
            // m_FirebaseManager = Container.Resolve<FirebaseManager>();

    // 메모리 로그 비활성화 코드 추가
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.wantsToQuit += OnWantToQuit;
            
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(m_UpdateRunner.gameObject);

            UnityEngine.Debug.Log("[ModularApplicationController] 프레임레이트 설정: 120fps");
            Application.targetFrameRate = 120;
            
            UnityEngine.Debug.Log("[ModularApplicationController] 종료: Start");
        }

        protected override void OnDestroy()
        {
            UnityEngine.Debug.Log("[ModularApplicationController] 시작: OnDestroy");
            
            if (m_Subscriptions != null)
            {
                UnityEngine.Debug.Log("[ModularApplicationController] 구독 해제");
                m_Subscriptions.Dispose();
            }

            base.OnDestroy();
            if (m_Subscriptions != null)
           {
               m_Subscriptions.Dispose();
           }

           if (m_LobbyServiceFacade != null)
           {
               m_LobbyServiceFacade.EndTracking();
           }
            UnityEngine.Debug.Log("[ModularApplicationController] 종료: OnDestroy");
        }
        private IEnumerator LeaveBeforeQuit()
        {
            Debug.Log("[ModularApplicationController] 종료: LeaveBeforeQuit");
            // We want to quit anyways, so if anything happens while trying to leave the Lobby, log the exception then carry on
            try
            {
                m_LobbyServiceFacade.EndTracking();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            yield return null;
            Application.Quit();
        }
        private bool OnWantToQuit()
        {
            UnityEngine.Debug.Log("[ModularApplicationController] 애플리케이션 종료 시도");
            Application.wantsToQuit -= OnWantToQuit;


            try
            {
                var resourceManager = Container.Resolve<ResourceManager>();
                if (resourceManager != null)
                {
                    UnityEngine.Debug.Log("[ModularApplicationController] ResourceManager.Clear() 호출 - 모든 리소스 초기화");
                    resourceManager.Clear();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ModularApplicationController] 리소스 정리 중 오류 발생: {ex.Message}");
            }
            // var canQuit = m_LocalLobby != null && string.IsNullOrEmpty(m_LocalLobby.LobbyID);
            // if (!canQuit)
            // {
            //     StartCoroutine(LeaveBeforeQuit());
            // }
            StartCoroutine(LeaveBeforeQuit());
            return true;
        }
    }
} 