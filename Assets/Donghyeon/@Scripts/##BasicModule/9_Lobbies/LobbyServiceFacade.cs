using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.Scene;
using Unity.Netcode;

/// <summary>
/// An abstraction layer between the direct calls into the Lobby API and the outcomes you actually want.
/// </summary>

namespace Unity.Assets.Scripts.UnityServices.Lobbies{
    public class LobbyServiceFacade : IDisposable, IStartable
    {

        [Inject] private DebugClassFacade _debugClassFacade;


        [Inject] LifetimeScope m_ParentScope;
        [Inject] UpdateRunner m_UpdateRunner;
        [Inject] LocalLobby m_LocalLobby;
        [Inject] LocalLobbyUser m_LocalUser;
        // [Inject] IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePub;
        [Inject] IPublisher<LobbyListFetchedMessage> m_LobbyListFetchedPub;
        [Inject] SceneManagerEx _sceneManagerEx;
        [Inject] NetworkManager _networkManager;
        const float k_HeartbeatPeriod = 8; // The heartbeat must be rate-limited to 5 calls per 30 seconds. We'll aim for longer in case periods don't align.
        float m_HeartbeatTime = 0;
        public int MaxConnectedPlayers = 2;

        LifetimeScope m_ServiceScope;
        LobbyAPIInterface m_LobbyApiInterface;

        RateLimitCooldown m_RateLimitQuery;
        RateLimitCooldown m_RateLimitJoin;
        RateLimitCooldown m_RateLimitQuickJoin;
        RateLimitCooldown m_RateLimitHost;

        public Lobby CurrentUnityLobby { get; private set; }

        ILobbyEvents m_LobbyEvents;

        bool m_IsTracking = false;

        LobbyEventConnectionState m_LobbyEventConnectionState = LobbyEventConnectionState.Unknown;


        
        public void Start()
        {
            m_ServiceScope = m_ParentScope.CreateChild(builder =>
            {
                builder.Register<LobbyAPIInterface>(Lifetime.Singleton);
            });

            m_LobbyApiInterface = m_ServiceScope.Container.Resolve<LobbyAPIInterface>();

            //See https://docs.unity.com/lobby/rate-limits.html
            m_RateLimitQuery = new RateLimitCooldown(1f);
            m_RateLimitJoin = new RateLimitCooldown(3f);
            m_RateLimitQuickJoin = new RateLimitCooldown(10f);
            m_RateLimitHost = new RateLimitCooldown(3f);
        }

        public void Dispose()
        {
            EndTracking();
            if (m_ServiceScope != null)
            {
                m_ServiceScope.Dispose();
            }
        }

        public void SetRemoteLobby(Lobby lobby)
        {
            CurrentUnityLobby = lobby;
            m_LocalLobby.ApplyRemoteData(lobby);
        }

        /// <summary>
        /// Initiates tracking of joined lobby's events. The host also starts sending heartbeat pings here.
        /// </summary>
        public void BeginTracking()
        {
            if (!m_IsTracking)
            {
                m_IsTracking = true;
                SubscribeToJoinedLobbyAsync();

                // Only the host sends heartbeat pings to the service to keep the lobby alive
                if (m_LocalUser.IsHost)
                {
                    m_HeartbeatTime = 0;
                    m_UpdateRunner.Subscribe(DoLobbyHeartbeat, 1.5f);
                }
            }
        }

        /// <summary>
        /// Ends tracking of joined lobby's events and leaves or deletes the lobby. The host also stops sending heartbeat pings here.
        /// </summary>
        public void EndTracking()
        {
            if (m_IsTracking)
            {
                m_IsTracking = false;
                UnsubscribeToJoinedLobbyAsync();

                // Only the host sends heartbeat pings to the service to keep the lobby alive
                if (m_LocalUser.IsHost)
                {
                    m_UpdateRunner.Unsubscribe(DoLobbyHeartbeat);
                }
            }

            if (CurrentUnityLobby != null)
            {
                if (m_LocalUser.IsHost)
                {
                    DeleteLobbyAsync();
                }
                else
                {
                    LeaveLobbyAsync();
                }
            }
        }

