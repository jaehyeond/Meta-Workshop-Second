using System;
using System.Collections;
using System.Collections.Generic;
// using System.Data;
using System.Threading.Tasks;
using Unity.Netcode;
// using UnityEditor.VersionControl;
using UnityEngine;
using VContainer;

    /// <summary>
    /// 앱의 연결 모드를 정의하는 열거형
    /// </summary>
    public enum ConnectionMode
    {
        OfflineOnly,    // 오프라인 전용
        OnlineRequired, // 온라인 필수
        Hybrid          // 혼합 모드
    }
    /// <summary>
    /// 네트워크 연결 상태를 나타내는 열거형
    /// 클라이언트와 호스트의 다양한 연결 상태를 정의
    /// </summary>
    public enum ConnectStatus
    {
        Undefined,               // 초기 상태
        Success,                // 연결 성공
        ServerFull,            // 서버가 가득 참
        LoggedInAgain,         // 다른 곳에서 로그인됨
        UserRequestedDisconnect, // 사용자가 연결 종료 요청
        GenericDisconnect,     // 일반적인 연결 종료
        Reconnecting,          // 재연결 시도 중
        IncompatibleBuildType, // 빌드 타입 불일치
        HostEndedSession,      // 호스트가 세션 종료
        StartHostFailed,       // 호스트 시작 실패
        StartClientFailed,      // 클라이언트 시작 실패
          Disconnected,
        Connecting,
        Connected,
        Failed,
 
    }

    /// <summary>
    /// 재연결 시도 정보를 담는 구조체
    /// 현재 시도 횟수와 최대 시도 횟수를 포함
    /// </summary>
    public struct ReconnectMessage
    {
        public int CurrentAttempt;  // 현재 재연결 시도 횟수
        public int MaxAttempt;      // 최대 재연결 시도 횟수

        public ReconnectMessage(int currentAttempt, int maxAttempt)
        {
            CurrentAttempt = currentAttempt;
            MaxAttempt = maxAttempt;
        }
    }

    /// <summary>
    /// 연결 이벤트 메시지 구조체
    /// 네트워크로 직렬화 가능한 연결 상태 정보
    /// </summary>
    public struct ConnectionEventMessage : INetworkSerializeByMemcpy
    {
        public ulong ClientId;

        public ConnectStatus ConnectStatus;  // 현재 연결 상태
    }

/// <summary>
/// 네트워크 연결 시 전달되는 페이로드 클래스
/// 플레이어 식별 및 디버그 정보 포함
/// </summary>
[Serializable]
public class ConnectionPayload
{
    public string playerId;    // 플레이어 고유 ID
    public string playerName;  // 플레이어 이름
    public bool isDebug;       // 디버그 모드 여부
}

/// <summary>
/// 네트워크 연결 관리자 클래스
/// Unity NGO(Netcode for GameObjects)를 사용한 네트워크 연결 관리
/// 상태 패턴을 사용하여 다양한 연결 상태 처리
/// 
/// NetworkBehaviour를 상속받아 RPC 기능을 제공하며, 상태 패턴과 통합되어 있습니다.
/// 각 상태 클래스는 이 클래스의 RPC 메서드를 간접적으로 호출하여 네트워크 통신을 수행합니다.
/// 
/// 주요 기능:
/// 1. 네트워크 연결 상태 관리 (상태 패턴)
/// 2. 서버-클라이언트 간 통신 (RPC)
/// 3. 연결 승인 및 재연결 처리
/// 4. 네트워크 이벤트 처리
/// </summary>
/// 

namespace Unity.Assets.Scripts.Network
{


    public class ConnectionManager : MonoBehaviour
    {
        // 연결 상태 변경 이벤트
        public int MaxConnectedPlayers = 2;
        public event System.Action<ConnectStatus> OnConnectionStatusChanged;

        [SerializeField]
        private ConnectionMode m_ConnectionMode = ConnectionMode.OnlineRequired;  // 기본값은 혼합 모드

        // 현재 연결 상태를 관리하는 상태 객체
        ConnectionState m_CurrentState;
        [Inject] private DebugClassFacade m_DebugClassFacade;

