// using Unity.Assets.Scripts.Gameplay.GameplayObjects.Character;
using System;
using System.Collections.Generic;
using Unity.Assets.Scripts.Data;
using Unity.Assets.Scripts.Resource;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 네트워크 멀티플레이어 게임에서 몬스터의 클라이언트 측 시각화를 담당하는 클래스입니다.
    /// ClientCharacter를 상속받아 네트워크 기능을 활용합니다.
    /// </summary>
    public class ClientHero : ClientCreature , IClientCreature
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private AudioSource audioSource;
        
        [Header("사운드 효과")]
        [SerializeField] private AudioClip spawnSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip deathSound;
        

        // 이전 방향 저장 (스프라이트 뒤집기용)





        public override void Awake(){
            base.Awake();
        }
        
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // CreatureState 변경 감지
            var creature = GetComponent<Creature>();
            if (creature != null)
            {
                creature.NetworkCreatureState.OnValueChanged += OnCreatureStateChanged;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            var creature = GetComponent<Creature>();
            if (creature != null)
            {
                creature.NetworkCreatureState.OnValueChanged -= OnCreatureStateChanged;
            }
        }
        
        private void OnCreatureStateChanged(ECreatureState previousValue, ECreatureState newValue)
        {
            UpdateAnimation();
        }
        protected override void UpdateAnimation()
        {
            var creature = GetComponent<Creature>();
            if (creature == null) return;

            switch (creature.CreatureState)
            {
                case ECreatureState.Idle:
                    // PlayAnimation(0, AnimName.IDLE, true);
                    break;
                case ECreatureState.Skill:
                    // PlayAnimation(0, AnimName.ATTACK_A, true);
                    break;
                case ECreatureState.Move:
                    // PlayAnimation(0, AnimName.MOVE, true);
                    break;
                case ECreatureState.OnDamaged:
                    // PlayAnimation(0, AnimName.IDLE, true);
                    break;
                case ECreatureState.Dead:
                    // PlayAnimation(0, AnimName.DEAD, true);
                    break;
                default:
                    break;
            }
        }
        public void SetAvatar(object avatar, object additionalParam = null)
        {
            if (avatar is HeroAvatarSO heroAvatar)
            {
                if (additionalParam is (string skeletonDataID, ResourceManager resourceManager))
                {
                     base.SetAvatar(heroAvatar, skeletonDataID, resourceManager);
                }
            }
        }
        
        
        // // ResourceManager를 전달받는 오버로드 추가
        // public override void SetAvatar(HeroAvatarSO avatarSO, string skeletonDataID, ResourceManager resourceManager)
        // {
        //     base.SetAvatar(avatarSO, skeletonDataID, resourceManager);
        // }

   
    }
}