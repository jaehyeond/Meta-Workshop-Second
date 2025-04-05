using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Unity.Assets.Scripts.Data;
using static Define;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Unity.Assets.Scripts.Objects
{

    public class ServerHero : Creature
    {	
        [Inject] private DebugClassFacade _debugClassFacade;
        [Inject] public ObjectManager _objectManager;


        protected HeroData heroData;
        protected ClientHero clientHero;

        #region Singleton
        private static ServerHero instance;
        public static ServerHero Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ServerHero>();
                    if (instance == null)
                    {
                        Debug.LogError("[ServerHero] 인스턴스를 찾을 수 없습니다!");
                    }
                }
                return instance;
            }
            set => instance = value;
        }
        #endregion

        #region Fields

        [SerializeField] private HeroAvatarSO heroAvatarSO;
        [SerializeField] private string rarity;
        [SerializeField] public CreatureStat gachaSpawnWeight = new CreatureStat(0);
        [SerializeField] public CreatureStat gachaWeight = new CreatureStat(0);
        [SerializeField] public CreatureStat gachaExpCount = new CreatureStat(0);
        [SerializeField] public CreatureStat atkSpeed = new CreatureStat(0);
        [SerializeField] public CreatureStat atkTime = new CreatureStat(0);



        public bool NeedArrange { get; set; }

        // 네트워크 변수
        public NetworkVariable<bool> IsAttacking = new NetworkVariable<bool>();
        public NetworkVariable<int> HeroId = new NetworkVariable<int>();
  
        public event Action<ServerHero, bool> OnDataLoadComplete;
  
	    public Data.CreatureData CreatureData { get; private set; }
        private UI_Spawn_Holder parent_holder;

        #endregion

        // Fields 리전 내에 추가
#if UNITY_EDITOR
        [Header("===== 영웅 디버그 정보 =====")]
        [Space(5)]
        [SerializeField] private float _dbgGachaSpawnWeight;
        [SerializeField] private float _dbgGachaWeight;
        [SerializeField] private float _dbgGachaExpCount;
        [SerializeField] private float _dbgAtkSpeed;
        [SerializeField] private float _dbgAtkTime;
#endif

        public override void Update()
        {
            base.Update(); 

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                _dbgGachaSpawnWeight = gachaSpawnWeight.Value;
                _dbgGachaWeight = gachaWeight.Value;
                _dbgGachaExpCount = gachaExpCount.Value;
                _dbgAtkSpeed = atkSpeed.Value;
                _dbgAtkTime = atkTime.Value;
            }
