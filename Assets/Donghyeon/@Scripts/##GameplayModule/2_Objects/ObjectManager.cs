using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;
using System;
using Unity.Assets.Scripts.Data;


public class ObjectManager
{
    [Inject] private ResourceManager _resourceManager;
	public CreatureData CreatureData { get; private set; }
    [Inject] private DebugClassFacade _debugClassFacade;

    private static ObjectManager s_instance;

    public static ObjectManager Instance
    {
        get
        {
            if (s_instance == null)
            {

            }
            return s_instance;
        }
    }


    // public HashSet<ServerMonster> Monsters { get; } = new HashSet<ServerMonster>();
	// public HashSet<ServerHero> Heroes { get; } = new HashSet<ServerHero>();
	// public HashSet<Projectile> Projectiles { get; } = new HashSet<Projectile>();
	// public HashSet<Env> Envs { get; } = new HashSet<Env>();
	// public HashSet<EffectBase> Effects { get; } = new HashSet<EffectBase>();
	// public HeroCamp Camp { get; private set; }



	#region Roots
	public Transform GetRootTransform(string name)
	{
		GameObject root = GameObject.Find(name);
		if (root == null)
			root = new GameObject { name = name };

		return root.transform;
	}

	public Transform HeroRoot { get { return GetRootTransform("@Heroes"); } }
	public Transform MonsterRoot { get { return GetRootTransform("@Monsters"); } }

	public Transform ProjectileRoot { get { return GetRootTransform("@Projectiles"); } }
	public Transform EnvRoot { get { return GetRootTransform("@Envs"); } }
	public Transform EffectRoot { get { return GetRootTransform("@Effects"); } }
	#endregion


	public void ShowDamageFont(Vector2 position, float damage, Transform parent, bool isCritical = false)
	{
		GameObject go = _resourceManager.Instantiate("DamageFont", pooling: true);
		DamageFont damageText = go.GetComponent<DamageFont>();
		damageText.SetInfo(position, damage, parent, isCritical, _resourceManager);

	}

	public GameObject SpawnGameObject(Vector3 position, string prefabName)
	{
        GameObject go = _resourceManager.Instantiate(prefabName, pooling: true, position: position);
		go.transform.position = position;
		return go;
	}


    public T Spawn<T>(
        Vector3 position,
        ulong clientID = 0,
        int templateID = 0,
        string prefabName = "") where T : BaseObject
    {
        _debugClassFacade?.LogInfo(GetType().Name, $"[ObjectManager] Spawn<T>(position) 호출: {prefabName} at {position}");
        return Spawn<T>(position, Quaternion.identity, clientID, templateID, prefabName);
    }


	public T Spawn<T>(Vector3Int cellPos, ulong clientId = 0, int templateID = 0, string prefabName = "", GameObject parent= null) where T : BaseObject
	{
		Vector3 spawnPos = new Vector3(cellPos.x, cellPos.y, 0);
		return Spawn<T>(spawnPos, clientId, templateID, prefabName);
	}


	public event Action<ulong, ulong> OnMonsterSpawned; // (networkObjectId, clientId)

