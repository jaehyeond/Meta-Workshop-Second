using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using Unity.Netcode;


/// <summary>       
/// ConnectionMethod contains all setup needed to setup NGO to be ready to start a connection, either host or client side.
/// Please override this abstract class to add a new transport or way of connecting.
/// </summary>
/// 

public abstract class ConnectionMethodBase
    {
        protected ConnectionManager m_ConnectionManager;

        readonly ProfileManager m_ProfileManager;

        protected readonly string m_PlayerName;
        protected const string k_DtlsConnType = "dtls";
        /// <summary>
        /// Setup the host connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract Task SetupHostConnectionAsync();


        /// <summary>
        /// Setup the client connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract Task SetupClientConnectionAsync();

        /// <summary>
        /// Setup the client for reconnection prior to reconnecting
        /// </summary>
        /// <returns>
        /// success = true if succeeded in setting up reconnection, false if failed.
        /// shouldTryAgain = true if we should try again after failing, false if not.
        /// </returns>
        public abstract Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync();

        public ConnectionMethodBase(ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
        {
            m_ConnectionManager = connectionManager;
            m_ProfileManager = profileManager;
            m_PlayerName = playerName;
        }

        protected void SetConnectionPayload(string playerId, string playerName)
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                playerId = playerId,
                playerName = playerName,
                isDebug = Debug.isDebugBuild
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            m_ConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }

        /// Using authentication, this makes sure your session is associated with your account and not your device. This means you could reconnect
        /// from a different device for example. A playerId is also a bit more permanent than player prefs. In a browser for example,
        /// player prefs can be cleared as easily as cookies.
        /// The forked flow here is for debug purposes and to make UGS optional in Boss Room. This way you can study the sample without
        /// setting up a UGS account. It's recommended to investigate your own initialization and IsSigned flows to see if you need
        /// those checks on your own and react accordingly. We offer here the option for offline access for debug purposes, but in your own game you
        /// might want to show an error popup and ask your player to connect to the internet.
      protected string GetPlayerId()
      {
          if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
          {
              return ClientPrefs.GetGuid() + m_ProfileManager.Profile;
          }

          return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + m_ProfileManager.Profile;
      }
    }


    /// <summary>
    /// UTP's Relay connection setup using the Lobby integration
    /// </summary>
    class ConnectionMethodRelay : ConnectionMethodBase
    {
        LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobby m_LocalLobby;
        
       public ConnectionMethodRelay(LobbyServiceFacade lobbyServiceFacade, LocalLobby localLobby, ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
           : base(connectionManager, profileManager, playerName)
       {
           m_LobbyServiceFacade = lobbyServiceFacade;
           m_LocalLobby = localLobby;
           m_ConnectionManager = connectionManager;
       }

        public override async Task SetupClientConnectionAsync()
        {
            Debug.Log("<color=red>[ConnectionMethodRelay] Setting up Unity Relay client</color>");

            SetConnectionPayload(GetPlayerId(), m_PlayerName);

            if (m_LobbyServiceFacade.CurrentUnityLobby == null)
            {
                throw new Exception("Trying to start relay while Lobby isn't set");
            }

            Debug.Log($"Setting Unity Relay client with join code {m_LocalLobby.RelayJoinCode}");

            // Create client joining allocation from join code
            var joinedAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: m_LocalLobby.RelayJoinCode);
            Debug.Log($"<color=red>[ConnectionMethodRelay] client: {joinedAllocation.ConnectionData[0]} {joinedAllocation.ConnectionData[1]}, " +
                $"host: {joinedAllocation.HostConnectionData[0]} {joinedAllocation.HostConnectionData[1]}, " +
                $"client: {joinedAllocation.AllocationId}</color>");

            // await m_LobbyServiceFacade.UpdatePlayerDataAsync(joinedAllocation.AllocationId.ToString(), m_LocalLobby.RelayJoinCode);
            await m_LobbyServiceFacade.UpdatePlayerDataAsync(joinedAllocation.AllocationId.ToString(), m_LocalLobby.RelayJoinCode);
            Debug.Log("<color=red>joinedAllocation.AllocationIdBytes: " + joinedAllocation.AllocationIdBytes + "</color>");
            Debug.Log("<color=red>joinedAllocation.AllocationIdBytes.ToString(): " + joinedAllocation.AllocationId.ToString() + "</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 시도: {m_LocalLobby.RelayJoinCode}</color>");


            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinedAllocation, k_DtlsConnType));

            // if (!string.IsNullOrEmpty(m_LocalLobby.RelayJoinCode) && m_ConnectionManager.NetworkManager.StartClient())
            // {
            //     Debug.Log("<color=red>릴레이 서버 연결 성공: " + m_LocalLobby.RelayJoinCode + "</color>");
            //     return;
            // }

            // 연결 성공 후
            // Configure UTP with allocation
            // var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;


            // 아래와 같이 변경:
            // utp.SetRelayServerData(
            //     joinedAllocation.RelayServer.IpV4,
            //     (ushort)joinedAllocation.RelayServer.Port,
            //     joinedAllocation.AllocationIdBytes,
            //     joinedAllocation.Key,
            //     joinedAllocation.ConnectionData,
            //     joinedAllocation.HostConnectionData
            // );

            Debug.Log($"<color=red>릴레이 서버 연결 성공: {joinedAllocation.ConnectionData[0]} {joinedAllocation.ConnectionData[1]}</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 성공: {joinedAllocation.AllocationIdBytes}</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 성공: {joinedAllocation.ConnectionData}</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 성공: {joinedAllocation.Key}</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 성공: {joinedAllocation.RelayServer.IpV4}</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 성공: {joinedAllocation.RelayServer.Port}</color>");

        }

        public override async Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
        {
            if (m_LobbyServiceFacade.CurrentUnityLobby == null)
            {
                Debug.Log("Lobby does not exist anymore, stopping reconnection attempts.");
                return (false, false);
            }

            // When using Lobby with Relay, if a user is disconnected from the Relay server, the server will notify the
            // Lobby service and mark the user as disconnected, but will not remove them from the lobby. They then have
            // some time to attempt to reconnect (defined by the "Disconnect removal time" parameter on the dashboard),
            // after which they will be removed from the lobby completely.
            // See https://docs.unity.com/lobby/reconnect-to-lobby.html
            var lobby = await m_LobbyServiceFacade.ReconnectToLobbyAsync();
            var success = lobby != null;
            Debug.Log(success ? "Successfully reconnected to Lobby." : "Failed to reconnect to Lobby.");
            return (success, true); // return a success if reconnecting to lobby returns a lobby
        }

        public override async Task SetupHostConnectionAsync()
        {
            Debug.Log("<color=red>[ConnectionMethodRelay] Setting up Unity Relay host</color>");

            // Create relay allocation
            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(m_ConnectionManager.MaxConnectedPlayers, region: null);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);

            Debug.Log($"<color=red>[ConnectionMethodRelay] server: connection data: {hostAllocation.ConnectionData[0]} {hostAllocation.ConnectionData[1]}, " +
                $"allocation ID:{hostAllocation.AllocationId}, region:{hostAllocation.Region}</color>");

            m_LocalLobby.RelayJoinCode = joinCode;

            // next line enables lobby and relay services integration
            await m_LobbyServiceFacade.UpdateLobbyDataAndUnlockAsync();
            await m_LobbyServiceFacade.UpdatePlayerDataAsync(hostAllocation.AllocationIdBytes.ToString(), joinCode);

            // NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(hostAllocation, k_DtlsConnType ));
            // return NetworkManager.Singleton.StartHost() ? joinCode : null;


            // Setup UTP with relay connection info
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetRelayServerData(new RelayServerData(
                hostAllocation.RelayServer.IpV4,    // 실제 Relay 서버 IP 사용
                (ushort) hostAllocation.RelayServer.Port,    // 실제 Relay 서버 포트 사용
                hostAllocation.AllocationIdBytes,
                hostAllocation.ConnectionData,
                hostAllocation.ConnectionData,  // 호스트의 경우 ConnectionData를 사용
                hostAllocation.Key,
                false       // isSecure
            ));

            // Set connection payload for host after setting up relay
            SetConnectionPayload(GetPlayerId(), m_PlayerName);

            Debug.Log($"<color=red>릴레이 서버 연결 성공: {hostAllocation.ConnectionData[0]} {hostAllocation.ConnectionData[1]}</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 성공: {hostAllocation.AllocationIdBytes}</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 성공: {hostAllocation.ConnectionData}</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 성공: {hostAllocation.Key}</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 성공: {hostAllocation.RelayServer.IpV4}</color>");
            Debug.Log($"<color=red>릴레이 서버 연결 성공: {hostAllocation.RelayServer.Port}</color>");
        }
    }
