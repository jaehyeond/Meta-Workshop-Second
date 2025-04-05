using System;
using Unity.Netcode;
using UnityEngine;
// using Spine.Unity;
using Unity.Assets.Scripts.Data;
using VContainer;
using Unity.Assets.Scripts.Resource;
// using Spine;
using UnityEngine.Rendering;

namespace Unity.Assets.Scripts.Objects
{
    public interface IClientCreature
{
    void SetAvatar(object avatar, object additionalParam = null);
}
    /// <summary>
    /// <see cref="ClientCharacter"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    public class ClientCreature : NetworkBehaviour
    {
        
        [SerializeField] private HeroAvatarSO heroAvatarSO;
        [SerializeField] Animator m_ClientVisualsAnimator;
        
        // [SerializeField] private SkeletonDataAsset m_skeletonDataAsset;

        [Inject] private ResourceManager _resourceManager;
        // [SerializeField]
        // VisualizationConfiguration m_VisualizationConfiguration;

        /// <summary>
        /// Returns a reference to the active Animator for this visualization
        /// </summary>
        // public Animator OurAnimator => m_ClientVisualsAnimator;
        // public SkeletonDataAsset SkeletonDataAsset => m_skeletonDataAsset;

	    // public SkeletonAnimation SkeletonAnim { get; private set; }

        // 수동으로 ResourceManager를 설정할 수 있는 속성 추가
        public ResourceManager ResourceManager 
        { 
            get { return _resourceManager; }
            set 
            { 
                if (_resourceManager == null)
                {
                    _resourceManager = value;
                    Debug.Log("[ClientCreature] ResourceManager가 수동으로 설정되었습니다.");
                }
            } 
        }

        public virtual void Awake(){}
        public virtual void SetAvatar(HeroAvatarSO avatarSO , string SkeletonDataID)
        {
            heroAvatarSO = avatarSO;

            if(SkeletonDataID == "" || SkeletonDataID == null)
            {
                Debug.Log("SPRITE RENDERER 추가");
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    // Set해야함 avatar에서 꺼내서
                }

                // 애니메이션 컨트롤러 추가
                Animator animator = GetComponent<Animator>();
                if (animator == null)
                {
                    Debug.Log("Animator 추가");
                    animator = gameObject.AddComponent<Animator>();
                    // 필요시 애니메이션 컨트롤러 설정
                }
            }
            // else
            // {
            //     SkeletonAnim = GetComponent<SkeletonAnimation>();
            //     if (SkeletonAnim == null)
            //     {
            //         Debug.Log("SkeletonAnimation 추가");
            //         SkeletonAnim = gameObject.AddComponent<SkeletonAnimation>();    
            //         if (_resourceManager == null)
            //         {
            //             Debug.LogError("[ClientCreature] ResourceManager가 주입되지 않았습니다. VContainer 설정을 확인해주세요.");
            //             // 이 경우 SetSpineAnimation 호출이 실패할 가능성이 높습니다.
            //             // 실제 프로젝트에서는 ResourceManager를 올바르게 주입하도록 구현해야 합니다.
            //             return;
            //         }
            //         SetSpineAnimation(SkeletonDataID, SortingLayers.CREATURE);
            //     }
            // }
          
            
        }

        // ResourceManager를 전달받는 오버로드 추가
        public virtual void SetAvatar(HeroAvatarSO avatarSO, string SkeletonDataID, ResourceManager resourceManager)
        {
            // 리소스 매니저 설정
            if (_resourceManager == null && resourceManager != null)
            {
                _resourceManager = resourceManager;
                Debug.Log("[ClientCreature] ResourceManager가 SetAvatar 호출에서 설정되었습니다.");
            }
            
            // 기존 메서드 호출
            SetAvatar(avatarSO, SkeletonDataID);
        }

