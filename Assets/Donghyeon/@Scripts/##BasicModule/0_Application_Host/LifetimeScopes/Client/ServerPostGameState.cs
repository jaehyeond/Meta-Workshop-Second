using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

//TODO WIN, LOSE 상태 추가 // LOST일시 ACTION 추가 //CONNECTIONMANAGER 끊기

    [RequireComponent(typeof(NetcodeHooks))]
    public class ServerPostGameState : GameStateLifetimeScope
    {
        [SerializeField]
        NetcodeHooks m_NetcodeHooks;

        // [FormerlySerializedAs("synchronizedStateData")]
        // [SerializeField]
        // NetworkPostGame networkPostGame;
        // public NetworkPostGame NetworkPostGame => networkPostGame;

        public override GameState ActiveState { get { return GameState.PostGame; } }

        // [Inject]
        // ConnectionManager m_ConnectionManager;

        // [Inject]
        // PersistentGameState m_PersistentGameState;

        protected override void Awake()
        {
            base.Awake();

            m_NetcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
        }

        void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
            }
            else
            {
                // SessionManager<SessionPlayerData>.Instance.OnSessionEnded();
                // networkPostGame.WinState.Value = m_PersistentGameState.WinState;
            }
        }

        protected override void OnDestroy()
        {

            base.OnDestroy();

            m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
        }

        public void PlayAgain()
        {
            // SceneLoaderWrapper.Instance.LoadScene("CharSelect", useNetworkSceneManager: true);
        }

        public void GoToMainMenu()
        {
            // m_ConnectionManager.RequestShutdown();
        }
    }
