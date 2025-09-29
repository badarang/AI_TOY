// /Assets/Scripts/Skills/PushBehavior.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/PushBehavior")]
public class PushBehavior : SkillBehavior
{
    public int pushDistance = 1;

    public override void Execute(SkillContext context)
    {
        UnitBase target = Core.Instance.GridManager.GetUnitAt(context.TargetPosition);
        if (target == null) return;

        // 블랙보드에 대상을 기록합니다.
        context.blackboard[BlackboardKeys.TargetUnit] = target;

        // 밀어낼 방향을 계산합니다.
        Vector2 directionFloat = new Vector2(target.position.x - context.Caster.position.x, target.position.y - context.Caster.position.y);
        directionFloat.Normalize();
        Vector2Int pushDirection = new Vector2Int(Mathf.RoundToInt(directionFloat.x), Mathf.RoundToInt(directionFloat.y));

        Vector2Int destination = target.position + pushDirection * pushDistance;

        if (!Core.Instance.GridManager.IsValidTile(destination) || Core.Instance.GridManager.HasUnitAt(destination))
        {
            context.blackboard[BlackboardKeys.PushedUnitHitWall] = true;
            Debug.Log($"{target.name}이(가) 벽 또는 다른 유닛에 부딪혔습니다!");
        }
        else
        {
            Core.Instance.GridManager.MoveUnit(target.position, destination);
            Debug.Log($"{target.name}을(를) {destination}으로 밀어냈습니다.");
        }
    }
}