        #region Spine
        protected virtual void SetSpineAnimation(string dataLabel, int sortingOrder)
        {
            if (_resourceManager == null)
            {
                Debug.LogError("ResourceManager가 null입니다!");
                return;
            }

            // SkeletonAnim.skeletonDataAsset = _resourceManager.Load<SkeletonDataAsset>(dataLabel);
            // SkeletonAnim.Initialize(true);

            // // Register AnimEvent
            // if (SkeletonAnim.AnimationState != null)
            // {
            //     SkeletonAnim.AnimationState.Event -= OnAnimEventHandler;
            //     SkeletonAnim.AnimationState.Event += OnAnimEventHandler;
            // }

            // Spine SkeletonAnimation은 SpriteRenderer 를 사용하지 않고 MeshRenderer을 사용함
            // 그렇기떄문에 2D Sort Axis가 안먹히게 되는데 SortingGroup을 SpriteRenderer,MeshRenderer을 같이 계산함.
            SortingGroup sg = Util.GetOrAddComponent<SortingGroup>(gameObject);
            sg.sortingOrder = sortingOrder;
        }

        protected virtual void UpdateAnimation()
        {
        }

        // public TrackEntry PlayAnimation(int trackIndex, string animName, bool loop)
        // {
        //     if (SkeletonAnim == null)
        //         return null;

        //     TrackEntry entry = SkeletonAnim.AnimationState.SetAnimation(trackIndex, animName, loop);

        //     if (animName == AnimName.DEAD)
        //         entry.MixDuration = 0;
        //     else
        //         entry.MixDuration = 0.2f;

        //     return entry;
        // }

        // public void AddAnimation(int trackIndex, string AnimName, bool loop, float delay)
        // {
        //     if (SkeletonAnim == null)
        //         return;

        //     SkeletonAnim.AnimationState.AddAnimation(trackIndex, AnimName, loop, delay);
        // }

        // public void Flip(bool flag)
        // {
        //     if (SkeletonAnim == null)
        //         return;

        //     SkeletonAnim.Skeleton.ScaleX = flag ? -1 : 1;
        // }

        // public virtual void OnAnimEventHandler(TrackEntry trackEntry, Spine.Event e)
        // {
        //     Debug.Log("OnAnimEventHandler");
        // }
        #endregion


        /// <summary>
        /// This RPC is invoked on the client when the active action FXs need to be cancelled (e.g. when the character has been stunned)
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void ClientCancelAllActionsRpc()
        {
            // m_ClientActionViz.CancelAllActions();
        }

        /// <summary>
        /// This RPC is invoked on the client when active action FXs of a certain type need to be cancelled (e.g. when the Stealth action ends)
        /// </summary>
        // [Rpc(SendTo.ClientsAndHost)]
        // public void ClientCancelActionsByPrototypeIDRpc(ActionID actionPrototypeID)
        // {
        //     m_ClientActionViz.CancelAllActionsWithSamePrototypeID(actionPrototypeID);
        // }

        /// <summary>
        /// Called on all clients when this character has stopped "charging up" an attack.
        /// Provides a value between 0 and 1 inclusive which indicates how "charged up" the attack ended up being.
        /// </summary>
        // [Rpc(SendTo.ClientsAndHost)]
        // public void ClientStopChargingUpRpc(float percentCharged)
        // {
        //     m_ClientActionViz.OnStoppedChargingUp(percentCharged);
        // }


        public override void OnNetworkSpawn()
        {
            if (!IsClient || transform.parent == null)
            {
                return;
            }

            enabled = true;

            // ResourceManager가 주입되었는지 확인
            if (_resourceManager == null)
            {
                Debug.LogError("[ClientCreature] ResourceManager가 주입되지 않았습니다. VContainer 설정을 확인해주세요.");
                // 이 경우 SetSpineAnimation 호출이 실패할 가능성이 높습니다.
                // 실제 프로젝트에서는 ResourceManager를 올바르게 주입하도록 구현해야 합니다.
            }

            // m_ClientActionViz = new ClientActionPlayer(this);
     
        }

        public override void OnNetworkDespawn()
        {

            enabled = false;
        }

        // void OnActionInput(ActionRequestData data)
        // {
        //     m_ClientActionViz.AnticipateAction(ref data);
        // }

