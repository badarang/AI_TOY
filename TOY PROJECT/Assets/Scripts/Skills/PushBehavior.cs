// /Assets/Scripts/Skills/PushBehavior.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/PushBehavior")]
public class PushBehavior : SkillBehavior
{
    public int pushDistance = 1;

    public override float Execute(SkillContext context)
    {
        UnitBase target = Core.Instance.GridManager.GetUnitAt(context.TargetPosition);
        if (target == null) return 0f;

        // 블랙보드에 대상을 기록합니다.
        context.blackboard[BlackboardKeys.TargetUnit] = target;

        // 밀어낼 방향을 계산합니다. (Vector2로 변환 후 정규화하고 다시 Vector2Int로 변환)
        Vector2 directionFloat = new Vector2(target.position.x - context.Caster.position.x, target.position.y - context.Caster.position.y);
        directionFloat.Normalize();
        Vector2Int pushDirection = new Vector2Int(Mathf.RoundToInt(directionFloat.x), Mathf.RoundToInt(directionFloat.y));

        Vector2Int destination = target.position + pushDirection * pushDistance;

        if (!Core.Instance.GridManager.IsValidTile(destination) || Core.Instance.GridManager.HasUnitAt(destination))
        {
            DebugPrinter.DebugColor(DebugType.Unit, "PushBehavior: 벽 또는 유닛 충돌. PushedUnitHitWall = true");
            context.blackboard[BlackboardKeys.PushedUnitHitWall] = true;
            DebugPrinter.DebugColor(DebugType.Unit, $"{target.name}이(가) 벽 또는 다른 유닛에 부딪혔습니다!");
        }
        else
        {
            DebugPrinter.DebugColor(DebugType.Unit, "PushBehavior: 충돌 없음. PushedUnitHitWall = false");
            context.blackboard[BlackboardKeys.PushedUnitHitWall] = false;
            Core.Instance.GridManager.MoveUnit(target.position, destination);

            // 밀려난 대상의 비주얼 위치도 동기화합니다.
            Vector3 targetWorldPos = new Vector3(destination.x + 0.5f, target.transform.position.y, destination.y + 0.5f);
            target.transform.position = targetWorldPos;

            DebugPrinter.DebugColor(DebugType.Unit, $"{target.name}을(를) {destination}으로 밀어냈습니다.");
        }
        return 0f;
    }
}