    public T Spawn<T>(
        Vector3 position,
        Quaternion rotation, // Rotation 파라미터 추가
        ulong clientID = 0,
        int templateID = 0,
        string prefabName = "") where T : BaseObject
    {
		_debugClassFacade?.LogInfo(GetType().Name, $"[ObjectManager] Spawn<T> 호출: {prefabName}");		
		_debugClassFacade?.LogInfo(GetType().Name, $"[ObjectManager] Spawn<T> 호출: {position}");

		if (string.IsNullOrEmpty(prefabName))
		{
			prefabName = typeof(T).Name;
			_debugClassFacade?.LogInfo(GetType().Name, $"[ObjectManager] 프리팹 이름 자동 설정: {prefabName}");
		}

        GameObject go = _resourceManager.Instantiate(prefabName, pooling: false, position: position, rotation:  rotation);
	    if (go == null)
        {
             _debugClassFacade?.LogError(GetType().Name, $"[ObjectManager] ResourceManager failed to instantiate '{prefabName}'.");
             return null; // 실패 시 null 반환
        }

		go.name = prefabName;

		BaseObject obj = go.GetComponent<BaseObject>();
		if (obj == null)
		{
			_debugClassFacade?.LogError(GetType().Name, $"[ObjectManager] '{prefabName}' 오브젝트에 BaseObject 컴포넌트가 없습니다.");
			_resourceManager.Destroy(go);
			return null;
		}


		// if (obj.ObjectType == EObjectType.Creature)
		// {
		// 	Creature creature = obj.GetComponent<Creature>();

		// 	_debugClassFacade?.LogInfo(GetType().Name, $"[ObjectManager] 생성된 Creature의 CreatureType: {creature.CreatureType}");

		// 	switch (creature.CreatureType)
		// 	{
		// 	// case CharacterTypeEnum.Monster:
		// 	// 	CreatureData = DataLoader.instance.MonsterDic[templateID];				
		// 	// 	ClientMonster clientMonster = go.GetComponent<ClientMonster>();
		// 	// 	MonsterAvatarSO clientMonsterAvatar = _resourceManager.Load<MonsterAvatarSO>(CreatureData.ClientAvatar);
		// 	// 	clientMonster.SetAvatar(clientMonsterAvatar);

		// 	// 	ServerMonster monster = go.GetComponent<ServerMonster>();
		// 	// 	monster.SetInfo(templateID, CreatureData, clientMonster);
		// 	// 	Monsters.Add(monster);
		// 	// 	break;
			
		// 	case CharacterTypeEnum.Hero:
		// 		CreatureData = DataLoader.instance.HeroDic[templateID];
		// 		// ClientHero clientHero = go.GetComponent<ClientHero>();
		// 		// HeroAvatarSO clientHeroAvatar = _resourceManager.Load<HeroAvatarSO>(CreatureData.ClientAvatar);
		// 		// clientHero.SetAvatar(clientHeroAvatar,  CreatureData.SkeletonDataID, _resourceManager);

		// 		// ServerHero serverHero = go.GetComponent<ServerHero>();				
		// 		// serverHero.SetInfo(templateID, CreatureData, clientHero);
		// 		// Heroes.Add(serverHero);
		// 		break;
		// 	default:
		// 		_debugClassFacade?.LogWarning(GetType().Name, $"[ObjectManager] 알 수 없는 CreatureType: {creature.CreatureType}");
		// 		break;
		// 	}
		// }



		NetworkObject networkObject = go.GetComponent<NetworkObject>();

		try 
		{
			if (!networkObject.IsSpawned && NetworkManager.Singleton.IsServer)
			{
				// Spawn 호출 직전의 Transform 상태 로깅
				Debug.Log($"[ObjectManager PRE-SPAWN] GO: {go.name}, Pos: {go.transform.position}, Rot: {go.transform.rotation.eulerAngles}");
				
				networkObject.Spawn();
				Debug.Log($"[ObjectManager] NetworkObject 스폰 완료: {networkObject.NetworkObjectId}");
				
				// --- 추가: 스폰 직후 NetworkTransform 상태 강제 업데이트 시도 ---
				if (networkObject.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var netTransform))
				{
					// 현재 transform 값을 NetworkTransform에 강제로 반영하고 전송 시도
					// (Netcode 버전에 따라 이런 명시적 함수가 없을 수 있음 - 주석 처리된 것은 예시)
					// netTransform.SetState(go.transform.position, go.transform.rotation, go.transform.localScale);
					// netTransform.TryCommitNetworkState(); // 상태 변경 즉시 전송 시도
					Debug.Log($"[ObjectManager POST-SPAWN] Forcing NetworkTransform update for {networkObject.NetworkObjectId}");
					// 참고: 많은 경우, 다음 FixedUpdate까지 기다리지 않고 상태를 보내는 명시적 방법은 제한적임.
					//      일단은 NetworkTransform이 변경을 감지하기를 기대.
				}
				// --- 추가 끝 ---
				
				// 클라이언트 ID가 0이 아닌 경우, 소유권을 해당 클라이언트로 변경
				if (clientID != 0)
				{
					networkObject.ChangeOwnership(clientID);
					_debugClassFacade?.LogInfo(GetType().Name, $"[ObjectManager] NetworkObject 소유권 변경됨: {networkObject.NetworkObjectId} -> 클라이언트 ID: {clientID}");
				}
			}
		}

