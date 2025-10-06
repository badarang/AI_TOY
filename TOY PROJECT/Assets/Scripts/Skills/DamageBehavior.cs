// /Assets/Scripts/Skills/DamageBehavior.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/DamageBehavior")]
public class DamageBehavior : SkillBehavior
{
    public int damage = 10;

    public override bool CanExecute(SkillContext context, SkillData skillData)
    {
        if (!base.CanExecute(context, skillData)) return false;
        
        var target = Core.Instance.GridManager.GetUnitAt(context.TargetPosition);
        if (target == null) return false;
        
        return target.factionData != context.Caster.factionData;
    }

    
public override float Execute(SkillContext context)
    {
        UnitBase target = Core.Instance.GridManager.GetUnitAt(context.TargetPosition);
        if (target != null)
        {
            Debug.Log($"{target.name}에게 {damage}의 피해를 입혔습니다!");
            target.TakeDamage(damage);
        }
        return 0f; // 즉시 끝나는 행동이므로 0을 반환합니다.
    }
}
