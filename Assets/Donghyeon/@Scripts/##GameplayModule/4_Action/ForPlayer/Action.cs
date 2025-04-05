// using System;
// using System.Collections.Generic;
// using Unity.Assets.Scripts.Gameplay.GameplayObjects;
// using Unity.Assets.Scripts.Gameplay.GameplayObjects.Character;
// // using Unity.BossRoom.VisualEffects;
// using Unity.Netcode;
// using UnityEngine;
// // using BlockingMode = Unity.Assets.Scripts.Gameplay.Actions.BlockingModeType;

// namespace Unity.Assets.Scripts.Gameplay.Actions
// {
//     /// <summary>
//     /// 모든 액션의 추상 부모 클래스입니다.
//     /// </summary>
//     /// <remarks>
//     /// 액션 시스템은 캐릭터가 네트워크 상에서 "행동"을 수행하기 위한 일반화된 메커니즘입니다.
//     /// 액션은 기본 공격부터 궁수의 연사와 같은 특수 스킬, 레버 당기기와 같은 일반적인 행동까지 모두 포함합니다.
//     /// 각 ActionLogic enum에 대해 이 클래스의 특수화된 버전이 하나씩 존재합니다.
//     /// 
//     /// 한 캐릭터에는 한 번에 하나의 활성 액션(blocking action)만 존재할 수 있지만,
//     /// 여러 액션이 동시에 존재할 수 있으며, 후속 액션들은 현재 활성 액션 뒤에서 대기하고
//     /// "non-blocking" 액션들은 백그라운드에서 실행될 수 있습니다.
//     ///
//     /// 액션의 실행 흐름:
//     /// 초기: Start()
//     /// 매 프레임: ShouldBecomeNonBlocking() (액션이 blocking인 경우만), 그 다음 Update()
//     /// 종료 시: End() 또는 Cancel()
//     /// 종료 후: ChainIntoNewAction() (액션이 blocking이었고 End()로 종료된 경우만)
//     /// </remarks>
//     public abstract class Action : ScriptableObject
//     {
//         /// <summary>
//         /// GameDataSource 배열의 액션 프로토타입 인덱스입니다. 
//         /// GameDataSource 클래스에 의해 런타임에 설정됩니다.
//         /// 액션이 프로토타입이 아닌 경우 프로토타입 참조의 액션 ID를 포함합니다.
//         /// 이 필드는 네트워크를 통해 전송될 수 있는 방식으로 액션을 식별하는 데 사용됩니다.
//         /// </summary>
//         [NonSerialized]
//         public ActionID ActionID;

//         /// <summary>
//         /// 기본 피격 반응 애니메이션입니다. 여러 ActionFX에서 사용됩니다.
//         /// </summary>
//         public const string k_DefaultHitReact = "HitReact1";

//         protected ActionRequestData m_Data;

//         /// <summary>
//         /// 이 액션이 시작된 시간(Time.time 기준)입니다. ActionPlayer나 ActionVisualization에 의해 설정됩니다.
//         /// </summary>
//         public float TimeStarted { get; set; }

//         /// <summary>
//         /// 액션이 실행된 시간(Start가 호출된 이후)입니다. Time.time을 통해 초 단위로 측정됩니다.
//         /// </summary>
//         public float TimeRunning { get { return (Time.time - TimeStarted); } }

//         /// <summary>
//         /// 인스턴스화될 때 사용된 요청 데이터입니다. 값은 읽기 전용으로 취급되어야 합니다.
//         /// </summary>
//         public ref ActionRequestData Data => ref m_Data;

//         /// <summary>
//         /// 이 액션에 대한 데이터 설명입니다.
//         /// </summary>
//         public ActionConfig Config;

//         public bool IsChaseAction => ActionID == GameDataSource.Instance.GeneralChaseActionPrototype.ActionID;
//         public bool IsStunAction => ActionID == GameDataSource.Instance.StunnedActionPrototype.ActionID;
//         public bool IsGeneralTargetAction => ActionID == GameDataSource.Instance.GeneralTargetActionPrototype.ActionID;

