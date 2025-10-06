using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(menuName = "SkillBehaviors/MoveBehavior")]
public class MoveBehavior : SkillBehavior
{
    public float animationDuration = 0.4f;

    public override bool CanExecute(SkillContext context, SkillData skillData)
    {
        if (!base.CanExecute(context, skillData)) return false;
        
        var targetUnit = Core.Instance.GridManager.GetUnitAt(context.TargetPosition);
        return targetUnit == null;
    }

    
public override float Execute(SkillContext context)
    {
        var caster = context.Caster;

        Vector2Int startPos = caster.position;
        Vector2Int targetPos = context.TargetPosition;

        Core.Instance.GridManager.MoveUnit(startPos, targetPos);

        Vector3 targetWorldPos = new Vector3(targetPos.x + 0.5f, caster.transform.position.y, targetPos.y + 0.5f);
        caster.transform.DOJump(targetWorldPos, 0.5f, 1, animationDuration);

        DebugPrinter.LogColor(LogType.Unit, $"{caster.name} moved to {targetPos}");

        return animationDuration;
    }
}
