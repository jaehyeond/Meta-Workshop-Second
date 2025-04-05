using System.Collections;
using System.Collections.Generic;
using Unity.Assets.Scripts.Objects;
using UnityEngine;
using static Define;

public class AreaSkill : SkillBase
{
	// protected SpellIndicator _indicator;
	protected Vector2 _skillDir;
	protected EIndicatorType _indicatorType = EIndicatorType.Cone;
	protected int _angleRange = 360;

	public override void SetInfo(Creature owner, int skillTemplateID, ClientCreature clientCreature)
	{
		base.SetInfo(owner, skillTemplateID, clientCreature);
	}

	public override void DoSkill()
	{
		base.DoSkill();

		if (Owner.CreatureState != ECreatureState.Skill)
			return;

		_skillDir = (Owner.Target.transform.position - Owner.transform.position).normalized;
	}

	public override void CancelSkill()
	{
		// if (_indicator)
		// 	_indicator.Cancel();
	}

	protected void AddIndicatorComponent()
	{
		// _indicator = Util.FindChild<SpellIndicator>(gameObject, recursive: true);
		// if (_indicator == null)
		// {
		// 	GameObject go = Managers.Resource.Instantiate(SkillData.PrefabLabel, gameObject.transform);
		// 	_indicator = Util.GetOrAddComponent<SpellIndicator>(go);
		// }
	}

	protected void SpawnSpellIndicator()
	{
		if (Owner.Target.IsValid() == false)
			return;

		// _indicator.ShowCone(Owner.transform.position, _skillDir.normalized, _angleRange);
	}

	protected override void OnAttackEvent()
	{
		// float radius = Util.GetEffectRadius(SkillData.EffectSize);
		// List<Creature> targets = Managers.Object.FindConeRangeTargets(Owner, _skillDir, radius, _angleRange);

		// foreach (var target in targets)
		// {
		// 	if (target.IsValid())
		// 	{
		// 		target.OnDamaged(Owner, this);
		// 	}
		// }
	}
}