//         /// <summary>
//         /// 액션 요청 데이터로 초기화합니다.
//         /// "data" 매개변수는 이 메서드로 전달된 후에는 보관되지 않아야 합니다.
//         /// ActionFactory에 의해 호출되어야 합니다.
//         /// </summary>
//         public void Initialize(ref ActionRequestData data)
//         {
//             m_Data = data;
//             ActionID = data.ActionID;
//         }

//         /// <summary>
//         /// 액션을 풀로 반환하기 전에 초기화하는 함수입니다.
//         /// </summary>
//         public virtual void Reset()
//         {
//             m_Data = default;
//             ActionID = default;
//             TimeStarted = 0;
//         }

//         /// <summary>
//         /// 액션이 실제로 실행되기 시작할 때 호출됩니다 (큐잉으로 인해 생성 후 지연될 수 있음).
//         /// </summary>
//         /// <returns>액션이 실행을 원하지 않으면 false, 그렇지 않으면 true를 반환합니다.</returns>
//         public abstract bool OnStart(ServerCharacter serverCharacter);

//         /// <summary>
//         /// 액션이 실행 중일 때 매 프레임마다 호출됩니다.
//         /// </summary>
//         /// <returns>계속 실행하려면 true, 중지하려면 false를 반환합니다.</returns>
//         public abstract bool OnUpdate(ServerCharacter clientCharacter);

//         /// <summary>
//         /// 활성("blocking") 액션에 대해 매 프레임마다 호출되어(OnUpdate() 이전에) 
//         /// 백그라운드 액션이 되어야 하는지 확인합니다.
//         /// </summary>
//         /// <returns>백그라운드 액션이 되려면 true, blocking 액션으로 남으려면 false를 반환합니다.</returns>
//         public virtual bool ShouldBecomeNonBlocking()
//         {
//             // return Config.BlockingMode == BlockingModeType.OnlyDuringExecTime ? TimeRunning >= Config.ExecTimeSeconds : false;
//             return false;
//         }

//         /// <summary>
//         /// 액션이 자연스럽게 종료될 때 호출됩니다. 기본적으로 Cancel()을 호출합니다.
//         /// </summary>
//         public virtual void End(ServerCharacter serverCharacter)
//         {
//             Cancel(serverCharacter);
//         }

//         /// <summary>
//         /// 액션이 취소될 때 호출됩니다. 액션은 이 시점에서 모든 진행 중인 효과를 정리해야 합니다.
//         /// (예: 이동을 포함하는 액션은 현재 진행 중인 이동을 취소해야 합니다)
//         /// </summary>
//         public virtual void Cancel(ServerCharacter serverCharacter) { }

//         /// <summary>
//         /// End() 이후에 호출됩니다. 이 시점에서 액션은 종료되었으며, Update() 등의 함수는 더 이상 호출되지 않습니다.
//         /// 액션이 즉시 다른 액션으로 전환하려는 경우 여기서 수행할 수 있습니다.
//         /// 새로운 액션은 다음 Update()에서 적용됩니다.
//         /// 
//         /// 참고: 이 함수는 조기에 취소된 액션에서는 호출되지 않으며, End()가 호출된 액션에서만 호출됩니다.
//         /// </summary>
//         /// <param name="newAction">즉시 전환할 새로운 액션</param>
//         /// <returns>새로운 액션이 있으면 true, 없으면 false</returns>
//         public virtual bool ChainIntoNewAction(ref ActionRequestData newAction) { return false; }

//         /// <summary>
//         /// 이 캐릭터가 다른 객체와 충돌할 때 활성("blocking") 액션에서 호출됩니다.
//         /// </summary>
//         public virtual void CollisionEntered(ServerCharacter serverCharacter, Collision collision) { }

//         public enum BuffableValue
//         {
//             PercentHealingReceived,  // 기본값은 1.0. 0으로 감소하면 "치유 없음". 2는 "2배 치유"
//             PercentDamageReceived,   // 기본값은 1.0. 0으로 감소하면 "데미지 없음". 2는 "2배 데미지"
//             ChanceToStunTramplers,   // 기본값은 0. 0보다 크면 이 캐릭터를 밟은 대상이 기절할 확률
//         }

