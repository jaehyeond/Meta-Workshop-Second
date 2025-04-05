using System;
using UnityEngine;
using Unity.Assets.Scripts.Data;
using Unity.Assets.Scripts.Resource;

namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 몬스터의 시각적 요소와 데이터 참조를 관리하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterAvatar", menuName = "GameData/MonsterAvatar", order = 2)]
    public class MonsterAvatarSO : CreatureAvatarSO
    {
 
        
        [Header("몬스터 전용 이펙트")]
        [Tooltip("몬스터의 공격 이펙트")]
        [SerializeField] private GameObject attackEffectPrefab;
        
        [Tooltip("몬스터의 피격 이펙트")]
        [SerializeField] private GameObject hitEffectPrefab;
        

        public GameObject AttackEffectPrefab => attackEffectPrefab;
 
        public GameObject HitEffectPrefab => hitEffectPrefab;
        
  
        
    }
}