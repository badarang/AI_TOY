using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/DamageBehavior")]
public class DamageBehavior : SkillBehavior
{
    public int damage = 1;

    public override void Execute(SkillContext context)
    {
        Debug.Log($"[DamageBehavior] Executing. Caster: {context.Caster.name}, TargetPos: {context.TargetPosition}.");

        // 스킬의 타겟 위치에 유닛이 있는지 확인
        UnitBase targetUnit = Core.Instance.GridManager.GetUnitAt(context.TargetPosition);

        if (targetUnit != null)
        {
            Debug.Log($"[DamageBehavior] Target '{targetUnit.name}' found. Calling TakeDamage({damage}).");
            targetUnit.TakeDamage(damage);
        }
        else
        {
            Debug.LogError($"[DamageBehavior] FAILED. No target unit found at position {context.TargetPosition}.");
        }
    }
}