#endif
        }

        EHeroMoveState _heroMoveState = EHeroMoveState.None;
        public EHeroMoveState HeroMoveState
        {
            get { return _heroMoveState; }
            private set
            {
                _heroMoveState = value;
                switch (value)
                {

                    case EHeroMoveState.TargetMonster:
                        NeedArrange = true;
                        break;
                    case EHeroMoveState.ForceMove:
                        NeedArrange = true;
                        break;
                }
            }
        }

        #region Unity Lifecycle
        private void Start()
        {
  
        }

        private void Awake()
        {
            base.Awake();
            Instance = this;


        }
        public override bool Init()
	    {
            if (base.Init() == false)
                return false;
            CreatureType = CharacterTypeEnum.Hero;
            ObjectType = EObjectType.Creature;
            Debug.Log($"<color=green>[ServerHero] Init: CreatureType 설정됨 = {CreatureType}</color>");
            return true;
        }


        public void SetParentHolder(UI_Spawn_Holder holder)
        {
            parent_holder = holder;

        }
      


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // ObjectManagerFacade를 통해 ObjectManager 참조 가져오기
            if (_objectManager == null)
            {
                var facade = FindAnyObjectByType<ObjectManagerFacade>();
                if (facade != null)
                {
                    _objectManager = facade._objectManager; // 참고: Manager는 ObjectManagerFacade에 있는 속성이어야 합니다
                }
 
            }
            if (IsServer)
            {
                // 이제 안전하게 NetworkVariable 설정
                HeroId.Value = _pendingHeroId;
                IsAttacking.Value = false;
            }
            
            StartCoroutine(CoUpdateAI());
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        private int _pendingHeroId;

        public override void SetInfo<T>(int templateID, Data.CreatureData creatureData, T clientHero) 
        where T : class	{
            Debug.Log($"<color=green>[ServerHero] SetInfo 시작. 현재 CreatureType: {CreatureType}</color>");
            base.SetInfo(templateID , creatureData, clientHero);
            DataTemplateID = templateID;
            _pendingHeroId = templateID;
            CreatureData = creatureData;
            gameObject.name = $"{CreatureData.DataId}_{CreatureData.CharacterType}";

            Debug.Log($"<color=green>[ServerHero] SetInfo: 데이터 설정 완료. CreatureType: {CreatureType}, DataId: {CreatureData.DataId}</color>");

            if (CreatureData is HeroData heroData)
            {
                rarity = heroData.Rarity;
                gachaSpawnWeight = new CreatureStat(heroData.GachaSpawnWeight);
                gachaWeight = new CreatureStat(heroData.GachaWeight);
                gachaExpCount = new CreatureStat(heroData.GachaExpCount);
                atkSpeed = new CreatureStat(heroData.AtkSpeed);
                atkTime = new CreatureStat(heroData.AtkTime);
                Debug.Log($"<color=green>[ServerHero] HeroData 세팅 완료: {heroData.DataId}</color>");
            }
            else
            {
                Debug.LogError($"<color=red>[ServerHero] CreatureData가 HeroData 타입이 아닙니다! 타입: {CreatureData.GetType().Name}</color>");
            }
        }


        #endregion
        public NetworkObject target;
        float atkSpeed_Coroutine_Value = 1.0f;
        private float attackTimer = 0f;

        protected override void UpdateIdle()
        {
            // 홀더 확인
            if (parent_holder == null)
            {
                _debugClassFacade?.LogWarning(GetType().Name, $"[ServerHero] UpdateIdle: parent_holder가 null입니다. 히어로 ID: {HeroId.Value}");
                return;
            }
            
            Debug.Log($"<color=green>[ServerHero] UpdateIdle: {HeroId.Value}, 홀더: {parent_holder.name}</color>");
            
            // 공격 타이머 업데이트
            attackTimer += Time.deltaTime * (atkSpeed.Value * atkSpeed_Coroutine_Value);
            
            // 1. 범위 내 몬스터 찾기
            BaseObject enemy = FindNearestEnemy();
            if (enemy != null)
            {
                _debugClassFacade?.LogInfo(GetType().Name, $"[ServerHero] 적 감지: {enemy.name}");
                
                Target = enemy;
                target = enemy.GetComponent<NetworkObject>();
                
                // 서버에서만 상태 변경
                if (IsServer)
                {
                    CreatureState = ECreatureState.Move;
                    HeroMoveState = EHeroMoveState.TargetMonster;
                }
                return;
            }
            
            // 2. 기타 상태 (필요시 추가)
        }

        //     // 1. 몬스터
        //     //Creature creature = FindClosestInRange(HERO_SEARCH_DISTANCE, Managers.Object.Monsters) as Creature;
        //     //if (creature != null)
        //     //{
        //     //    Target = creature;
        //     //    CreatureState = ECreatureState.Move;
        //     //    HeroMoveState = EHeroMoveState.TargetMonster;
        //     //    return;
        //     //}

        // }

        protected override void UpdateMove()
        {
            // 1. 몬스터 추적
            if (HeroMoveState == EHeroMoveState.TargetMonster)
            {
                // 몬스터가 죽었으면 포기
                if (Target == null || !Target.IsValid())
                {
                    HeroMoveState = EHeroMoveState.None;
                    if (IsServer)
                    {
                        CreatureState = ECreatureState.Idle;
                    }
                    return;
                }
                
                // 거리 계산 및 공격 또는 이동 결정
                ChaseOrAttackTarget(AtkRange.Value * 2.0f, AtkRange.Value);
                return;
            }
            
            // 2. 기타 이동 상태 (필요시 추가)
        }

        protected override void UpdateSkill()
        {
            // 공격 타이머 업데이트
            attackTimer += Time.deltaTime * (atkSpeed.Value * atkSpeed_Coroutine_Value);
            
            // 타겟이 유효하지 않으면 상태 변경
            if (Target == null || !Target.IsValid())
            {
                if (IsServer)
                {
                    CreatureState = ECreatureState.Idle;
                }
                return;
            }
            
            // 타겟이 범위를 벗어났는지 확인
            float distance = Vector2.Distance(parent_holder.transform.position, Target.transform.position);
            if (distance > AtkRange.Value)
            {
                if (IsServer)
                {
                    CreatureState = ECreatureState.Move;
                }
                return;
            }
            
            // 공격 실행
            if (attackTimer >= atkTime.Value)
            {
                attackTimer = 0f;
                Skills.CurrentSkill.DoSkill();
            }
        }


    public override void OnDamaged(BaseObject attacker, SkillBase skill)
    {
        // 몬스터로부터의 공격을 무시
        if (attacker is ServerMonster)
        {
            Debug.Log($"<color=blue>[ServerHero] 몬스터 {attacker.name}의 공격을 무시합니다!</color>");
            return; // 몬스터 공격은 처리하지 않음
        }
        // 다른 종류의 공격은 기본 처리
        base.OnDamaged(attacker, skill);
        Debug.Log($"<color=red>[ServerHero] 데미지를 받음. 남은 체력: {Hp}/{MaxHp.Value}</color>");
    }


    private BaseObject FindNearestEnemy()
    {
        if (parent_holder == null) return null;
        
        Debug.Log($"[ServerHero] 몬스터 탐색 시작");
        Debug.Log($"[ServerHero] AtkRange: {AtkRange.Value}");
        
        // Creature 클래스의 범용 간편 메서드 사용 (범위를 10배로 넓혀서 테스트)
        return FindNearestTargetInAttackRange(
            LayerNames.Monster,
            10f // AtkRange의 10배 범위로 검색
        );
    }


    }
}