        [Inject] private NetworkManager m_NetworkManager;
        [Inject] IObjectResolver m_Resolver;
        [Inject] private IPublisher<ConnectionEventMessage> m_ConnectionEventPublisher;
        [Inject] protected IPublisher<ConnectStatus> m_ConnectStatusPublisher;
        public NetworkManager NetworkManager => m_NetworkManager;

        [SerializeField]
        int m_NbReconnectAttempts = 2;
        public int NbReconnectAttempts => m_NbReconnectAttempts;

        // 상태 패턴을 위한 상태 객체들
        internal readonly OfflineState m_Offline = new OfflineState();
        internal readonly LobbyConnectingState m_LobbyConnecting = new LobbyConnectingState();
        internal readonly ClientConnectingState m_ClientConnecting = new ClientConnectingState();
        internal readonly ClientConnectedState m_ClientConnected = new ClientConnectedState();
        internal readonly ClientReconnectingState m_ClientReconnecting = new ClientReconnectingState();
        internal readonly StartingHostState m_StartingHost = new StartingHostState();
        internal readonly HostingState m_Hosting = new HostingState();

        // 네트워크 이벤트 구독/해제 상태 추적
        private bool m_IsNetworkCallbacksRegistered = false;
        
        // 연결 상태 체크 코루틴
        private Coroutine m_ConnectionStatusCheckCoroutine;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (NetworkManager == null)
            {
                Debug.LogError("[ConnectionManager] NetworkManager is null in Start");
                return;
            }

            m_DebugClassFacade?.LogInfo(GetType().Name, "네트워크 매니저 초기화 시작");
            List<ConnectionState> states = new()
            {
                m_Offline,
                m_ClientConnecting,
                m_ClientConnected,
                m_ClientReconnecting,
                m_StartingHost,
                m_Hosting,
                m_LobbyConnecting
            };

            m_DebugClassFacade?.LogInfo(GetType().Name, "초기 상태 설정: Offline");
            m_CurrentState = m_Offline;

            // 이벤트 핸들러 등록
            RegisterNetworkCallbacks();
            
            // ConnectionState 상태 객체들에 종속성 주입
            foreach (var connectionState in states)
            {
                m_Resolver.Inject(connectionState);
            }
            
            // 연결 상태 체크 코루틴 시작
            StartConnectionStatusCheck();
            
            m_DebugClassFacade?.LogInfo(GetType().Name, "종료: Start");
        }

        // 네트워크 콜백 등록을 별도 메서드로 분리
        private void RegisterNetworkCallbacks()
        {
            if (m_IsNetworkCallbacksRegistered)
            {
                m_DebugClassFacade?.LogInfo(GetType().Name, "네트워크 이벤트 핸들러가 이미 등록되어 있습니다.");
                return;
            }

            m_DebugClassFacade?.LogInfo(GetType().Name, "네트워크 이벤트 핸들러 등록 시작");
            NetworkManager.NetworkConfig.ConnectionApproval = true;
            NetworkManager.NetworkConfig.EnableSceneManagement = true;
            // 이벤트 핸들러 등록
            NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            m_DebugClassFacade?.LogInfo(GetType().Name, "[ConnectionManager] OnClientConnectedCallback 핸들러 등록됨");
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck; // 추가: 기존 콜백 제거

            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            m_DebugClassFacade?.LogInfo(GetType().Name, "[ConnectionManager] OnClientDisconnectCallback 핸들러 등록됨");
            
            NetworkManager.OnServerStarted += OnServerStarted;
            m_DebugClassFacade?.LogInfo(GetType().Name, "[ConnectionManager] OnServerStarted 핸들러 등록됨");
            
            NetworkManager.OnTransportFailure += OnTransportFailure;
            NetworkManager.OnServerStopped += OnServerStopped;
            
            m_DebugClassFacade?.LogInfo(GetType().Name, "네트워크 이벤트 핸들러 등록 완료");
            
            m_IsNetworkCallbacksRegistered = true;
        }

