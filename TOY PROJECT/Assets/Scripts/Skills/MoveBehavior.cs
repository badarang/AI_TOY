using UnityEngine;

using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(menuName = "SkillBehaviors/MoveBehavior")]
public class MoveBehavior : SkillBehavior
{
    public override void Execute(SkillContext context)
    {
        UnitBase caster = context.Caster;
        Vector2Int startPos = caster.position;
        Vector2Int targetPos = context.TargetPosition;

        // 1. Update grid data
        Core.Instance.GridManager.MoveUnit(startPos, targetPos);

        // 2. Play visual tween
        Vector3 targetWorldPos = new Vector3(targetPos.x + 0.5f, 0, targetPos.y + 0.5f);
        caster.transform.DOJump(targetWorldPos, 0.5f, 1, 0.4f).OnComplete(() =>
        {
            // 3. Refresh action highlights after move is complete
            if (caster.ap > 0)
            {
                caster.ShowAvailableActions();
            }
            else
            {
                Core.Instance.GridManager.ClearSelection();
                // TODO: Link with TurnManager to end turn
            }
        });

        Debug.Log($"{caster.name} moved to {targetPos}");
    }
}
