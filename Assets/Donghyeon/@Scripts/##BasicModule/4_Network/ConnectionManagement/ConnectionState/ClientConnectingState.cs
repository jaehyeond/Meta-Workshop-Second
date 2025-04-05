using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Assets.Scripts.Scene;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using VContainer;

/// <summary>
/// Connection state corresponding to when a client is attempting to connect to a server. Starts the client when
/// entering. If successful, transitions to the ClientConnected state. If not, transitions to the Offline state.
/// </summary>
class ClientConnectingState : OnlineState
{
    protected ConnectionMethodBase m_ConnectionMethod;

    [Inject]
    protected LocalLobby m_LocalLobby;

    [Inject] SceneManagerEx _sceneManagerEx;
    [Inject] LobbyServiceFacade m_LobbyServiceFacade;
    // 연결 시도 타임아웃을 관리하기 위한 필드 추가
    private Coroutine m_ConnectionTimeoutCoroutine;
    private const float CONNECTION_TIMEOUT = 30.0f; // 15초 타임아웃
    private bool m_ConnectionTimeoutTriggered = false;

    public ClientConnectingState Configure(ConnectionMethodBase baseConnectionMethod)
    {
        m_ConnectionMethod = baseConnectionMethod;
        return this;
    }

    public override void Enter()
    {
        m_ConnectionTimeoutTriggered = false;
        Debug.Log($"<color=green>[ClientConnectingState] Enter 호출됨</color>");
  
        ConnectClientAsync();
        m_ConnectionTimeoutCoroutine = m_ConnectionManager.StartCoroutine(ConnectionTimeoutCheck());
    }

