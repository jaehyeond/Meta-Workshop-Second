using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers;
using static Define;


namespace Unity.Assets.Scripts.Data
{

    #region 기본 데이터 구조
    [Serializable]
    public class GameObjectData
    {		
		public string Name;
        public int DataId;
        public string PrefabLabel;
        public string Tag;
        public string Layer;
		public string DescriptionText;

    }
    #endregion

    #region 물리 오브젝트 기본 데이터
    [Serializable]
    public class PhysicsObjectData : GameObjectData
    {
        public float Mass;
        public float Drag;
        public float AngularDrag;
        public bool UseGravity;
        public bool IsKinematic;
        public string PhysicsMaterial;
    }
    #endregion



    #region 공 데이터
    [Serializable]
    public class BallData : PhysicsObjectData
    {
        // 기본 속성
        public string BallType;
        public float BaseRadius;
        public float BaseSpeed;
        public float BaseDamage;

        // 스킬 및 능력 ID 참조
        public int DefaultSkillId;  // 기본 스킬
        public int EnvSkillId;      // 환경 스킬
        public int VisualId;
		public int PhysicsId;
		public int MinigameId;
		public int StatsId;
        public string IconImage;
		public string ClientPrefab; //trail , skill , env sound, else prefab
        // 레벨별 속성 및 스킬 구성
		// ID 리스트로 변경
		public List<int> LevelConfigsId = new List<int>();
		
		[NonSerialized]
		private List<BallLevelConfig> _levelConfigs;
		
		// 현재 레벨에 맞는 설정 가져오기
		public BallLevelConfig GetLevelConfig(int level)
		{
			// 아직 설정이 로드되지 않았다면 로드
			if (_levelConfigs == null)
			{
				LoadLevelConfigs();
			}
			
			foreach (var config in _levelConfigs)
			{
				if (config.Level == level)
					return config;
			}
			
			// 레벨에 맞는 설정이 없으면 가장 낮은 레벨 반환
			if (_levelConfigs.Count > 0)
				return _levelConfigs[0];
					
			return null;
		}
		
		// 레벨 설정을 로드하는 메서드
		private void LoadLevelConfigs()
		{
			_levelConfigs = new List<BallLevelConfig>();
			
			// 여기서는 예시로 BallLevelConfigManager에서 설정을 로드합니다
			// 실제 구현에서는 귀하의 데이터 관리 시스템에 맞게 수정하세요
			foreach (int configId in LevelConfigsId)
			{
				// 옵션 1: 이미 로드된 데이터 매니저에서 가져오기
				// BallLevelConfig config = BallLevelConfigManager.Instance.GetConfig(configId);
				
				// 옵션 2: 간단히 새 객체를 생성하고 ID 할당 (임시 방법)
				BallLevelConfig config = new BallLevelConfig();
				config.Level = configId;
				
				if (config != null)
					_levelConfigs.Add(config);
			}
		}
    }

			
	[Serializable]
	public class BallDataLoader : ILoader<int, BallData>
	{
		public List<BallData> balls = new List<BallData>();
		public Dictionary<int, BallData> MakeDict()
		{
				Dictionary<int, BallData> dict = new Dictionary<int, BallData>();
				foreach (BallData ball in balls)
					dict.Add(ball.DataId, ball);
				return dict;
			}
	}


    #region PaddleData
    [Serializable]
    public class PaddleData : PhysicsObjectData
    {
		public float BaseMoveSpeed;
		public Vector3 BaseSize;
		public float BaseBallLaunchForce;
		public bool DefaultCanHoldBall;
		public float MinSize;
		public float MaxSize;
		public bool DefaultBallAngleControl;

		public int DefaultSkillId;  // 기본 스킬
		public int SpecialSkillId;  // 특수 스킬
		public int VisualId;        // 비주얼 설정
		public int PhysicsId;       // 물리 속성 설정


		    // 레벨별 패들 구성
		// public List<PaddleLevelConfig> LevelConfigs = new List<PaddleLevelConfig>();
		
		// // 현재 레벨에 맞는 설정 가져오기
		// public PaddleLevelConfig GetLevelConfig(int level)
		// {
		// 	foreach (var config in LevelConfigs)
		// 	{
		// 		if (config.Level == level)
		// 			return config;
		// 	}
			
		// 	// 레벨에 맞는 설정이 없으면 가장 낮은 레벨 반환
		// 	if (LevelConfigs.Count > 0)
		// 		return LevelConfigs[0];
				
