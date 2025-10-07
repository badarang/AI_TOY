// /Assets/Scripts/Skills/DamageBehavior.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/DamageBehavior")]
public class DamageBehavior : SkillBehavior
{
    public int damage = 10;

public override bool CanExecute(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        if (!base.CanExecute(caster, targetPos, skill)) return false;
        
        var target = Core.Instance.GridManager.GetUnitAt(targetPos);
        if (target == null) return false;
        
        return target.factionData != caster.factionData;
    }

    
public override float Execute(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        // 스킬의 modifiers에서 증가된 데미지 가져오기
        int finalDamage = skill.GetModifiedValue("damage", damage);
        
        UnitBase target = Core.Instance.GridManager.GetUnitAt(targetPos);
        if (target != null)
        {
            DebugPrinter.LogColor(LogType.Action, $"{target.name}에게 {finalDamage}의 피해를 입혔습니다!");
            target.TakeDamage(finalDamage);
        }
        return 0f;
    }
}