    public override void Exit()
    {
        if (m_ConnectionTimeoutCoroutine != null)
        {
            m_ConnectionManager.StopCoroutine(m_ConnectionTimeoutCoroutine);
            m_ConnectionTimeoutCoroutine = null;
        }
    }
       public override void OnClientConnected(ulong clientId)
       {
           m_ConnectStatusPublisher.Publish(ConnectStatus.Success);
           m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnected);
       }


    public override void OnClientDisconnect(ulong _)
    {
        // client ID is for sure ours here
        StartingClientFailed();
    }

    public override void OnTransportFailure()
    {
        StartingClientFailed();
    }

    void StartingClientFailed()
    {
        var disconnectReason = m_ConnectionManager.NetworkManager.DisconnectReason;
        if (string.IsNullOrEmpty(disconnectReason))
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.StartClientFailed);
        }
        else
        {
            var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
            m_ConnectStatusPublisher.Publish(connectStatus);
        }
        m_ConnectionManager.ChangeState(m_ConnectionManager.m_LobbyConnecting);
    }

    // 타임아웃 체크 코루틴 - 일정 시간 후에도 연결이 안되면 강제로 상태 변경
    private IEnumerator ConnectionTimeoutCheck()
    {
        Debug.Log($"[ClientConnectingState] 연결 타임아웃 체크 시작: {CONNECTION_TIMEOUT}초");
        yield return new WaitForSeconds(CONNECTION_TIMEOUT);

        // 타임아웃 발생 - 아직도 이 상태에 있다면 강제로 다음 상태로 이동
        if (m_ConnectionManager.NetworkManager.IsClient && m_ConnectionManager.NetworkManager.IsConnectedClient)
        {
            Debug.Log("[ClientConnectingState] 연결은 성공했으나 콜백이 호출되지 않음 - 강제로 ClientConnected 상태로 전환");
            m_ConnectionTimeoutTriggered = true;
            
            // 직접 콜백 호출하는 대신 상태 전환
            m_ConnectStatusPublisher.Publish(ConnectStatus.Success);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnected);
        }
        else if (!m_ConnectionManager.NetworkManager.IsConnectedClient)
        {
            Debug.LogWarning("[ClientConnectingState] 연결 타임아웃 - 연결 실패로 처리");
            m_ConnectionTimeoutTriggered = true;
            StartingClientFailed();
        }
    }

    public async Task ConnectClientAsync()
    {
        try
        {
            Debug.Log("[ClientConnectingState] 클라이언트 연결 시도 시작");
            
            // 1. 연결 설정
            await m_ConnectionMethod.SetupClientConnectionAsync();
            Debug.Log("[ClientConnectingState] SetupClientConnectionAsync 성공");
            
            // 2. NetworkManager가 준비되었는지 확인
            if (m_ConnectionManager.NetworkManager == null)
            {
                throw new Exception("NetworkManager가 null입니다");
            }

            var transport = m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            Debug.Log($"[ClientConnectingState] Transport 상태: {transport.GetType().Name}");
            
            // 3. 클라이언트 시작
            Debug.Log($"[ClientConnectingState] StartClient 시도 전 상태 - IsClient: {m_ConnectionManager.NetworkManager.IsClient}, IsConnectedClient: {m_ConnectionManager.NetworkManager.IsConnectedClient}, IsListening: {m_ConnectionManager.NetworkManager.IsListening}");
            
            if (!m_ConnectionManager.NetworkManager.StartClient())
            {
                throw new Exception("NetworkManager StartClient failed");
            }
            Debug.Log("[ClientConnectingState] NetworkManager.StartClient 성공");
            
            // 4. 연결 상태 확인
            Debug.Log($"[ClientConnectingState] StartClient 이후 상태 - IsClient: {m_ConnectionManager.NetworkManager.IsClient}, IsConnectedClient: {m_ConnectionManager.NetworkManager.IsConnectedClient}, IsListening: {m_ConnectionManager.NetworkManager.IsListening}");
            
            if (!m_ConnectionManager.NetworkManager.IsClient)
            {
                throw new Exception("클라이언트가 시작되지 않았습니다");
            }
            Debug.Log("[ClientConnectingState] 클라이언트 상태 확인 완료");
            
            // 5. Transport 설정 확인
            var utp = transport as UnityTransport;
            if (utp != null)
            {
                Debug.Log($"<color=green>[ClientConnectingState] Transport 설정 - ServerIP: {utp.ConnectionData.Address}, Port: {utp.ConnectionData.Port}, IsRelayEnabled: {utp.Protocol == UnityTransport.ProtocolType.RelayUnityTransport}</color>");
            }
// 연결 시도 횟수와 타임아웃 설정 (가능한 경우)
            try {
                utp.MaxConnectAttempts = 5; // 연결 시도 횟수 증가
                utp.ConnectTimeoutMS = 10000; // 타임아웃 시간 증가 (10초)
            } catch (Exception ex) {
                Debug.LogWarning($"<color=yellow>연결 설정 변경 중 오류: {ex.Message}</color>");
            }
            Debug.Log("[ClientConnectingState] 연결 설정 완료, 콜백 대기 중...");
            Debug.Log($"<color=green>[ClientConnectingState] NetworkManager 설정 - ConnectionApprovalCallback: {m_ConnectionManager.NetworkManager.ConnectionApprovalCallback != null}, NetworkConfig: {m_ConnectionManager.NetworkManager.NetworkConfig != null}</color>     ");
            if (m_ConnectionManager.NetworkManager.ConnectionApprovalCallback == null)
            {
                Debug.LogWarning("<color=yellow>[ClientConnectingState] ConnectionApprovalCallback이 null입니다 - 연결 승인 문제가 발생할 수 있습니다</color>");
                
                // 간단한 승인 콜백 설정
                m_ConnectionManager.NetworkManager.ConnectionApprovalCallback = (request, response) => {
                    Debug.Log($"<color=green>[ConnectionApproval] 연결 승인 요청: ClientId={request.ClientNetworkId}</color>");
                    response.Approved = true;
                    response.CreatePlayerObject = true;
                };
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"[ClientConnectingState] 연결 실패: {e.Message}\n{e.StackTrace}");
            StartingClientFailed();
        }
    }
    

}

