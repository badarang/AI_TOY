// /Assets/Scripts/Skills/ConditionalDamageBehavior.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/ConditionalDamageBehavior")]
public class ConditionalDamageBehavior : SkillBehavior
{
    public int extraDamage = 1;

    public override float Execute(SkillContext context)
    {
        DebugPrinter.DebugColor(DebugType.Unit, "ConditionalDamageBehavior: 실행됨. PushedUnitHitWall 키 확인 시작.");

        if (context.blackboard.TryGetValue(BlackboardKeys.PushedUnitHitWall, out object hitWallValue) && (bool)hitWallValue)
        {
            DebugPrinter.DebugColor(DebugType.Unit, "ConditionalDamageBehavior: PushedUnitHitWall 키가 true입니다. 추가 데미지를 적용합니다.");
            if (context.blackboard.TryGetValue(BlackboardKeys.TargetUnit, out object targetObj) && targetObj is UnitBase target)
            {
                DebugPrinter.DebugColor(DebugType.Unit, $"{target.name}이(가) 벽 충돌로 {extraDamage}의 추가 피해를 입습니다!");
                target.TakeDamage(extraDamage);
            }
        }
        else
        {
            DebugPrinter.DebugColor(DebugType.Unit, "ConditionalDamageBehavior: PushedUnitHitWall 키가 없거나 false입니다. 추가 데미지 없음.");
        }
        return 0f;
    }
}
