using UnityEngine;
// using Spine.Unity;


namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 생물체의 시각적 요소와 데이터 참조를 관리하는 ScriptableObject의 기본 클래스입니다.
    /// 이 클래스는 프리팹, 스프라이트, 애니메이터 컨트롤러와 같은 시각적 요소를 포함합니다.
    /// </summary>
    public abstract class CreatureAvatarSO : GuidScriptableObject
    {
        [Header("식별 정보")]
        [Tooltip("생물체의 고유 ID (CreatureStatsSO의 DataId와 일치해야 함)")]
        [SerializeField] protected int dataId;
        
        
        [Tooltip("생물체의 대표 스프라이트")]
        [SerializeField] public Sprite creatureSprite;
        
        [Tooltip("생물체의 애니메이션 컨트롤러")]
        [SerializeField] protected RuntimeAnimatorController animatorController; // AnimatorController 대신
        
        [Header("오디오")]
        [Tooltip("생물체의 기본 사운드 효과")]
        [SerializeField] protected AudioClip[] creatureSounds;
        
        [Header("이펙트")]
        [Tooltip("생물체의 스폰 이펙트")]
        [SerializeField] protected GameObject spawnEffectPrefab;
        
        [Tooltip("생물체의 사망 이펙트")]
        [SerializeField] protected GameObject deathEffectPrefab;
        

        // [Tooltip("스켈레톤데이터")]
        // [SerializeField] public SkeletonDataAsset skeletonAnim;
        /// <summary>
        /// 생물체의 고유 ID를 반환합니다.
        /// </summary>
        public int DataId => dataId;
        
    
        
        /// <summary>
        /// 생물체의 게임 오브젝트 프리팹을 반환합니다.
        /// </summary>
        
        /// <summary>
        /// 생물체의 대표 스프라이트를 반환합니다.
        /// </summary>
        public Sprite CreatureSprite => creatureSprite;
        
        /// <summary>
        /// 생물체의 애니메이션 컨트롤러를 반환합니다.
        /// </summary>
        public RuntimeAnimatorController AnimatorController => animatorController;
        
        /// <summary>
        /// 생물체의 사운드 효과 배열을 반환합니다.
        /// </summary>
        public AudioClip[] CreatureSounds => creatureSounds;
        
        /// <summary>
        /// 생물체의 스폰 이펙트 프리팹을 반환합니다.
        /// </summary>
        public GameObject SpawnEffectPrefab => spawnEffectPrefab;
        
        /// <summary>
        /// 생물체의 사망 이펙트 프리팹을 반환합니다.
        /// </summary>
        public GameObject DeathEffectPrefab => deathEffectPrefab;
        
        // public SkeletonDataAsset SkeletonAnim => skeletonAnim;

        /// <summary>
        /// 생물체의 데이터 참조를 반환합니다.
        /// 하위 클래스에서 구현해야 합니다.
        /// </summary>
        // public void SetSkeletonAnimation(SkeletonDataAsset skeleton)
        // {
        //     skeletonAnim = skeleton;
            
        //     #if UNITY_EDITOR
        //     // 에디터에서 변경사항 저장
        //     UnityEditor.EditorUtility.SetDirty(this);
        //     UnityEditor.AssetDatabase.SaveAssets();
        //     #endif
        // }
        /// <summary>
        /// 애니메이션 컨트롤러를 설정합니다.
        /// </summary>
        /// <param name="controller">설정할 애니메이션 컨트롤러</param>
        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            animatorController = controller;
            
            #if UNITY_EDITOR
            // 에디터에서 변경사항 저장
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
        
        /// <summary>
        /// 스프라이트를 설정합니다.
        /// </summary>
        /// <param name="sprite">설정할 스프라이트</param>
        public void SetCreatureSprite(Sprite sprite)
        {
            creatureSprite = sprite;
            
            #if UNITY_EDITOR
            // 에디터에서 변경사항 저장
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
        
        /// <summary>
        /// 프리팹을 설정합니다.
        /// </summary>
        /// <param name="prefab">설정할 프리팹</param>
        public void SetCreaturePrefab(GameObject prefab)
        {
            // creaturePrefab = prefab;
            
            #if UNITY_EDITOR
            // 에디터에서 변경사항 저장
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
        
        /// <summary>
        /// 데이터 ID를 설정합니다.
        /// </summary>
        /// <param name="id">설정할 데이터 ID</param>
        public void SetDataId(int id)
        {
            dataId = id;
            
            #if UNITY_EDITOR
            // 에디터에서 변경사항 저장
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
        
 
    }
}