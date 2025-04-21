using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using Unity.Assets.Scripts.Scene;


/// <summary>
/// Connection state corresponding to a listening host. Handles incoming client connections. When shutting down or
/// being timed out, transitions to the Offline state.
/// </summary>
class HostingState : OnlineState
{
    [Inject]
    protected LocalLobby m_LocalLobby;

    [Inject] SceneManagerEx _sceneManagerEx;

    [Inject]
    LobbyServiceFacade m_LobbyServiceFacade;

    [Inject]
    IPublisher<ConnectionEventMessage> m_ConnectionEventPublisher;

    [Inject] DebugClassFacade m_DebugClassFacade;
    
    // 연결 상태 체크 코루틴 제거
    // private Coroutine m_ConnectionStatusCheckCoroutine;
    // private float lastSceneLoadAttemptTime = 0f;
    // private bool _isSceneLoaded = false;

    public override void Enter()
    {
        Debug.Log("[HostingState] Enter - 호스트 모드로 진입");
        
        // 로비 추적 시작
        if (m_LobbyServiceFacade.CurrentUnityLobby != null)
        {
            m_LobbyServiceFacade.BeginTracking();
            Debug.Log("[HostingState] 로비 추적 시작");
        }

        // 호스팅 시작 시 즉시 게임 씬 로드
        Debug.Log("[HostingState] 게임 씬 즉시 로드 시도");
        _sceneManagerEx.LoadScene(EScene.BasicGame.ToString(), useNetworkSceneManager: true);

        // 플레이어 수 확인 및 씬 전환 로직 제거
        // CheckPlayersAndLoadScene();
        
        // 연결 상태 모니터링 로직 제거
        // StartPlayerCountMonitoring();
    }

    // 플레이어 수 확인 및 필요시 씬 전환 로직 제거
    // private void CheckPlayersAndLoadScene()
    // {
    //     ...
    // }
    
    // 플레이어 수 모니터링 관련 메소드 제거
    // private void StartPlayerCountMonitoring()
    // {
    //     ...
    // }
    // private System.Collections.IEnumerator MonitorPlayerCount()
    // {
    //     ...
    // }
  

    public override void Exit()
    {
        // 코루틴 정리 로직 제거
        // if (m_ConnectionStatusCheckCoroutine != null)
        // {
        //     m_ConnectionManager.StopCoroutine(m_ConnectionStatusCheckCoroutine);
        //     m_ConnectionStatusCheckCoroutine = null;
        // }
        
        // _isSceneLoaded 플래그 관련 로직 제거
        // _isSceneLoaded = false;
        
        // 세션 정리
        SessionManager<SessionPlayerData>.Instance.OnServerEnded();
    }

    public override void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[HostingState] 클라이언트 연결됨: ClientID={clientId}");
        
        // 간단한 세션 데이터 생성
        try
        {
            // 기본 세션 데이터 생성 (자세한 검증 없이)
            string playerId = clientId.ToString(); // 간단하게 클라이언트 ID를 플레이어 ID로 사용
            string playerName = $"Player_{clientId}"; // 기본 이름 부여
            Debug.Log($"[HostingState] 클라이언트 {clientId} 세션 데이터 설정 중 ####################################");
            // 세션 데이터 설정
            SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(
                clientId, 
                playerId,
                new SessionPlayerData(clientId, playerName, new NetworkGuid(), 0, true)
            );
            
            // 연결 성공 이벤트 발행
            m_ConnectionEventPublisher.Publish(new ConnectionEventMessage 
            { 
                ConnectStatus = ConnectStatus.Success, 
                ClientId = clientId 
            });
            
            Debug.Log($"[HostingState] 클라이언트 {clientId} 세션 데이터 설정 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[HostingState] 클라이언트 {clientId} 세션 데이터 설정 중 오류: {e.Message}");
        }
        
        // 플레이어 수 확인 및 씬 전환 로직 제거
        // CheckPlayersAndLoadScene();
    }

    public override void OnClientDisconnect(ulong clientId)
    {
        Debug.Log($"[HostingState] 클라이언트 연결 해제: ClientID={clientId}");
        
        if (clientId != m_ConnectionManager.NetworkManager.LocalClientId)
        {
            try
            {
                // 연결 해제 이벤트 발행
                m_ConnectionEventPublisher.Publish(new ConnectionEventMessage 
                { 
                    ConnectStatus = ConnectStatus.GenericDisconnect, 
                    ClientId = clientId 
                });
                
                // 세션에서 클라이언트 제거
                SessionManager<SessionPlayerData>.Instance.DisconnectClient(clientId);
                Debug.Log($"[HostingState] 클라이언트 {clientId} 세션에서 제거됨");
            }
            catch (Exception e)
            {
                Debug.LogError($"[HostingState] 클라이언트 {clientId} 연결 해제 처리 중 오류: {e.Message}");
            }
        }
    }

    public override void OnUserRequestedShutdown()
    {
        var reason = JsonUtility.ToJson(ConnectStatus.HostEndedSession);
        for (var i = m_ConnectionManager.NetworkManager.ConnectedClientsIds.Count - 1; i >= 0; i--)
        {
            var id = m_ConnectionManager.NetworkManager.ConnectedClientsIds[i];
            if (id != m_ConnectionManager.NetworkManager.LocalClientId)
            {
                m_ConnectionManager.NetworkManager.DisconnectClient(id, reason);
            }
        }
        m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
    }

    public override void OnServerStopped()
    {
        m_ConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
        m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
    }

    public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log($"[HostingState] 클라이언트 승인 요청: ClientID={request.ClientNetworkId}");
        
        // 최대 연결 수 확인
        if (m_ConnectionManager.NetworkManager.ConnectedClientsIds.Count >= m_ConnectionManager.MaxConnectedPlayers)
        {
            response.Approved = false;
            response.Reason = JsonUtility.ToJson(ConnectStatus.ServerFull);
            Debug.LogWarning($"[HostingState] 연결 거부: 서버가 가득 찼습니다. (ClientID: {request.ClientNetworkId})");
            return; // 추가 처리 중단
        }
        
        try
        {
            // 연결 승인
            float randomX = UnityEngine.Random.Range(-1f, 1f);
            float randomZ = UnityEngine.Random.Range(-1f, 1f);
            Vector3 spawnPosition = new Vector3(randomX, 0f, randomZ); // Y는 0으로 가정

            // --- 연결 승인 및 스폰 설정 ---
            response.Approved = true;           // 연결 승인
            response.CreatePlayerObject = true; // 플레이어 객체 자동 생성 요청 (유지!)

            // --- 계산된 랜덤 위치 및 기본 회전 설정 ---
            response.Position = spawnPosition;       // 스폰될 위치 지정
            response.Rotation = Quaternion.identity; // 스폰될 회전 지정 (기본값)

            Debug.Log($"[HostingState] 클라이언트 승인 완료: ClientID={request.ClientNetworkId}, SpawnPosition={spawnPosition}");

            // 페이로드 관련 로직은 그대로 유지 (필요하다면)
            // if (request.Payload != null && request.Payload.Length > 0)
            // {
            //     ...
            // }
        }
        catch (Exception e)
        {
            // 예외 발생 시 연결 거부
            response.Approved = false;
            response.Reason = JsonUtility.ToJson(ConnectStatus.GenericDisconnect);
            Debug.LogError($"[HostingState] 클라이언트 승인 중 오류 발생: {e.Message} (ClientID: {request.ClientNetworkId})");
            // 추가적으로 오류 처리가 필요할 수 있음
        }
    }
}