		catch (Exception e)
		{
			_debugClassFacade?.LogError(GetType().Name, $"[ObjectManager] NetworkObject 스폰 중 오류 발생: {e.Message}");
			_resourceManager.Destroy(go);
			return null;
		}


		return obj as T;
	}




	public void Despawn<T>(T obj) where T : BaseObject
	{
		// NetworkObject 처리
		NetworkObject networkObj = obj.GetComponent<NetworkObject>();

		if (networkObj != null && networkObj.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
		{
			// 서버에서만 NetworkObject를 디스폰합니다.
			if (NetworkManager.Singleton.IsServer)
			{
				Debug.Log($"[ObjectManager] NetworkObject Despawn 호출: {obj.name} (NetworkObjectId: {networkObj.NetworkObjectId})");
				networkObj.Despawn();
			}
			else
			{
				// 클라이언트에서 호출된 경우 경고 메시지만 표시하고 디스폰을 수행하지 않습니다.
				Debug.LogWarning($"[ObjectManager] 클라이언트에서 NetworkObject Despawn 시도: {obj.name}. 서버가 아닌 클라이언트에서는 NetworkObject를 디스폰할 수 없습니다.");
				// 클라이언트에서는 디스폰을 수행하지 않음 (네트워크 에러 방지)
				return;
			}
		}

		_resourceManager.Destroy(obj.gameObject);
	}

  

    #region Skill 판정
    // public List<Creature> FindConeRangeTargets(Creature owner, Vector3 dir, float range, int angleRange, bool isAllies = false)
    // {
    // 	HashSet<Creature> targets = new HashSet<Creature>();
    // 	HashSet<Creature> ret = new HashSet<Creature>();

    // 	ECreatureType targetType = Util.DetermineTargetType(owner.CreatureType, isAllies);

    // 	if (targetType == ECreatureType.Monster)
    // 	{
    // 		var objs = Managers.Map.GatherObjects<Monster>(owner.transform.position, range, range);
    // 		targets.AddRange(objs);
    // 	}
    // 	else if (targetType == ECreatureType.Hero)
    // 	{
    // 		var objs = Managers.Map.GatherObjects<Hero>(owner.transform.position, range, range);
    // 		targets.AddRange(objs);
    // 	}

    // 	foreach (var target in targets)
    // 	{
    // 		// 1. 거리안에 있는지 확인
    // 		var targetPos = target.transform.position;
    // 		float distance = Vector3.Distance(targetPos, owner.transform.position);

    // 		if (distance > range)
    // 			continue;

    // 		// 2. 각도 확인
    // 		if (angleRange != 360)
    // 		{
    // 			BaseObject ownerTarget = (owner as Creature).Target;

    // 			// 2. 부채꼴 모양 각도 계산
    // 			float dot = Vector3.Dot((targetPos - owner.transform.position).normalized, dir.normalized);
    // 			float degree = Mathf.Rad2Deg * Mathf.Acos(dot);

    // 			if (degree > angleRange / 2f)
    // 				continue;
    // 		}

    // 		ret.Add(target);
    // 	}

    // 	return ret.ToList();
    // }

    // public List<Creature> FindCircleRangeTargets(Creature owner, Vector3 startPos, float range, bool isAllies = false)
    // {
    // 	HashSet<Creature> targets = new HashSet<Creature>();
    // 	HashSet<Creature> ret = new HashSet<Creature>();

    // 	ECreatureType targetType = Util.DetermineTargetType(owner.CreatureType, isAllies);

    // 	if (targetType == ECreatureType.Monster)
    // 	{
    // 		var objs = Managers.Map.GatherObjects<Monster>(owner.transform.position, range, range);
    // 		targets.AddRange(objs);
    // 	}
    // 	else if (targetType == ECreatureType.Hero)
    // 	{
    // 		var objs = Managers.Map.GatherObjects<Hero>(owner.transform.position, range, range);
    // 		targets.AddRange(objs);
    // 	}

    // 	foreach (var target in targets)
    // 	{
    // 		// 1. 거리안에 있는지 확인
    // 		var targetPos = target.transform.position;
    // 		float distSqr = (targetPos - startPos).sqrMagnitude;

    // 		if (distSqr < range * range)
    // 			ret.Add(target);
    // 	}

    // 	return ret.ToList();
    // }
    #endregion
}