        void OnMoveInput(Vector3 position)
        {
            // if (!IsAnimating())
            // {
            //     OurAnimator.SetTrigger(m_VisualizationConfiguration.AnticipateMoveTriggerID);
            // }
        }

        void OnStealthyChanged(bool oldValue, bool newValue)
        {
            SetAppearanceSwap();
        }

        void SetAppearanceSwap()
        {

        }

        /// <summary>
        /// Returns the value we should set the Animator's "Speed" variable, given current gameplay conditions.
        /// </summary>
        // float GetVisualMovementSpeed(MovementStatus movementStatus)
        // {
            // return 0;
            // if (m_ServerCharacter.NetLifeState.LifeState.Value != LifeState.Alive)
            // {
            //     return m_VisualizationConfiguration.SpeedDead;
            // }

            // switch (movementStatus)
            // {
            //     case MovementStatus.Idle:
            //         return m_VisualizationConfiguration.SpeedIdle;
            //     case MovementStatus.Normal:
            //         return m_VisualizationConfiguration.SpeedNormal;
            //     case MovementStatus.Uncontrolled:
            //         return m_VisualizationConfiguration.SpeedUncontrolled;
            //     case MovementStatus.Slowed:
            //         return m_VisualizationConfiguration.SpeedSlowed;
            //     case MovementStatus.Hasted:
            //         return m_VisualizationConfiguration.SpeedHasted;
            //     case MovementStatus.Walking:
            //         return m_VisualizationConfiguration.SpeedWalking;
            //     default:
            //         throw new Exception($"Unknown MovementStatus {movementStatus}");
            // }
        // }

        // void OnMovementStatusChanged(MovementStatus previousValue, MovementStatus newValue)
        // {
        //     m_CurrentSpeed = GetVisualMovementSpeed(newValue);
        // }

        void Update()
        {
            // On the host, Characters are translated via ServerCharacterMovement's FixedUpdate method. To ensure that
            // the game camera tracks a GameObject moving in the Update loop and therefore eliminate any camera jitter,
            // this graphics GameObject's position is smoothed over time on the host. Clients do not need to perform any
            // positional smoothing since NetworkTransform will interpolate position updates on the root GameObject.
            // if (IsHost)
            // {
            //     // Note: a cached position (m_LerpedPosition) and rotation (m_LerpedRotation) are created and used as
            //     // the starting point for each interpolation since the root's position and rotation are modified in
            //     // FixedUpdate, thus altering this transform (being a child) in the process.
            //     m_LerpedPosition = m_PositionLerper.LerpPosition(m_LerpedPosition,
            //         serverCharacter.physicsWrapper.Transform.position);
            //     m_LerpedRotation = m_RotationLerper.LerpRotation(m_LerpedRotation,
            //         serverCharacter.physicsWrapper.Transform.rotation);
            //     transform.SetPositionAndRotation(m_LerpedPosition, m_LerpedRotation);
            // }

            // if (m_ClientVisualsAnimator)
            // {
            //     // set Animator variables here
            //     OurAnimator.SetFloat(m_VisualizationConfiguration.SpeedVariableID, m_CurrentSpeed);
            // }

            // m_ClientActionViz.OnUpdate();
        }

        void OnAnimEvent(string id)
        {
            //if you are trying to figure out who calls this method, it's "magic". The Unity Animation Event system takes method names as strings,
            //and calls a method of the same name on a component on the same GameObject as the Animator. See the "attack1" Animation Clip as one
            //example of where this is configured.

            // m_ClientActionViz.OnAnimEvent(id);
        }

        public bool IsAnimating()
        {
            // if (OurAnimator.GetFloat(m_VisualizationConfiguration.SpeedVariableID) > 0.0) { return true; }

            // for (int i = 0; i < OurAnimator.layerCount; i++)
            // {
            //     if (OurAnimator.GetCurrentAnimatorStateInfo(i).tagHash != m_VisualizationConfiguration.BaseNodeTagID)
            //     {
            //         //we are in an active node, not the default "nothing" node.
            //         return true;
            //     }
            // }

            return false;
        }
    }
}
