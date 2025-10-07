using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(menuName = "SkillBehaviors/MoveBehavior")]
public class MoveBehavior : SkillBehavior
{
    public float animationDuration = 0.4f;

public override bool CanExecute(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        if (!base.CanExecute(caster, targetPos, skill)) return false;
        
        var targetUnit = Core.Instance.GridManager.GetUnitAt(targetPos);
        return targetUnit == null;
    }

    
public override float Execute(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        Vector2Int startPos = caster.position;

        Core.Instance.GridManager.MoveUnit(startPos, targetPos);

        Vector3 targetWorldPos = new Vector3(targetPos.x + 0.5f, caster.transform.position.y, targetPos.y + 0.5f);
        caster.transform.DOJump(targetWorldPos, 0.5f, 1, animationDuration);

        DebugPrinter.LogColor(LogType.Unit, $"{caster.name} moved to {targetPos}");

        return animationDuration;
    }
}
