using Unity.Assets.Scripts.Scene;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using UnityEngine;
using VContainer;


    /// <summary>
    /// Connection state corresponding to a connected client. When being disconnected, transitions to the
    /// ClientReconnecting state if no reason is given, or to the Offline state.
    /// </summary>
    class ClientConnectedState : OnlineState
    {
        [Inject]
        protected LobbyServiceFacade m_LobbyServiceFacade;
        [Inject] SceneManagerEx _sceneManagerEx;
        [Inject] LocalLobby m_LocalLobby;
        public override void Enter()
        {
            Debug.Log("[ClientConnectedState] 클라이언트 연결 상태 진입");
            if (m_LobbyServiceFacade.CurrentUnityLobby != null)
            {
                m_LobbyServiceFacade.BeginTracking();
            }
            // if (m_LocalLobby.LobbyUsers.Count >= m_ConnectionManager.MaxConnectedPlayers)
            // {
            //     Debug.Log("[ClientConnectedState] 플레이어 수 충족 - 게임 씬으로 전환");
            //     _sceneManagerEx.LoadScene(EScene.BasicGame.ToString(), useNetworkSceneManager: true);
            // }
        }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong _)
        {
            var disconnectReason = m_ConnectionManager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason) ||
                disconnectReason == "Disconnected due to host shutting down.")
            {
                m_ConnectStatusPublisher.Publish(ConnectStatus.Reconnecting);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientReconnecting);
            }
            else
            {
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                m_ConnectStatusPublisher.Publish(connectStatus);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
        }
    }

