using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Unity.Assets.Scripts.Data;
using static Define;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Assets.Scripts.Objects
{
 
    public class ServerMonster : Creature
    {	

        [Inject] private DebugClassFacade _debugClassFacade;

        #region Singleton
        protected MonsterData monsterData;
        private static ServerMonster instance;
        public static ServerMonster Instance
        {
            get
            {
                if (instance == null)
                {
                    instance =FindFirstObjectByType<ServerMonster>();
                    if (instance == null)
                    {
                        Debug.LogError("[ServerMonster] 인스턴스를 찾을 수 없습니다!");
                    }
                }
                return instance;
            }
            set => instance = value;
        }
        #endregion

        #region Fields
        [Header("===== 몬스터 설정 =====")]
        [Space(10)]
        [SerializeField] private MonsterAvatarSO monsterAvatarSO;
        [SerializeField] private int dropItemId;
        

        // 네트워크 변수
        public NetworkVariable<float> CurrentHp = new NetworkVariable<float>();
        public NetworkVariable<bool> IsAttacking = new NetworkVariable<bool>();
        public NetworkVariable<int> MonsterId = new NetworkVariable<int>();
        
        // 이벤트
        public event Action<ServerMonster> OnMonsterDeath;
        public event Action<ServerMonster, float> OnMonsterDamaged;
        public event Action<ServerMonster, bool> OnDataLoadComplete;
        
        // 몬스터 데이터
        
        public int DropItemId => dropItemId;
	    public Data.CreatureData CreatureData { get; private set; }

        // 이동 관련
        private int target_Value = 0;
        private List<Vector2> _moveList = new List<Vector2>();

        
        public NetworkVariable<Vector3> NetworkPosition = new NetworkVariable<Vector3>();
        private bool positionInitialized = false;
        #endregion

        public override ECreatureState CreatureState 
        {
            get { return base.CreatureState; }
            set
            {
                if (_creatureState.Value != value)
                {
                    base.CreatureState = value;
                    switch (value)
                    {
                        case ECreatureState.Idle:
                            UpdateAITick = 0.5f;
                            break;
                        case ECreatureState.Move:
                            UpdateAITick = 0.0f;
                            break;
                        case ECreatureState.Skill:
                            UpdateAITick = 0.0f;
                            break;
                        case ECreatureState.Dead:
                            UpdateAITick = 1.0f;
                            break;
                    }
                }
            }
        }

        #region Unity Lifecycle
        private void Awake()
        {
            base.Awake();
            Instance = this;

        }

     	public override bool Init()
	    {
            if (base.Init() == false)
                return false;
            CreatureType = CharacterTypeEnum.Monster;
            ObjectType = EObjectType.Creature;
            Debug.Log($"<color=red>[ServerMonster] Init: CreatureType 설정됨 = {CreatureType}</color>");
            return true;
        }


        private int _pendingMonsterId;
        public override void SetInfo<T>(int templateID, Data.CreatureData creatureData, T clientCreature) 
        where T : class	{
            Debug.Log($"<color=red>[ServerMonster] SetInfo 시작. 현재 CreatureType: {CreatureType}</color>");
            base.SetInfo(templateID, creatureData, clientCreature);
            _pendingMonsterId = templateID;
            DataTemplateID = templateID;
            CreatureData = creatureData;
            gameObject.name = $"{CreatureData.DataId}_{CreatureData.CharacterType}";
            gameObject.layer = LayerNames.Monster;
            
            Debug.Log($"<color=red>[ServerMonster] SetInfo: 데이터 설정 완료. CreatureType: {CreatureType}, DataId: {CreatureData.DataId}, Layer: {LayerMask.LayerToName(gameObject.layer)}</color>");

            if (CreatureData is MonsterData monsterData)
            {
                dropItemId = monsterData.DropItemId;
                Debug.Log($"<color=red>[ServerMonster] MonsterData 세팅 완료: {monsterData.DataId}, DropItemId: {dropItemId}</color>");
            }
            else
            {
                Debug.LogError($"<color=red>[ServerMonster] CreatureData가 MonsterData 타입이 아닙니다! 타입: {CreatureData.GetType().Name}</color>");
            }
        }


        protected override void UpdateIdle()
        {   CreatureState = ECreatureState.Move;
            // Not Implemented this time
            // Patrol 
            // {
            //     int patrolPercent = 10;
            //     int rand = Random.Range(0, 100);
            //     if (rand <= patrolPercent)
            //     {
            //         _destPos = _initPos + new Vector3(Random.Range(-2, 2), Random.Range(-2, 2));
            //         CreatureState = ECreatureState.Move;
            //         return;
            //     }
            // }

        }
        protected override void UpdateMove(){}
        
        protected override void UpdateSkill(){}

        protected override void UpdateDead(){}

        public override void OnDamaged(BaseObject attacker, SkillBase skill)
        {
            base.OnDamaged(attacker, skill);  // 부모 클래스의 OnDamaged 호출 (여기서 Hp 값이 변경됨)
            
            // 네트워크 변수인 CurrentHp도 함께 업데이트
            CurrentHp.Value = Hp;
            
            Debug.Log($"<color=yellow>[ServerMonster] Damaged by {attacker.name}, HP: {Hp}/{MaxHp.Value}</color>");
            
            // HP가 0 이하면 죽음 상태로 변경
            if (Hp <= 0)
            {
                Debug.Log($"<color=red>[ServerMonster] 사망!</color>");
                CreatureState = ECreatureState.Dead;
                OnDead(attacker, skill);
            }
        }
        public override void OnDead(BaseObject attacker, SkillBase skill)
        {
            base.OnDead(attacker, skill);
            Debug.Log($"<color=red>[ServerMonster] OnDead called!</color>");
            
            // 몬스터 사망 이벤트 발생
            OnMonsterDeath?.Invoke(this);
            
            // 죽음 상태로 변경 (혹시 상위 OnDead에서 처리되지 않았을 경우)
            CreatureState = ECreatureState.Dead;
            
            // 몬스터 오브젝트 제거
            _objectManager.Despawn(this);
        }

        // FixedUpdate 메소드 수정
        private void FixedUpdate()
        {
            if (IsServer)
            {
                HandleServerMovement();
            }
            else if (IsClient)
            {
                HandleClientMovement();
            }
        }

        private void HandleServerMovement()
        {
            if (_moveList.Count == 0 || target_Value >= _moveList.Count) return;

            // Calculate new position with movement speed
            Vector3 newPosition = Vector2.MoveTowards(transform.position, _moveList[target_Value], Time.deltaTime * 1);

            transform.position = newPosition;

            // Update network position (critical for clients)
            NetworkPosition.Value = newPosition;

            // Debug movement
            _debugClassFacade?.LogInfo(GetType().Name, $"[ServerMonster:{MonsterId.Value}] Moving: {transform.position} → {_moveList[target_Value]}, Speed: {MoveSpeed.Value}");

            if (Vector2.Distance(transform.position, _moveList[target_Value]) <= 0.1f)
            {
                _debugClassFacade?.LogInfo(GetType().Name, $"[ServerMonster:{MonsterId.Value}] Reached point {target_Value}");
                target_Value++;
                if (target_Value >= 4) // Using hardcoded 4 for path length
                {
                    target_Value = 0;
                }
            }
        }

        private void HandleClientMovement()
        {
            if (!positionInitialized && NetworkPosition.Value != Vector3.zero)
            {
                positionInitialized = true;
                transform.position = NetworkPosition.Value;
                _debugClassFacade?.LogInfo(GetType().Name, $"[ServerMonster:{MonsterId.Value}] Initial client position set to {NetworkPosition.Value}");
            }

            if (positionInitialized)
            {
                // Use MoveTowards for smoother client-side movement rather than direct position setting
                transform.position = Vector3.MoveTowards(transform.position, NetworkPosition.Value, Time.deltaTime * 1);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
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
                MonsterId.Value = _pendingMonsterId;
                CurrentHp.Value = Hp;
                
                // CreatureState도 여기서 설정
                CreatureState = ECreatureState.Idle;
            }
            
            StartCoroutine(CoUpdateAI());
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            CurrentHp.OnValueChanged -= OnHpChanged;
        }


        
        #endregion

        #region Initialization
    
        #endregion

        #region Movement
        private void HandleMovement()
        {
            // ClientMonster처럼 간단한 체크
            if (_moveList.Count == 0 || target_Value >= _moveList.Count) return;

            // ClientMonster와 유사한 이동 방식 사용
            transform.position = Vector2.MoveTowards(transform.position, _moveList[target_Value], Time.deltaTime * MoveSpeed.Value);
            
            if (Vector2.Distance(transform.position, _moveList[target_Value]) <= 0.0f)
            {
                target_Value++;
                
                // ClientMonster와 유사한 스프라이트 flip 처리
                // SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                // if (spriteRenderer != null)
                // {
                //     spriteRenderer.flipX = target_Value >= 3;
                // }
                
                // ClientMonster와 유사한 경로 순환 처리 (하드코딩된 4 사용)
                if (target_Value >= 4)
                {
                    target_Value = 0;
                }
            }
        }

        public void SetMoveList(List<Vector2> moveList)
        {
            if (moveList == null || moveList.Count == 0)
            {
                _debugClassFacade?.LogError(GetType().Name, $"[ServerMonster:{MonsterId.Value}] SetMoveList: 유효하지 않은 경로 데이터");
                return;
            }
            _moveList = moveList;
            target_Value = 0;
        }
        #endregion

     


 

        #region Combat
        public void TakeDamage(float damage)
        {
            if (!IsServer)
            {
                _debugClassFacade?.LogWarning(GetType().Name, "TakeDamage는 서버에서만 호출해야 합니다!");
                return;
            }
            
            if (CurrentHp.Value <= 0) return;
            
            CurrentHp.Value -= damage;
            Hp = CurrentHp.Value;
        }

        private void OnHpChanged(float oldValue, float newValue)
        {
            Hp = newValue;
            
            if (newValue <= 0 && oldValue > 0)
            {
                OnMonsterDeath?.Invoke(this);
            }
            else if (newValue < oldValue)
            {
                OnMonsterDamaged?.Invoke(this, oldValue - newValue);
            }
        }

        /// <summary>
        /// 가장 가까운 영웅을 찾는 메서드
        /// </summary>
        /// <param name="rangeMultiplier">AtkRange의 배수 (기본값: 1)</param>
        /// <param name="debugDraw">디버그 로그 표시 여부</param>
        /// <returns>가장 가까운 영웅 객체</returns>
        public BaseObject FindNearestHero(float rangeMultiplier = 1f)
        {
            Debug.Log($"<color=purple>[ServerMonster] 영웅 탐색 시작, 범위 배수: {rangeMultiplier}</color>");
            // Creature 클래스의 범용 간편 메서드 사용
            return FindNearestTargetInAttackRange(
                LayerNames.Hero,
                rangeMultiplier
            );
        }

        /// <summary>
        /// 범위 내 모든 영웅 찾기
        /// </summary>
        /// <param name="rangeMultiplier">AtkRange의 배수 (기본값: 1)</param>
        /// <param name="maxResults">최대 결과 수 (0 = 무제한)</param>
        /// <returns>범위 내 영웅 리스트</returns>
        public List<BaseObject> FindAllHeroesInRange(float rangeMultiplier = 1f, int maxResults = 0)
        {
            return FindAllTargetsInAttackRange(
                LayerNames.Hero,
                rangeMultiplier,
                maxResults
            );
        }

        #endregion


	// RewardData GetRandomReward()
	// {
	// 	if (MonsterData == null)
	// 		return null;

	// 	if (Managers.Data.DropTableDic.TryGetValue(MonsterData.DropItemId, out DropTableData dropTableData) == false)
	// 		return null;

	// 	if (dropTableData.Rewards.Count <= 0)
	// 		return null;

	// 	int sum = 0;
	// 	int randValue = UnityEngine.Random.Range(0, 100);

	// 	foreach (RewardData item in dropTableData.Rewards)
	// 	{
	// 		sum += item.Probability;

	// 		if (randValue <= sum)
	// 			return item;
	// 	}

	// 	//return dropTableData.Rewards.RandomElementByWeight(e => e.Probability);
	// 	return null;
	// }

    }
}