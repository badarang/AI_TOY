using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/PushBehavior")]
public class PushBehavior : SkillBehavior
{
    public override bool CanExecute(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        if (!base.CanExecute(caster, targetPos, skill))
            return false;

        var target = Core.Instance.GridManager.GetUnitAt(targetPos);
        if (target == null)
            return false;

        return target.factionData != caster.factionData;
    }

    public int pushDistance = 1;

    public override UniTask ExecuteAsync(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        UnitBase target = Core.Instance.GridManager.GetUnitAt(targetPos);
        if (target == null) return UniTask.CompletedTask;

        // 블랙보드에 타겟 저장
        skill.blackboard["targetUnit"] = target;

        Vector2 directionFloat = new Vector2(
            target.position.x - caster.position.x,
            target.position.y - caster.position.y
        );
        directionFloat.Normalize();
        Vector2Int pushDirection = new Vector2Int(
            Mathf.RoundToInt(directionFloat.x),
            Mathf.RoundToInt(directionFloat.y)
        );

        Vector2Int destination = target.position + pushDirection * pushDistance;

        if (
            !Core.Instance.GridManager.IsValidTile(destination)
            || Core.Instance.GridManager.HasUnitAt(destination)
        )
        {
            // 벽 충돌 정보를 블랙보드에 저장
            skill.blackboard["pushedUnitHitWall"] = true;
            DebugPrinter.LogColor(
                LogType.Unit,
                $"{target.name}이(가) 벽 또는 다른 유닛에 부딕혔습니다!"
            );
        }
        else
        {
            skill.blackboard["pushedUnitHitWall"] = false;
            Core.Instance.GridManager.MoveUnit(target.position, destination);

            Vector3 targetWorldPos = new Vector3(
                destination.x + 0.5f,
                target.transform.position.y,
                destination.y + 0.5f
            );
            target.transform.position = targetWorldPos;

            DebugPrinter.LogColor(
                LogType.Unit,
                $"{target.name}을(를) {destination}으로 밀어냈습니다."
            );
        }
        return UniTask.CompletedTask;
    }
}
