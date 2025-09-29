// /Assets/Scripts/Skills/MoveToTargetOriginBehavior.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/MoveToTargetOriginBehavior")]
public class MoveToTargetOriginBehavior : SkillBehavior
{
    public override void Execute(SkillContext context)
    {
        bool hitWall = context.blackboard.TryGetValue(BlackboardKeys.PushedUnitHitWall, out object val) && (bool)val;

        if (!hitWall)
        {
            // PushBehavior가 먼저 실행되었으므로, context.TargetPosition은 타겟의 '원래' 위치입니다.
            Core.Instance.GridManager.MoveUnit(context.Caster.position, context.TargetPosition);
            Debug.Log($"{context.Caster.name}이(가) 적의 원래 위치로 이동합니다.");
        }
    }
}