		// 	return null;
		// }


    }

    [Serializable]
    public class PaddleDataLoader : ILoader<int, PaddleData>
    {
        public List<PaddleData> paddles = new List<PaddleData>();
        public Dictionary<int, PaddleData> MakeDict()
        {
            Dictionary<int, PaddleData> dict = new Dictionary<int, PaddleData>();
            foreach (PaddleData paddle in paddles)
                dict.Add(paddle.DataId, paddle);
            return dict;
        }
    }
    #endregion

#region BrickData
[Serializable]

public class BrickData : PhysicsObjectData
{
    // 기본 속성
    public int BaseHealth;
    public int BaseScoreValue;
    public string BrickType;
    public Vector3 BaseSize;
    public bool DefaultShowScoreNumber;
    public int BaseFragmentCount;
    public float BaseDropProbability;
    
    // 스킬 및 효과 ID 참조
    public int DestroyEffectId;
    public int HitEffectId;
    public int VisualId;
    public int PhysicsId;
    
    // 레벨별 설정
    // public List<BrickLevelConfig> LevelConfigs = new List<BrickLevelConfig>();
    
    // // 현재 레벨에 맞는 설정 가져오기
    // public BrickLevelConfig GetLevelConfig(int level)
    // {
    //     foreach (var config in LevelConfigs)
    //     {
    //         if (config.Level == level)
    //             return config;
    //     }
        
    //     // 레벨에 맞는 설정이 없으면 가장 낮은 레벨 반환
    //     if (LevelConfigs.Count > 0)
    //         return LevelConfigs[0];
            
    //     return null;
    // }
    
    // // 가능한 드롭 아이템 목록
    // public List<string> PossibleDrops = new List<string>();
}


    [Serializable]
    public class BrickDataLoader : ILoader<int, BrickData>
    {
        public List<BrickData> bricks = new List<BrickData>();
        public Dictionary<int, BrickData> MakeDict()
        {
            Dictionary<int, BrickData> dict = new Dictionary<int, BrickData>();
            foreach (BrickData brick in bricks)
                dict.Add(brick.DataId, brick);
            return dict;
        }
    }
    #endregion








	[Serializable]
	public class BallLevelConfig
	{
	public int Level;
	public string ClientPrefab; //trail , skill , env sound, else prefab

	// 레벨에 따른 참조 ID들
	public int DefaultSkillId;    // 기본 스킬
	public int EnvSkillId;        // 환경 스킬
	public int SkillAId;          // 특수 스킬 A
	public int SkillBId;          // 특수 스킬 B
	public int VisualId;          // 비주얼 설정
	public int PhysicsId;         // 물리 속성 설정
	public int StatsId;           // 스탯 설정
	
	// 레벨별 스탯 보정치 (기본값 = 변경 없음)
	public float RadiusMultiplier = 1.0f;
	public float SpeedMultiplier = 1.0f;
	public float DamageMultiplier = 1.0f;
	public int UpgradeCoins;
	public int UpgradeGems;
	public int RequiredPlayerLevel;
	// 레벨별 시각 효과 (직접 설정하거나 VisualId로 참조)
	// public string MaterialId;
	// public bool HasTrail;
	// public string TrailEffectId;
	// public string ImpactEffectId;
	
	// 레벨별 물리 효과 (직접 설정하거나 PhysicsId로 참조)
	// public float BounceFactor = 1.0f;
	// public float Friction = 0.2f;
	
	// 업그레이드 요구사항

	}


	[Serializable]
	public class VisualConfig
	{
		public int Id;
		public string renderer_material;
		public string renderer_sortingLayer;
		public int renderer_sortingOrder;
		public string renderer_sprite;
		public string renderer_mesh;
		public string renderer_trail;

		//prefab
		
	}


	[Serializable]
	public class PhysicsConfig
	{
		public int Id;
		
		// Rigidbody 속성
		public float rigidbody_mass;
		public float rigidbody_drag;
		public float rigidbody_angularDrag;
		public bool rigidbody_useGravity;
		public bool rigidbody_isKinematic;
		public string rigidbody_collisionDetectionMode; // "Discrete", "Continuous", "ContinuousDynamic"
		public string rigidbody_constraints; // "None", "FreezePositionX", "FreezePositionY" 등 조합
		public float rigidbody_maxAngularVelocity;
		public float rigidbody_maxDepenetrationVelocity;
		