        /// <summary>
        /// Attempt to create a new lobby and then join it.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> TryCreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate)
        {
            if (!m_RateLimitHost.CanCall)
            {
                Debug.LogWarning("Create Lobby hit the rate limit.");
                return (false, null);
            }

            try
            {
                var lobby = await m_LobbyApiInterface.CreateLobby(AuthenticationService.Instance.PlayerId, lobbyName, maxPlayers, isPrivate, m_LocalUser.GetDataForUnityServices(), null);
                return (true, lobby);
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitHost.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join an existing lobby. Will try to join via code, if code is null - will try to join via ID.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> TryJoinLobbyAsync(string lobbyId, string lobbyCode)
        {
            if (!m_RateLimitJoin.CanCall ||
                (lobbyId == null && lobbyCode == null))
            {
                Debug.LogWarning("Join Lobby hit the rate limit.");
                return (false, null);
            }
            Debug.Log($"[LobbyServiceFacade] 로비 참가 요청 - 로비 아이디: {lobbyId}, 로비 코드: {lobbyCode}");
            try
            {
                if (!string.IsNullOrEmpty(lobbyCode))
                {
                    var lobby = await m_LobbyApiInterface.JoinLobbyByCode(AuthenticationService.Instance.PlayerId, lobbyCode, m_LocalUser.GetDataForUnityServices());
                    Debug.Log($"[LobbyServiceFacade] 로비 {lobby.Id}에 참가합니다");
                    return (true, lobby);
                }
                else
                {
                    var lobby = await m_LobbyApiInterface.JoinLobbyById(AuthenticationService.Instance.PlayerId, lobbyId, m_LocalUser.GetDataForUnityServices());
                    Debug.Log($"[LobbyServiceFacade] 로비 {lobby.Id}에 참가합니다 호호");
                    return (true, lobby);
                }
            }


            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    Debug.Log($"[LobbyServiceFacade] 로비 참가 요청 - 로비 아이디: {lobbyId}, 로비 코드: {lobbyCode} 레이트 리미트 처리");
                    m_RateLimitJoin.PutOnCooldown();
                }
                else
                {
                    Debug.Log($"[LobbyServiceFacade] 아아아아아앙아 시바 로비 참가 요청 - 로비 아이디: {lobbyId}, 로비 코드: {lobbyCode} 오류 발생");
                    PublishError(e);
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join the first lobby among the available lobbies that match the filtered onlineMode.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> TryQuickJoinLobbyAsync()
        {
            if (!m_RateLimitQuickJoin.CanCall)
            {
                Debug.LogWarning("Quick Join Lobby hit the rate limit.");
                return (false, null);
            }
            Debug.Log($"[LobbyServiceFacade] 로비 빠르게 참가 요청");
            try
            {
                Debug.Log($"<color=red>[LobbyServiceFacade] AuthenticationService.Instance.PlayerId: {AuthenticationService.Instance.PlayerId}</color>");
                Debug.Log($"<color=red>[LobbyServiceFacade] m_LocalUser.GetDataForUnityServices(): {m_LocalUser.GetDataForUnityServices()}</color>");
                Debug.Log($"<color=red>[LobbyServiceFacade] m_LocalLobby.LobbyID: {m_LocalLobby.LobbyID}</color>");
                Debug.Log($"<color=red>[LobbyServiceFacade] m_LocalLobby.LobbyUsers: {m_LocalLobby.LobbyUsers}</color>");
                Debug.Log($"<color=red>[LobbyServiceFacade] m_LocalLobby.LobbyUsers.Count: {m_LocalLobby.LobbyUsers.Count}</color>");
                Debug.Log($"<color=red>[LobbyServiceFacade] m_LocalLobby.LobbyUsers.Values: {m_LocalLobby.LobbyUsers.Values}</color>");
                Debug.Log($"<color=red>[LobbyServiceFacade] m_LocalLobby.LobbyUsers.Values.Count: {m_LocalLobby.LobbyUsers.Values.Count}</color>");

                var lobby = await m_LobbyApiInterface.QuickJoinLobby(AuthenticationService.Instance.PlayerId, m_LocalUser.GetDataForUnityServices());
                Debug.Log($"[LobbyServiceFacade] 로비 빠르게 참가 요청 성공");
                return (true, lobby);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"[LobbyServiceFacade] 로비 빠르게 참가 요청 실패");
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuickJoin.PutOnCooldown();
                }
                else
                {
                    Debug.Log($"[LobbyServiceFacade] 로비 빠르게 참가 요청 실패 오류 발생");
                    PublishError(e);
                }
            }

            return (false, null);
        }

        void ResetLobby()
        {
            CurrentUnityLobby = null;
            if (m_LocalUser != null)
            {
                m_LocalUser.ResetState();
            }
            if (m_LocalLobby != null && m_LocalUser != null)
            {
                m_LocalLobby.Reset(m_LocalUser);
            }

            // no need to disconnect Netcode, it should already be handled by Netcode's callback to disconnect
        }

        void OnLobbyChanges(ILobbyChanges changes)
        {
            if (changes.LobbyDeleted)
            {
                Debug.Log("Lobby deleted");
                ResetLobby();
                EndTracking();
            }
            else
            {
                Debug.Log("Lobby updated");
                changes.ApplyToLobby(CurrentUnityLobby);
                m_LocalLobby.ApplyRemoteData(CurrentUnityLobby);

                // 모든 플레이어가 준비되었는지 확인하고 씬 전환
                Debug.Log($"로비 사용자 수 확인: {m_LocalLobby.LobbyUsers.Count}/{MaxConnectedPlayers}");

                // as client, check if host is still in lobby
                if (!m_LocalUser.IsHost)
                {
                    foreach (var lobbyUser in m_LocalLobby.LobbyUsers)
                    {
                        if (lobbyUser.Value.IsHost)
                        {
                            return;
                        }
                    }

                    EndTracking();
                    // no need to disconnect Netcode, it should already be handled by Netcode's callback to disconnect
                }
            }
        }
        void OnKickedFromLobby()
        {
            Debug.Log("Kicked from Lobby");
            ResetLobby();
            EndTracking();
        }

        void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState lobbyEventConnectionState)
        {
            m_LobbyEventConnectionState = lobbyEventConnectionState;
            Debug.Log($"LobbyEventConnectionState changed to {lobbyEventConnectionState}");
        }

