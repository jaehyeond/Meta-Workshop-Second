using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class LayerNames
{
    public static readonly int Monster = LayerMask.NameToLayer("Monster");
	public static readonly int Hero = LayerMask.NameToLayer("Hero");

    // 다른 레이어도 필요하면 추가
}


public static class Define
{



	public const string GOOGLE_PLAY_STORE = "https://play.google.com/store/apps/details?id=com.DreadNought.PokerDefense";
    public const string APPLE_APP_STORE = "";

    public const float THIRD_PARTY_SERVICE_INIT_TIME = 1f;

    public const int MAX_CHAPTER = 4;



	public enum AchievementType
	{
		/// <summary>
		/// 골드 수집 업적
		/// </summary>
		CollectGold = 0,
		
		/// <summary>
		/// 레벨 달성 업적
		/// </summary>
		ReachLevel = 1,
		
		/// <summary>
		/// 게임 플레이 횟수 업적
		/// </summary>
		PlayGames = 2,
		
		/// <summary>
		/// 아이템 획득 업적
		/// </summary>
		CollectItems = 3,
		
		/// <summary>
		/// 적 처치 업적
		/// </summary>
		DefeatEnemies = 4,
		
		/// <summary>
		/// 스킬 사용 업적
		/// </summary>
		UseSkills = 5,
		
		/// <summary>
		/// 퀘스트 완료 업적
		/// </summary>
		CompleteQuests = 6,
		
		InviteFriends = 7,

		ConsecutiveLogins = 8,
		
		PlayTime = 9
	}




	public enum ItemType
	{
		Weapon,
		Shield,
		ChestArmor,
		Gloves,
		Boots,
		Accessory,
	}

    public enum RewardType
    {
        Gold,
        Gem,
    }


	public enum HostType
	{
		Host,
		Client,
		All
	}
	public enum ESkillSlot
	{
		Default,
		Env,
		A,
		B
	}
	public enum EOrganizer
	{
		HOST,
		CLIENT
	}
    public enum EUIEvent
	{
		Click,
		PointerDown,
		PointerUp,
		Drag,
	}
		public enum EObjectType
	{
		None,
		HeroCamp,
		Creature,
		Projectile,
		Env,
		Effect,
		Monster,
		Hero
	}
		public enum ECreatureState
	{
		None,
		Idle,
		Move,
		Skill,
		OnDamaged,
		Dead
	}
	public enum EIndicatorType
	{
		None,
		Cone,
		Rectangle,
	}
	public enum EEffectType
	{
		Buff,
		Debuff,
		CrowdControl,
	}
	public enum EEffectSize
	{
		CircleSmall,
		CircleNormal,
		CircleBig,
		ConeSmall,
		ConeNormal,
		ConeBig,
	}
	public enum EEffectClearType
	{
		TimeOut, // 시간초과로 인한 Effect 종료
		ClearSkill, // 정화 스킬로 인한 Effect 종료
		TriggerOutAoE, // AoE스킬을 벗어난 종료
		EndOfAirborne, // 에어본이 끝난 경우 호출되는 종료
	}
	public enum EEffectSpawnType
	{
		Skill, // 지속시간이 있는 기본적인 이펙트 
		External, // 외부(장판스킬)에서 이펙트를 관리(지속시간에 영향을 받지않음)
	}
	public enum EEffectClassName
	{
		Bleeding,
		Poison,
		Ignite,
		Heal,
		AttackBuff,
		MoveSpeedBuff,
		AttackSpeedBuff,
		LifeStealBuff,
		ReduceDmgBuff,
		ThornsBuff,
		Knockback,
		Airborne,
		PullEffect,
		Stun,
		Freeze,
		CleanDebuff,
	}

	public enum EStatModType
	{
		Add,
		PercentAdd,
		PercentMult,
	}
    public enum EJoystickState
    {
        PointerDown,
        PointerUp,
        Drag
    }
	public const int HERO_WIZARD_ID = 201000;
	public const int HERO_KNIGHT_ID = 201001;
	public const int HERO_LION_ID = 201003;

	public const int MONSTER_SLIME_ID = 202001;
	public const int MONSTER_SPIDER_COMMON_ID = 202002;
	public const int MONSTER_WOOD_COMMON_ID = 202004;
	public const int MONSTER_GOBLIN_ARCHER_ID = 202005;
	public const int MONSTER_BEAR_ID = 202006;

	public const int ENV_TREE1_ID = 300001;
	public const int ENV_TREE2_ID = 301000;

	public const char MAP_TOOL_WALL = '0';
	public const char MAP_TOOL_NONE = '1';
	public const char MAP_TOOL_SEMI_WALL = '2';

}
	public enum ECreatureState
	{
		None,
		Idle,
		Move,
		Skill,
		OnDamaged,
		Dead
	}

	public enum EHeroMoveState
	{
		None,
		TargetMonster,
		CollectEnv,
		ReturnToCamp,
		ForceMove,
		ForcePath
	}


public static class SortingLayers
{
	public const int SPELL_INDICATOR = 200;
	public const int CREATURE = 300;
	public const int ENV = 300;
	public const int NPC = 310;
	public const int PROJECTILE = 310;
	public const int SKILL_EFFECT = 310;
	public const int DAMAGE_FONT = 410;
}

public static class AnimName
{
	public const string ATTACK_A = "attack";
	public const string ATTACK_B = "attack";
	public const string SKILL_A = "skill";
	public const string SKILL_B = "skill";
	public const string IDLE = "idle";
	public const string MOVE = "move";
	public const string DAMAGED = "hit";
	public const string DEAD = "dead";
	public const string EVENT_ATTACK_A = "event_attack";
	public const string EVENT_ATTACK_B = "event_attack";
	public const string EVENT_SKILL_A = "event_attack";
	public const string EVENT_SKILL_B = "event_attack";
}