        private void UnregisterNetworkCallbacks()
        {
            if (!m_IsNetworkCallbacksRegistered)
                return;

            NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            NetworkManager.OnServerStarted -= OnServerStarted;
            NetworkManager.OnTransportFailure -= OnTransportFailure;
            NetworkManager.OnServerStopped -= OnServerStopped;
            NetworkManager.ConnectionApprovalCallback -= ApprovalCheck; // 추가: 기존 콜백 제거

            m_IsNetworkCallbacksRegistered = false;
            m_DebugClassFacade?.LogInfo(GetType().Name, "네트워크 이벤트 핸들러 등록 해제");
        }

        // 연결 상태 주기적 체크 코루틴
        private void StartConnectionStatusCheck()
        {
            if (m_ConnectionStatusCheckCoroutine != null)
            {
                StopCoroutine(m_ConnectionStatusCheckCoroutine);
            }
            m_ConnectionStatusCheckCoroutine = StartCoroutine(ConnectionStatusCheckCoroutine());
        }

        private IEnumerator ConnectionStatusCheckCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(5.0f);
                
                if (NetworkManager != null)
                {
                    bool isClient = NetworkManager.IsClient;
                    bool isConnected = NetworkManager.IsConnectedClient;
                    bool isHost = NetworkManager.IsHost;
                    bool isServer = NetworkManager.IsServer;
                    
                    m_DebugClassFacade?.LogInfo(GetType().Name, 
                        $"연결 상태 체크: Client={isClient}, Connected={isConnected}, Host={isHost}, Server={isServer}, " +
                        $"현재 상태={m_CurrentState?.GetType().Name}");
                    
                    // 상태와 실제 연결 상태의 불일치 감지
                    if (isClient && isConnected && m_CurrentState is ClientConnectingState)
                    {
                        m_DebugClassFacade?.LogWarning(GetType().Name, "연결은 완료되었는데 상태가 ClientConnecting에 머물러 있음 - 상태 전환 시도");
                        ChangeState(m_ClientConnected);
                    }
                    
                    // 콜백 등록 확인 및 필요시 재등록
                    if (!m_IsNetworkCallbacksRegistered)
                    {
                        m_DebugClassFacade?.LogWarning(GetType().Name, "네트워크 콜백이 등록되지 않음 - 재등록 시도");
                        RegisterNetworkCallbacks();
                    }
                }
            }
        }

        void OnDestroy()
        {
            UnregisterNetworkCallbacks();
            
            if (m_ConnectionStatusCheckCoroutine != null)
            {
                StopCoroutine(m_ConnectionStatusCheckCoroutine);
                m_ConnectionStatusCheckCoroutine = null;
            }
        }

        internal void ChangeState(ConnectionState nextState)
        {
            Debug.Log($"{name}: Changed connection state from {m_CurrentState?.GetType().Name} to {nextState.GetType().Name}.");

            if (m_CurrentState != null)
            {
                m_CurrentState.Exit();
            }
            m_CurrentState = nextState;
            m_CurrentState.Enter();
        }

        // NGO 이벤트 핸들러들
        void OnClientDisconnectCallback(ulong clientId)
        {
            m_CurrentState.OnClientDisconnect(clientId);
        }

        void OnClientConnectedCallback(ulong clientId)
        {
            m_CurrentState.OnClientConnected(clientId);
        }
        void OnServerStarted()
        {
            m_DebugClassFacade?.LogInfo(GetType().Name, "[ConnectionManager] 매치 서버 시작됨");
            
            if (m_CurrentState != null)
            {
                m_CurrentState.OnServerStarted();
            }
            else
            {
                m_DebugClassFacade?.LogError(GetType().Name, "[ConnectionManager] m_CurrentState가 null입니다 - OnServerStarted 처리 불가");
            }
        }

       void OnTransportFailure()
       {
           m_CurrentState.OnTransportFailure();
       }

        void OnServerStopped(bool _)
        {
            m_CurrentState.OnServerStopped();
        }

        public async Task<bool> CheckNetworkStatusAsync()
        {
            try
            {
                // 1. 인터넷 연결 상태 확인
                bool isOnline = Application.internetReachability != NetworkReachability.NotReachable;
                if (!isOnline)
                {
                    m_DebugClassFacade?.LogWarning(GetType().Name, "인터넷 연결이 없습니다.");
                    return false;
                }

                // 2. 현재 연결 상태 확인
                if (m_CurrentState is OfflineState)
                {
                    // 오프라인 상태인 경우, 로비 연결 상태로 전환
                    m_DebugClassFacade?.LogInfo(GetType().Name, "오프라인 상태에서 로비 연결 상태로 전환");
                    ChangeState(m_LobbyConnecting);
                    await System.Threading.Tasks.Task.Delay(1000); // 상태 전환 대기
                    return m_CurrentState is not OfflineState;
                }

                // 3. 현재 상태가 오프라인이 아닌 경우
                return true;
            }
            catch (System.Exception e)
            {
                m_DebugClassFacade?.LogError(GetType().Name, $"네트워크 상태 확인 중 오류 발생: {e.Message}");
                return false;
            }
        }

        public void StartClientLobby(string playerName)
        {
            m_CurrentState.StartClientLobby(playerName);
        }

        public void StartHostLobby(string playerName)
        {
            m_CurrentState.StartHostLobby(playerName);
        }


      // 추가: 연결 승인 콜백
       void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
       {
           m_CurrentState.ApprovalCheck(request, response);
       }


        public void RequestShutdown()
        {
            m_CurrentState.OnUserRequestedShutdown();
        }

        [ClientRpc]
        public void LoadSceneClientRpc(string sceneName)
        {
            m_DebugClassFacade?.LogInfo(GetType().Name, $"[ConnectionManager] 씬 전환 RPC 수신: {sceneName}");
            
            // 클라이언트는 이 RPC를 수신만 하고 직접 씬을 로드하지 않음
            // 서버가 NetworkSceneManager를 통해 씬을 로드하면 자동으로 클라이언트에도 적용됨
            m_DebugClassFacade?.LogInfo(GetType().Name, $"[ConnectionManager] 씬 전환 RPC 수신 완료. 서버의 NetworkSceneManager 씬 전환을 기다립니다.");
        }        
        // [ClientRpc]
        // public void LoadSceneClientRpc(string sceneName)
        // {
        //     m_DebugClassFacade?.LogInfo(GetType().Name, $"[ConnectionManager] 씬 전환 RPC 수신: {sceneName}");
            
        //     if (m_NetworkManager != null && m_NetworkManager.SceneManager != null)
        //     {
        //         try
        //         {
        //             m_DebugClassFacade?.LogInfo(GetType().Name, $"[ConnectionManager] NetworkSceneManager를 통해 {sceneName} 씬 로드 시작");
        //             m_NetworkManager.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        //             m_DebugClassFacade?.LogInfo(GetType().Name, $"[ConnectionManager] NetworkSceneManager 통한 씬 로드 요청 완료: {sceneName}");
        //         }
        //         catch (Exception e)
        //         {
        //             m_DebugClassFacade?.LogError(GetType().Name, $"[ConnectionManager] NetworkSceneManager를 통한 씬 로드 실패: {e.Message}");
                    
        //             // 실패 시 직접 씬 로드 시도
        //             try
        //             {
        //                 m_DebugClassFacade?.LogWarning(GetType().Name, $"[ConnectionManager] 직접 {sceneName} 씬 로드 시도");
        //                 UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        //             }
        //             catch (Exception e2)
        //             {
        //                 m_DebugClassFacade?.LogError(GetType().Name, $"[ConnectionManager] 직접 씬 로드도 실패: {e2.Message}");
        //             }
        //         }
        //     }
        //     else
        //     {
        //         m_DebugClassFacade?.LogError(GetType().Name, "[ConnectionManager] NetworkManager 또는 SceneManager가 null입니다.");
                
        //         // NetworkManager가 null이면 직접 씬 로드 시도
        //         try
        //         {
        //             m_DebugClassFacade?.LogWarning(GetType().Name, $"[ConnectionManager] NetworkManager가 null이므로 직접 {sceneName} 씬 로드 시도");
        //             UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        //         }
        //         catch (Exception e)
        //         {
        //             m_DebugClassFacade?.LogError(GetType().Name, $"[ConnectionManager] 직접 씬 로드 실패: {e.Message}");
        //         }
        //     }
        // }

        
    }
}