        async void SubscribeToJoinedLobbyAsync()
        {
            var lobbyEventCallbacks = new LobbyEventCallbacks();
            lobbyEventCallbacks.LobbyChanged += OnLobbyChanges;
            lobbyEventCallbacks.KickedFromLobby += OnKickedFromLobby;
            lobbyEventCallbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
            // The LobbyEventCallbacks object created here will now be managed by the Lobby SDK. The callbacks will be
            // unsubscribed from when we call UnsubscribeAsync on the ILobbyEvents object we receive and store here.
            m_LobbyEvents = await m_LobbyApiInterface.SubscribeToLobby(m_LocalLobby.LobbyID, lobbyEventCallbacks);
        }

        async void UnsubscribeToJoinedLobbyAsync()
        {
            if (m_LobbyEvents != null && m_LobbyEventConnectionState != LobbyEventConnectionState.Unsubscribed)
            {

                await m_LobbyEvents.UnsubscribeAsync();
            }
        }

        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        public async Task<(bool Success, Lobby Lobby)> FindAvailableLobby()
        {
            if (!m_RateLimitQuery.CanCall)
            {
                Debug.LogWarning("Retrieve Lobby list hit the rate limit. Will try again soon...");
                return (false, null);
            }

            try
            {
                var response = await m_LobbyApiInterface.QueryAllLobbies();
                
                // response가 null이거나 Results가 null인 경우 처리
                if (response == null || response.Results == null)
                {
                    Debug.Log("[LobbyServiceFacade] 로비 쿼리 결과가 null입니다");
                    return (false, null);
                }

                // 로비 목록이 비어있는 경우 처리
                if (response.Results.Count == 0)
                {
                    Debug.Log("[LobbyServiceFacade] 사용 가능한 로비가 없습니다");
                    return (false, null);
                }

                // 로비 목록을 발행
                m_LobbyListFetchedPub.Publish(new LobbyListFetchedMessage(LocalLobby.CreateLocalLobbies(response)));
                Debug.Log($"[LobbyServiceFacade] {response.Results.Count}개의 로비를 찾았습니다");

                // 첫 번째 사용 가능한 로비를 반환
                return (true, response.Results[0]);
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
                return (false, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyServiceFacade] 로비 검색 중 예외 발생: {e.Message}");
                return (false, null);
            }
        }

        public async Task<Lobby> ReconnectToLobbyAsync()
        {
            try
            {
                return await m_LobbyApiInterface.ReconnectToLobby(m_LocalLobby.LobbyID);
            }
            catch (LobbyServiceException e)
            {
                // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                if (e.Reason != LobbyExceptionReason.LobbyNotFound && !m_LocalUser.IsHost)
                {
                    PublishError(e);
                }
            }

            return null;
        }

        /// <summary>
        /// Attempt to leave a lobby
        /// </summary>
        async void LeaveLobbyAsync()
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            try
            {
                await m_LobbyApiInterface.RemovePlayerFromLobby(uasId, m_LocalLobby.LobbyID);
            }
            catch (LobbyServiceException e)
            {
                // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                if (e.Reason != LobbyExceptionReason.LobbyNotFound && !m_LocalUser.IsHost)
                {
                    PublishError(e);
                }
            }
            finally
            {
                ResetLobby();
            }

        }

