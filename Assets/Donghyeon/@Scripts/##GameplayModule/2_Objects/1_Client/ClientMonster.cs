// using Unity.Assets.Scripts.Gameplay.GameplayObjects.Character;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 네트워크 멀티플레이어 게임에서 몬스터의 클라이언트 측 시각화를 담당하는 클래스입니다.
    /// ClientCharacter를 상속받아 네트워크 기능을 활용합니다.
    /// </summary>
    public class ClientMonster : ClientCreature , IClientCreature
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private AudioSource audioSource;
        
        [Header("사운드 효과")]
        [SerializeField] private AudioClip spawnSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip deathSound;
        

        [SerializeField] private MonsterAvatarSO MonsterAvatarSO;

        // 이전 방향 저장 (스프라이트 뒤집기용)
        private Vector2 m_PrevDirection = Vector2.right;
        
        private void Awake()
        {
            // Debug.Log("<color=green>ClientMonster Awake</color>");
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Debug.Log("<color=green>ClientMonster Awake###########################</color>");

   
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

        }
        public void SetAvatar(object avatar, object additionalParam = null)
        {
            if (avatar is MonsterAvatarSO monsterAvatar)
            {
               MonsterAvatarSO = monsterAvatar;
            }
        }
    
        /// <summary>
        /// 이동 방향에 따라 애니메이션과 스프라이트 방향을 업데이트합니다.
        /// </summary>
        /// <param name="direction">이동 방향</param>
        public void UpdateMovementVisuals(Vector2 direction)
        {
            if (direction.magnitude > 0.1f)
            {
                m_PrevDirection = direction;
            }
            
            // 이동 애니메이션
            if (animator != null)
            {
                animator.SetBool("IsMoving", direction.magnitude > 0.1f);
            }
            
            // 방향에 따른 스프라이트 뒤집기
            if (spriteRenderer != null && m_PrevDirection.x != 0)
            {
                spriteRenderer.flipX = m_PrevDirection.x < 0;
            }
        }
        
        /// <summary>
        /// 사운드를 재생합니다.
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
        }


    }
}