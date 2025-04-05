using System.Collections;
using System.Collections.Generic;
using Unity.Assets.Scripts.Objects;
using UnityEngine;

public class AreaAttack : AreaSkill
{
	public override void SetInfo(Creature owner, int skillTemplateID, ClientCreature clientCreature)
	{
		base.SetInfo(owner, skillTemplateID, clientCreature);

		_angleRange = 90;

		AddIndicatorComponent();

		// if (_indicator != null)
		// 	_indicator.SetInfo(Owner, SkillData, Define.EIndicatorType.Cone);
	}

	public override void DoSkill()
	{
		base.DoSkill();

		SpawnSpellIndicator();
	}
}