        public async void RemovePlayerFromLobbyAsync(string uasId)
        {
            if (m_LocalUser.IsHost)
            {
                try
                {
                    await m_LobbyApiInterface.RemovePlayerFromLobby(uasId, m_LocalLobby.LobbyID);
                }
                catch (LobbyServiceException e)
                {
                    PublishError(e);
                }
            }
            else
            {
                Debug.LogError("Only the host can remove other players from the lobby.");
            }
        }

        public async void DeleteLobbyAsync()
        {
            if (m_LocalUser != null && m_LocalUser.IsHost)
            {
                try
                {
                    if (m_LocalLobby != null && !string.IsNullOrEmpty(m_LocalLobby.LobbyID))
                    {
                        await m_LobbyApiInterface.DeleteLobby(m_LocalLobby.LobbyID);
                    }
                }
                catch (LobbyServiceException e)
                {
                    PublishError(e);
                }
                finally
                {
                    ResetLobby();
                }
            }
            else
            {
                Debug.LogError("Only the host can delete a lobby.");
            }
        }

        /// <summary>
        /// Attempt to push a set of key-value pairs associated with the local player which will overwrite any existing
        /// data for these keys. Lobby can be provided info about Relay (or any other remote allocation) so it can add
        /// automatic disconnect handling.
        /// </summary>
        public async Task UpdatePlayerDataAsync(string allocationId, string connectionInfo)
        {
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            try
            {
                var result = await m_LobbyApiInterface.UpdatePlayer(CurrentUnityLobby.Id, AuthenticationService.Instance.PlayerId, m_LocalUser.GetDataForUnityServices(), allocationId, connectionInfo);
                Debug.Log($"<color=red>[LobbyServiceFacade] UpdatePlayerDataAsync 호출됨 - 결과: {result}</color>");
                if (result != null)
                {
                    CurrentUnityLobby = result; // Store the most up-to-date lobby now since we have it, instead of waiting for the next heartbeat.
                    Debug.Log($"<color=red>[LobbyServiceFacade] UpdatePlayerDataAsync 호출됨 - 결과: {result}</color>");
                }

            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
                else if (e.Reason != LobbyExceptionReason.LobbyNotFound && !m_LocalUser.IsHost) // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                {
                    PublishError(e);
                }
            }
        }

        /// <summary>
        /// Attempt to update the set of key-value pairs associated with a given lobby and unlocks it so clients can see it.
        /// </summary>
        public async Task UpdateLobbyDataAndUnlockAsync()
        {
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            var localData = m_LocalLobby.GetDataForUnityServices();

            var dataCurr = CurrentUnityLobby.Data;
            if (dataCurr == null)
            {
                dataCurr = new Dictionary<string, DataObject>();
            }

            foreach (var dataNew in localData)
            {
                if (dataCurr.ContainsKey(dataNew.Key))
                {
                    dataCurr[dataNew.Key] = dataNew.Value;
                }
                else
                {
                    dataCurr.Add(dataNew.Key, dataNew.Value);
                }
            }

            try
            {
                var result = await m_LobbyApiInterface.UpdateLobby(CurrentUnityLobby.Id, dataCurr, shouldLock: false);

                if (result != null)
                {
                    CurrentUnityLobby = result;
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.RateLimited)
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }
        }

        /// <summary>
        /// Lobby requires a periodic ping to detect rooms that are still active, in order to mitigate "zombie" lobbies.
        /// </summary>
        void DoLobbyHeartbeat(float dt)
        {
            m_HeartbeatTime += dt;
            if (m_HeartbeatTime > k_HeartbeatPeriod)
            {
                m_HeartbeatTime -= k_HeartbeatPeriod;
                try
                {
                    m_LobbyApiInterface.SendHeartbeatPing(CurrentUnityLobby.Id);


                }
                catch (LobbyServiceException e)
                {
                    // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                    if (e.Reason != LobbyExceptionReason.LobbyNotFound && !m_LocalUser.IsHost)
                    {
                        PublishError(e);
                    }
                }
            }
        }

        void PublishError(LobbyServiceException e)
        {
            var reason = e.InnerException == null ? e.Message : $"{e.Message} ({e.InnerException.Message})"; // Lobby error type, then HTTP error type.
            // m_UnityServiceErrorMessagePub.Publish(new UnityServiceErrorMessage("Lobby Error", reason, UnityServiceErrorMessage.Service.Lobby, e));
        }



    }

}