		// Physics Material 속성
		public float physicsMaterial_bounciness;
		public float physicsMaterial_dynamicFriction;
		public float physicsMaterial_staticFriction;
		public string physicsMaterial_frictionCombine; // "Average", "Minimum", "Maximum", "Multiply"
		public string physicsMaterial_bounceCombine; // "Average", "Minimum", "Maximum", "Multiply"
		
		// 초기 힘과 속도 설정
		public float force_initialVelocityX;
		public float force_initialVelocityY;
		public float force_initialVelocityZ;
		public float force_initialAngularVelocityX;
		public float force_initialAngularVelocityY;
		public float force_initialAngularVelocityZ;
		public float force_torqueMultiplier;
		public float force_forceMultiplier;
		public float force_gravityScale;
		public float force_angleRange;
		
		// 콜라이더 속성
		public string collider_type; // "Sphere", "Box", "Capsule" 등
		public bool collider_isTrigger;
		public float collider_offsetX;
		public float collider_offsetY;
		public float collider_offsetZ;
		public float collider_radius; // Sphere, Capsule에 사용
		
		// Box Collider에만 사용되는 속성들 (필요한 경우)
		public float collider_sizeX;
		public float collider_sizeY;
		public float collider_sizeZ;
		
		// Capsule Collider에만 사용되는 속성들 (필요한 경우)
		public float collider_height;
		public int collider_direction; // 0 = X, 1 = Y, 2 = Z
	}

	[Serializable]
	public class PhysicsConfigLoader : ILoader<int, PhysicsConfig>
	{
		public List<PhysicsConfig> physicsConfigs = new List<PhysicsConfig>();
		public Dictionary<int, PhysicsConfig> MakeDict()
		{
			Dictionary<int, PhysicsConfig> dict = new Dictionary<int, PhysicsConfig>();
			foreach (PhysicsConfig config in physicsConfigs)
				dict.Add(config.Id, config);
			return dict;
		}
	}





	#region CreatureData
	[Serializable]
	public class CreatureData
	{
		public int DataId;
		public string DescriptionTextID;
		public string PrefabLabel;
		public float ColliderOffsetX;
		public float ColliderOffsetY;
		public float ColliderRadius;
		public float MaxHp;
		public float UpMaxHpBonus;
		public float Atk;
		public float AtkRange;
		public float AtkBonus;
		public float MoveSpeed;
		public float CriRate;
		public float CriDamage;
		public string IconImage;
		public string SkeletonDataID;
		public int DefaultSkillId;
		public int EnvSkillId;
		public int SkillAId;
		public int SkillBId;
		public string CharacterType;
		public bool IsValidTarget;
		public bool IsNpc;
		public string ClientAvatar;
	}
	#endregion
    
	#region MonsterData
	[Serializable]
	public class MonsterData : CreatureData
	{
		public int DropItemId;
	}

	[Serializable]
	public class MonsterDataLoader : ILoader<int, MonsterData>
	{
		public List<MonsterData> monsters = new List<MonsterData>();
		public Dictionary<int, MonsterData> MakeDict()
		{
			Dictionary<int, MonsterData> dict = new Dictionary<int, MonsterData>();
			foreach (MonsterData monster in monsters)
				dict.Add(monster.DataId, monster);
			return dict;
		}
	}
	#endregion

	#region HeroData
	[Serializable]
	public class HeroData : CreatureData
	{
		public string Rarity;
		public int GachaSpawnWeight;
		public int GachaWeight;
		public int GachaExpCount;
		public float AtkSpeed;
		public float AtkTime;

	}

	[Serializable]
	public class HeroDataLoader : ILoader<int, HeroData>
	{
		public List<HeroData> heroes = new List<HeroData>();
		public Dictionary<int, HeroData> MakeDict()
		{
			Dictionary<int, HeroData> dict = new Dictionary<int, HeroData>();
			foreach (HeroData hero in heroes)
				dict.Add(hero.DataId, hero);
			return dict;
		}
	}
	#endregion

	#region SkillData
	[Serializable]
	public class SkillData
	{
		public int DataId;
		public string Name;
		public string ClassName;
		public string Description;
		public int ProjectileId;
		public string PrefabLabel;
		public string IconLabel;
		public string AnimName;
		public float CoolTime;
		public float DamageMultiplier;
		public float Duration;
		public float AnimImpactDuration;
		public string CastingSound;
		public float SkillRange;
		public float ScaleMultiplier;
		public int TargetCount;
		public List<int> EffectIds = new List<int>();
		public int NextLevelId;
		public int AoEId;
		public EEffectSize EffectSize;
	}

