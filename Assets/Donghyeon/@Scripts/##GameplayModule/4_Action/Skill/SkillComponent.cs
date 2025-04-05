using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Assets.Scripts.Data;
using Unity.Assets.Scripts.Objects;
using UnityEngine;
using static Define;
using Random = UnityEngine.Random;

public class SkillComponent : InitBase
{
	public List<SkillBase> SkillList { get; } = new List<SkillBase>();
	public List<SkillBase> ActiveSkills { get; set; } = new List<SkillBase>();

	public SkillBase DefaultSkill { get; private set; }
	public SkillBase EnvSkill { get; private set; }
	public SkillBase ASkill { get; private set; }
	public SkillBase BSkill { get; private set; }

	public SkillBase CurrentSkill
	{
		get
		{
			if (ActiveSkills.Count == 0)
				return DefaultSkill;

			int randomIndex = Random.Range(0, ActiveSkills.Count);
			return ActiveSkills[randomIndex];
		}
	}

	Creature _owner;
	ClientCreature _clientCreature;
	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		return true;
	}

	public void SetInfo(Creature owner, CreatureData creatureData, ClientCreature clientCreature) 
	{
		_owner = owner;
		_clientCreature = clientCreature;

		AddSkill(creatureData.DefaultSkillId, ESkillSlot.Default);
		AddSkill(creatureData.EnvSkillId, ESkillSlot.Env);
		AddSkill(creatureData.SkillAId, ESkillSlot.A);
		AddSkill(creatureData.SkillBId, ESkillSlot.B);
	}

	public void AddSkill(int skillTemplateID, Define.ESkillSlot skillSlot)
	{
		// if (skillTemplateID == 0)
		// 	return;

		// if (DataLoader.instance.SkillDic.TryGetValue(skillTemplateID, out var data) == false)
		// {
		// 	Debug.LogWarning($"AddSkill Failed {skillTemplateID}");
		// 	return;
		// }
		// //example NormalAttack
		// SkillBase skill = gameObject.AddComponent(Type.GetType(data.ClassName)) as SkillBase;
		// if (skill == null)
		// 	return;

		// skill.SetInfo(_owner, skillTemplateID, _clientCreature);

		// SkillList.Add(skill);

		// switch (skillSlot)
		// {
		// 	case Define.ESkillSlot.Default:
		// 		DefaultSkill = skill;
		// 		break;
		// 	case Define.ESkillSlot.Env:
		// 		EnvSkill = skill;
		// 		break;
		// 	case Define.ESkillSlot.A:
		// 		ASkill = skill;
		// 		ActiveSkills.Add(skill);
		// 		break;
		// 	case Define.ESkillSlot.B:
		// 		BSkill = skill;
		// 		ActiveSkills.Add(skill);
		// 		break;
		// }
	}
}