//         /// <summary>
//         /// 게임플레이 계산 결과를 수정할 기회를 모든 활성 액션에 제공합니다.
//         /// 이는 "버프"(긍정적 효과)와 "디버프"(부정적 효과) 모두에 사용됩니다.
//         /// </summary>
//         public virtual void BuffValue(BuffableValue buffType, ref float buffedValue) { }

//         /// <summary>
//         /// BuffableValue의 기본값("버프되지 않은")을 반환하는 정적 유틸리티 함수입니다.
//         /// </summary>
//         public static float GetUnbuffedValue(Action.BuffableValue buffType)
//         {
//             switch (buffType)
//             {
//                 case BuffableValue.PercentDamageReceived: return 1;
//                 case BuffableValue.PercentHealingReceived: return 1;
//                 case BuffableValue.ChanceToStunTramplers: return 0;
//                 default: throw new System.Exception($"Unknown buff type {buffType}");
//             }
//         }

//         public enum GameplayActivity
//         {
//             AttackedByEnemy,     // 적에게 공격받음
//             Healed,              // 치유됨
//             StoppedChargingUp,   // 차지 중단됨
//             UsingAttackAction,   // 공격 액션을 사용하기 직전에 호출됨
//         }

//         /// <summary>
//         /// 주목할 만한 게임플레이 이벤트가 발생했을 때 활성 액션에 알리기 위해 호출됩니다.
//         /// AttackedByEnemy나 Healed 같은 GameplayActivity가 발생하면,
//         /// OnGameplayAction()이 BuffValue() 호출 전에 호출됩니다.
//         /// </summary>
//         public virtual void OnGameplayActivity(ServerCharacter serverCharacter, GameplayActivity activityType) { }

//         /// <summary>
//         /// 이 액션 효과가 서버의 확인을 받기 전에 즉시 실행되기 시작했는지 여부입니다.
//         /// </summary>
//         public bool AnticipatedClient { get; protected set; }

//         /// <summary>
//         /// 클라이언트 측에서 액션 효과를 시작합니다.
//         /// 파생 클래스는 즉시 종료하고 싶은 경우 false를 반환할 수 있습니다.
//         /// </summary>
//         public virtual bool OnStartClient(ClientCharacter clientCharacter)
//         {
//             AnticipatedClient = false; //once you start for real you are no longer an anticipated action.
//             TimeStarted = UnityEngine.Time.time;
//             return true;
//         }

//         /// <summary>
//         /// 클라이언트 측에서 액션을 업데이트합니다.
//         /// </summary>
//         public virtual bool OnUpdateClient(ClientCharacter clientCharacter)
//         {
//             // return ActionConclusion.Continue;
//             return false;
//         }
//         /// <summary>
//         /// 클라이언트 측에서 액션 효과가 종료될 때 호출됩니다.
//         /// 파생 클래스가 정리 로직을 넣기에 좋은 위치입니다.
//         /// </summary>
//         public virtual void EndClient(ClientCharacter clientCharacter)
//         {
//             CancelClient(clientCharacter);
//         }

//         /// <summary>
//         /// 클라이언트 측에서 액션 효과가 중단될 때 호출됩니다.
//         /// End와 논리적으로 구분되어 있어 액션이 중단될 때 다른 효과를 재생할 수 있습니다.
//         /// </summary>
//         public virtual void CancelClient(ClientCharacter clientCharacter) { }

//         /// <summary>
//         /// 이 액션 효과가 클라이언트에서 미리 생성되어야 하는지 확인합니다.
//         /// </summary>
//         /// <param name="clientCharacter">이 ActionFX를 실행할 ActionVisualization입니다.</param>
//         /// <param name="data">서버로 전송될 요청입니다.</param>
//         /// <returns>true인 경우 ActionVisualization은 서버의 응답을 기다리지 않고 
//         /// 소유 클라이언트에서 미리 ActionFX를 생성해야 합니다.</returns>
//         public static bool ShouldClientAnticipate(ClientCharacter clientCharacter, ref ActionRequestData data)
//         {
//             if (!clientCharacter.CanPerformActions) { return false; }

