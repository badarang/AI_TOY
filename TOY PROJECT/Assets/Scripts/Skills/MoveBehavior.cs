using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(menuName = "SkillBehaviors/MoveBehavior")]
public class MoveBehavior : SkillBehavior
{
    public float animationDuration = 0.4f;

    public override bool CanExecute(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        if (!base.CanExecute(caster, targetPos, skill)) return false;

        // 1. 목표 타일이 그리드 범위 안인지 확인
        if (!Core.Instance.GridManager.IsValidTile(targetPos)) return false;

        // 2. 목표 타일에 다른 유닛이 없는지 확인
        var targetUnit = Core.Instance.GridManager.GetUnitAt(targetPos);
        if (targetUnit != null) return false;

        // 3. 전투가 끝났으면 거리 제한 없이 이동 가능
        if (Core.Instance?.TurnManager != null && Core.Instance.TurnManager.BattleEnded)
        {
            return true;
        }

        // 4. 일반 전투 중에는 스킬의 이동 패턴(사거리) 안에 목표 타일이 있는지 확인
        bool isWithinPattern = false;
        if (skill.data.movementPattern != null)
        {
            foreach (var offset in skill.data.movementPattern)
            {
                if (caster.position + offset == targetPos)
                {
                    isWithinPattern = true;
                    break;
                }
            }
        }

        return isWithinPattern;
    }

    public override UniTask ExecuteAsync(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        Vector2Int startPos = caster.position;

        Core.Instance.GridManager.MoveUnit(startPos, targetPos);

        Vector3 targetWorldPos = new Vector3(targetPos.x + 0.5f, caster.transform.position.y, targetPos.y + 0.5f);
        caster.transform.DOJump(targetWorldPos, 0.5f, 1, animationDuration);

        DebugPrinter.LogColor(LogType.Unit, $"{caster.name} moved to {targetPos}");

        return UniTask.CompletedTask;
    }
}