	[Serializable]
	public class SkillDataLoader : ILoader<int, SkillData>
	{
		public List<SkillData> skills = new List<SkillData>();

		public Dictionary<int, SkillData> MakeDict()
		{
			Dictionary<int, SkillData> dict = new Dictionary<int, SkillData>();
			foreach (SkillData skill in skills)
				dict.Add(skill.DataId, skill);
			return dict;
		}
	}
	#endregion

	#region ProjectileData
	[Serializable]
	public class ProjectileData
	{
		public int DataId;
		public string Name;
		public string ClassName;
		public string ComponentName;
		public string ProjectileSpriteName;
		public string PrefabLabel;
		public float Duration;
		public float HitSound;
		public float ProjRange;
		public float ProjSpeed;
	}

	[Serializable]
	public class ProjectileDataLoader : ILoader<int, ProjectileData>
	{
		public List<ProjectileData> projectiles = new List<ProjectileData>();

		public Dictionary<int, ProjectileData> MakeDict()
		{
			Dictionary<int, ProjectileData> dict = new Dictionary<int, ProjectileData>();
			foreach (ProjectileData projectile in projectiles)
				dict.Add(projectile.DataId, projectile);
			return dict;
		}
	}
	#endregion

	#region Env
	[Serializable]
	public class EnvData
	{
		public int DataId;
		public string DescriptionTextID;
		public string PrefabLabel;
		public float MaxHp;
		public int ResourceAmount;
		public float RegenTime;
		public List<String> SkeletonDataIDs = new List<String>();
		public int DropItemId;
	}

	[Serializable]
	public class EnvDataLoader : ILoader<int, EnvData>
	{
		public List<EnvData> envs = new List<EnvData>();
		public Dictionary<int, EnvData> MakeDict()
		{
			Dictionary<int, EnvData> dict = new Dictionary<int, EnvData>();
			foreach (EnvData env in envs)
				dict.Add(env.DataId, env);
			return dict;
		}
	}
	#endregion

	#region EffectData
	[Serializable]
	public class EffectData
	{
		public int DataId;
		public string Name;
		public string ClassName;
		public string DescriptionTextID;
		public string SkeletonDataID;
		public string IconLabel;
		public string SoundLabel;
		public float Amount;
		public float PercentAdd;
		public float PercentMult;
		public float TickTime;
		public float TickCount;
		public EEffectType EffectType;
	}

	[Serializable]
	public class EffectDataLoader : ILoader<int, EffectData>
	{
		public List<EffectData> effects = new List<EffectData>();
		public Dictionary<int, EffectData> MakeDict()
		{
			Dictionary<int, EffectData> dict = new Dictionary<int, EffectData>();
			foreach (EffectData effect in effects)
				dict.Add(effect.DataId, effect);
			return dict;
		}
	}
	#endregion

	#region AoEData
	[Serializable]
	public class AoEData
	{
		public int DataId;
		public string Name;
		public string ClassName;
		public string SkeletonDataID;
		public string SoundLabel;
		public float Duration;
		public List<int> AllyEffects = new List<int>();
		public List<int> EnemyEffects = new List<int>();
		public string AnimName;
	}

	[Serializable]
	public class AoEDataLoader : ILoader<int, AoEData>
	{
		public List<AoEData> aoes = new List<AoEData>();
		public Dictionary<int, AoEData> MakeDict()
		{
			Dictionary<int, AoEData> dict = new Dictionary<int, AoEData>();
			foreach (AoEData aoe in aoes)
				dict.Add(aoe.DataId, aoe);
			return dict;
		}
	}
	#endregion
		#endregion

}






	// #region MonsterData
	// [Serializable]
	// public class MonsterData : CreatureData
	// {
	// 	public int DropItemId;
	// }

	// [Serializable]
	// public class MonsterDataLoader : ILoader<int, MonsterData>
	// {
	// 	public List<MonsterData> monsters = new List<MonsterData>();
	// 	public Dictionary<int, MonsterData> MakeDict()
	// 	{
	// 		Dictionary<int, MonsterData> dict = new Dictionary<int, MonsterData>();
	// 		foreach (MonsterData monster in monsters)
	// 			dict.Add(monster.DataId, monster);
	// 		return dict;
	// 	}
	// }
	// #endregion
