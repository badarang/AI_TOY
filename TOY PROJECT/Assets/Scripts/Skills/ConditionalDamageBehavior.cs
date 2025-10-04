using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/ConditionalDamageBehavior")]
public class ConditionalDamageBehavior : SkillBehavior
{
    public int damageAmount;
    public bool triggerOnWallHit;

    public override float Execute(SkillContext context)
    {
        bool hitWall = context.PushedUnitHitWall ?? false;

        if (hitWall == triggerOnWallHit)
        {
            UnitBase target = context.TargetUnit;
            if (target != null)
            {
                target.TakeDamage(damageAmount);
                DebugPrinter.DebugColor(DebugType.Unit, $"ConditionalDamageBehavior: {target.name}에게 {damageAmount}의 조건부 데미지를 입혔습니다. (벽 충돌: {hitWall})");
            }
        }
        return 0f;
    }
}