//             var actionDescription = GameDataSource.Instance.GetActionPrototypeByID(data.ActionID).Config;

//             //for actions with ShouldClose set, we check our range locally. If we are out of range, we shouldn't anticipate, as we will
//             //need to execute a ChaseAction (synthesized on the server) prior to actually playing the skill.
//             bool isTargetEligible = true;
//             if (data.ShouldClose == true)
//             {
//                 ulong targetId = (data.TargetIds != null && data.TargetIds.Length > 0) ? data.TargetIds[0] : 0;
//                 if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject networkObject))
//                 {
//                     float rangeSquared = actionDescription.Range * actionDescription.Range;
//                     isTargetEligible = (networkObject.transform.position - clientCharacter.transform.position).sqrMagnitude < rangeSquared;
//                 }
//             }

//             //at present all Actionts anticipate except for the Target action, which runs a single instance on the client and is
//             //responsible for action anticipation on its own.
//             // return isTargetEligible && actionDescription.Logic != ActionLogic.Target;
//             return false;
//         }

//         /// <summary>
//         /// 시각화가 애니메이션 이벤트를 수신할 때 호출됩니다.
//         /// </summary>
//         public virtual void OnAnimEventClient(ClientCharacter clientCharacter, string id) { }

//         /// <summary>
//         /// 이 액션이 "차지 업"을 완료했을 때 호출됩니다.
//         /// (일부 액션 타입에서만 의미가 있으며, 다른 액션에서는 호출되지 않습니다.)
//         /// </summary>
//         public virtual void OnStoppedChargingUpClient(ClientCharacter clientCharacter, float finalChargeUpPercentage) { }

//         /// <summary>
//         /// Utility function that instantiates all the graphics in the Spawns list.
//         /// If parentToOrigin is true, the new graphics are parented to the origin Transform.
//         /// If false, they are positioned/oriented the same way but are not parented.
//         /// </summary>
//         // protected List<SpecialFXGraphic> InstantiateSpecialFXGraphics(Transform origin, bool parentToOrigin)
//         // {
//         //     var returnList = new List<SpecialFXGraphic>();
//         //     foreach (var prefab in Config.Spawns)
//         //     {
//         //         if (!prefab) { continue; } // skip blank entries in our prefab list
//         //         returnList.Add(InstantiateSpecialFXGraphic(prefab, origin, parentToOrigin));
//         //     }
//         //     return returnList;
//         // }

//         /// <summary>
//         /// Utility function that instantiates one of the graphics from the Spawns list.
//         /// If parentToOrigin is true, the new graphics are parented to the origin Transform.
//         /// If false, they are positioned/oriented the same way but are not parented.
//         /// </summary>
//         // protected SpecialFXGraphic InstantiateSpecialFXGraphic(GameObject prefab, Transform origin, bool parentToOrigin)
//         // {
//         //     if (prefab.GetComponent<SpecialFXGraphic>() == null)
//         //     {
//         //         throw new System.Exception($"One of the Spawns on action {this.name} does not have a SpecialFXGraphic component and can't be instantiated!");
//         //     }
//         //     var graphicsGO = GameObject.Instantiate(prefab, origin.transform.position, origin.transform.rotation, (parentToOrigin ? origin.transform : null));
//         //     return graphicsGO.GetComponent<SpecialFXGraphic>();
//         // }

//         /// <summary>
//         /// 액션이 클라이언트에서 "예상"될 때 호출됩니다. 
//         /// 예를 들어, 탱크의 소유자이고 망치를 휘두르면 서버 왕복 전에 
//         /// 클라이언트에서 즉시 이 호출이 발생합니다.
//         /// 오버라이드하는 클래스는 항상 구현에서 기본 클래스를 호출해야 합니다!
//         /// </summary>
//         public virtual void AnticipateActionClient(ClientCharacter clientCharacter)
//         {
//             AnticipatedClient = true;
//             TimeStarted = UnityEngine.Time.time;

//             if (!string.IsNullOrEmpty(Config.AnimAnticipation))
//             {
//                 clientCharacter.OurAnimator.SetTrigger(Config.AnimAnticipation);
//             }
//         }

//     }
// }
