// /Assets/Scripts/Skills/ConditionalDamageBehavior.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/ConditionalDamageBehavior")]
public class ConditionalDamageBehavior : SkillBehavior
{
    public int extraDamage = 10;

    public override void Execute(SkillContext context)
    {
        if (context.blackboard.TryGetValue(BlackboardKeys.PushedUnitHitWall, out object hitWall) && (bool)hitWall)
        {
            if (context.blackboard.TryGetValue(BlackboardKeys.TargetUnit, out object targetObj) && targetObj is UnitBase target)
            {
                Debug.Log($"{target.name}이(가) 벽 충돌로 {extraDamage}의 추가 피해를 입습니다!");
                target.TakeDamage(extraDamage);
            }
        }
    }
}
