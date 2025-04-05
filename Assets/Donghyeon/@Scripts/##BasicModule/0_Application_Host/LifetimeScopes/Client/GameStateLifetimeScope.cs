using System;
using UnityEngine;
using VContainer.Unity;

    public enum GameState
    {
        MainMenu,
        CharSelect,
        BossRoom,
        PostGame,
        BasicGame
    }


    public abstract class GameStateLifetimeScope : LifetimeScope
    {
        /// <summary>
        /// Does this GameState persist across multiple scenes?
        /// </summary>
        public virtual bool Persists
        {
            get { return false; }
        }

        /// <summary>
        /// What GameState this represents. Server and client specializations of a state should always return the same enum.
        /// </summary>
        public abstract GameState ActiveState { get; }

        /// <summary>
        /// This is the single active GameState object. There can be only one.
        /// </summary>
        private static GameObject s_ActiveStateGO;

        protected override void Awake()
        {
            base.Awake();

            if (Parent != null)
            {
                Parent.Container.Inject(this);
            }
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            if (s_ActiveStateGO != null)
            {
                if (s_ActiveStateGO == gameObject)
                {
                    //nothing to do here, if we're already the active state object.
                    return;
                }

                //on the host, this might return either the client or server version, but it doesn't matter which;
                //we are only curious about its type, and its persist state.
                var previousState = s_ActiveStateGO.GetComponent<GameStateLifetimeScope>();

                if (previousState.Persists && previousState.ActiveState == ActiveState)
                {
                    //we need to make way for the DontDestroyOnLoad state that already exists.
                    Destroy(gameObject);
                    return;
                }

                //otherwise, the old state is going away. Either it wasn't a Persisting state, or it was,
                //but we're a different kind of state. In either case, we're going to be replacing it.
                Destroy(s_ActiveStateGO);
            }

            s_ActiveStateGO = gameObject;
            if (Persists)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected override void OnDestroy()
        {
            if (!Persists)
            {
                s_ActiveStateGO = null;
            }
        }
    }

