// /Assets/Scripts/Skills/MoveBehavior.cs
using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(menuName = "SkillBehaviors/MoveBehavior")]
public class MoveBehavior : SkillBehavior
{
    public float animationDuration = 0.4f;

    public override float Execute(SkillContext context)
    {
        var caster = context.Caster;

        Vector2Int startPos = caster.position;
        Vector2Int targetPos = context.TargetPosition;

        // 1. Update grid data
        Core.Instance.GridManager.MoveUnit(startPos, targetPos);

        // 2. Play visual tween
        Vector3 targetWorldPos = new Vector3(targetPos.x + 0.5f, 0, targetPos.y + 0.5f);
        caster.transform.DOJump(targetWorldPos, 0.5f, 1, animationDuration);

        Debug.Log($"{caster.name} moved to {targetPos}");

        return animationDuration; // 애니메이션 시간을 반환합니다.
    }
}
