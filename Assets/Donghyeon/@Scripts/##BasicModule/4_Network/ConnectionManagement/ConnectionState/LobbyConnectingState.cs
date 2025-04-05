using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using VContainer;
using System.Collections.Generic;
using Unity.Assets.Scripts.Auth;
using Unity.Assets.Scripts.Scene;
using System.Collections;
using Unity.Networking.Transport;
using Unity.Assets.Scripts.UnityServices.Lobbies;


namespace Unity.Assets.Scripts.Network
{
    /// <summary>
    /// 로비 연결 상태 클래스
    /// 
    /// 로비 연결 상태를 관리하며, 매칭 서비스를 통해 로비 생성/참가를 처리합니다.
    /// </summary>
    public class LobbyConnectingState : ConnectionState
    {
        

        private readonly float k_MatchmakingTimeout = 60.0f; // 매칭 타임아웃 (20초)
        private bool m_IsWaitingForPlayers = false;
                // 로비 관련 변수 추가
        private const int maxPlayers = 2; // 최대 플레이어 수 (필요에 따라 조정)
        public static event Action<bool> OnWaitingStateChanged; // true: 대기 시작, false: 대기 종료

        // 플레이어 세션 관련 변수
        private string m_LocalPlayerId;
        private string m_LocalPlayerName = "Player"; // 기본 이름
        private SessionManager<SessionPlayerData> m_SessionManager => SessionManager<SessionPlayerData>.Instance;
              
        [Inject] private SceneManagerEx _sceneManagerEx;
        [Inject] private LocalLobby m_LocalLobby;
        [Inject] private AuthManager m_authManager;
        [Inject] private LobbyServiceFacade m_LobbyServiceFacade;

        [Inject]
        ProfileManager m_ProfileManager;
        public override void Enter()
        {
            m_DebugClassFacade?.LogInfo(GetType().Name, "[LobbyConnectingState] 로비대기 상태");

            // if (m_authManager.IsAuthenticated)
            // {
            //     m_LocalPlayerId =  m_authManager.PlayerId;
            //     m_DebugClassFacade?.LogInfo(GetType().Name, $"[LobbyConnectingState] 인증 플레이어 ID 초기화: {m_LocalPlayerId}");

            //     // // 인증된 플레이어라면 DB에서 데이터 가져오기
            //     // PlayerData data = await DatabaseService.GetPlayerData(m_LocalPlayerId);
            //     // if (data != null)
            //     // {
            //     //     m_LocalPlayerName = data.playerName;
            //     //     // 기타 데이터 로드
            //     // }
            // } 
            // else {
            //     m_LocalPlayerId = System.Guid.NewGuid().ToString();
            //     m_DebugClassFacade?.LogInfo(GetType().Name, $"[LobbyConnectingState] 로컬 플레이어 ID 초기화: {m_LocalPlayerId}");
            // }

            // m_LobbyServiceFacade.EndTracking();
        }
        public override void Exit(){}

        public override void StartHostLobby(string playerName)
        {
            var connectionMethod = new ConnectionMethodRelay(m_LobbyServiceFacade, m_LocalLobby, m_ConnectionManager, m_ProfileManager, playerName);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_StartingHost.Configure(connectionMethod));
        }


          public override void StartClientLobby(string playerName)
        {
            var connectionMethod = new ConnectionMethodRelay(m_LobbyServiceFacade, m_LocalLobby, m_ConnectionManager, m_ProfileManager, playerName);
            m_ConnectionManager.m_ClientReconnecting.Configure(connectionMethod);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnecting.Configure(connectionMethod));
        }


    }
}