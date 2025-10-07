using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/ConditionalDamageBehavior")]
public class ConditionalDamageBehavior : SkillBehavior
{
    public int damageAmount;
    
public override bool CanExecute(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        if (!base.CanExecute(caster, targetPos, skill)) return false;
        
        // 블랙보드에 타겟 유닛이 저장되어 있는지 확인
        return skill.blackboard.ContainsKey("targetUnit");
    }
public bool triggerOnWallHit;

public override float Execute(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        // 블랙보드에서 벽 충돌 여부 확인
        bool hitWall = skill.blackboard.ContainsKey("pushedUnitHitWall") && (bool)skill.blackboard["pushedUnitHitWall"];

        if (hitWall == triggerOnWallHit)
        {
            UnitBase target = skill.blackboard.ContainsKey("targetUnit") ? (UnitBase)skill.blackboard["targetUnit"] : null;
            if (target != null)
            {
                int finalDamage = skill.GetModifiedValue("conditionalDamage", damageAmount);
                target.TakeDamage(finalDamage);
                DebugPrinter.LogColor(LogType.Unit, $"ConditionalDamageBehavior: {target.name}에게 {finalDamage}의 조건부 데미지를 입혔습니다. (벽 충돌: {hitWall})");
            }
        }
        return 0f;
    }
}
