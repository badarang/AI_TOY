using UnityEngine;
using System.Collections.Generic;

public struct EnemyDecision
{
    public enum ActionType
    {
        Attack,
        Move,
        Wait
    }

    public ActionType actionType;
    public Vector2Int targetPosition;
    public int skillIndex;
    public List<Vector2Int> affectedTiles;
}

public static class EnemyDecisionLogic
{
    public static EnemyDecision DecideAction(UnitBase enemy, Vector2Int playerPosition, bool isPreview = false, HashSet<Vector2Int> occupiedTiles = null)    {
        if (enemy.unitData == null || enemy.unitData.skills == null)
        {
            return CreateWaitDecision(enemy.position);
        }

        // Preview 모드일 때 "다음 턴 시작 직후" 상태로 판단
        int effectiveAP = isPreview ? enemy.unitData.maxAp : enemy.ap;

        for (int i = 0; i < enemy.unitData.skills.Length; i++)
        {
            var skill = enemy.unitData.skills[i];

            if (skill.skillType == SkillType.Attack)
            {
                int distance = GridUtils.ChebyshevDistance(enemy.position, playerPosition);
                int effectiveCooldown = isPreview ? Mathf.Max(0, enemy.GetSkillCooldown(i) - 1) : enemy.GetSkillCooldown(i);

                if (distance <= skill.range && effectiveCooldown == 0 && effectiveAP >= skill.apCost)
                {
                    var decision = new EnemyDecision
                    {
                        actionType = EnemyDecision.ActionType.Attack,
                        targetPosition = playerPosition,
                        skillIndex = i,
                        affectedTiles = new List<Vector2Int> { playerPosition }
                    };

                    DebugPrinter.LogColor(LogType.AI, $"{enemy.name} will ATTACK player at {playerPosition}");
                    return decision;
                }
            }
        }

        int moveSkillIndex = GetMoveSkillIndex(enemy);
        if (moveSkillIndex >= 0)
        {
            var moveSkill = enemy.unitData.skills[moveSkillIndex];
            int effectiveMoveCooldown = isPreview ? Mathf.Max(0, enemy.GetSkillCooldown(moveSkillIndex) - 1) : enemy.GetSkillCooldown(moveSkillIndex);

            if (effectiveMoveCooldown == 0 && effectiveAP >= moveSkill.apCost)
            {
                var movableTiles = Core.Instance.GridManager.GetWalkableTilesInRange(enemy.position, moveSkill.range);

                Vector2Int? bestMoveTarget = null;
                int closestChebyshevDistance = int.MaxValue;
                int closestManhattanDistance = int.MaxValue;

                foreach (var tile in movableTiles)
                {
                    if (Core.Instance.GridManager.GetUnitAt(tile) != null)
                        continue;

                    if (occupiedTiles != null && occupiedTiles.Contains(tile))
                        continue;

                    int chebyshevDist = GridUtils.ChebyshevDistance(tile, playerPosition);
                    int manhattanDist = GridUtils.ManhattanDistance(tile, playerPosition);

                    bool isBetter = false;

                    if (chebyshevDist < closestChebyshevDistance)
                    {
                        isBetter = true;
                    }
                    else if (chebyshevDist == closestChebyshevDistance && manhattanDist < closestManhattanDistance)
                    {
                        isBetter = true;
                    }

                    if (isBetter)
                    {
                        closestChebyshevDistance = chebyshevDist;
                        closestManhattanDistance = manhattanDist;
                        bestMoveTarget = tile;
                    }
                }

                if (bestMoveTarget.HasValue && bestMoveTarget.Value != enemy.position)
                {
                    var decision = new EnemyDecision
                    {
                        actionType = EnemyDecision.ActionType.Move,
                        targetPosition = bestMoveTarget.Value,
                        skillIndex = moveSkillIndex,
                        affectedTiles = new List<Vector2Int>()
                    };

                    DebugPrinter.LogColor(LogType.AI, $"{enemy.name} will MOVE to {bestMoveTarget.Value}");
                    return decision;
                }
            }
        }

        DebugPrinter.LogColor(LogType.AI, $"{enemy.name} will WAIT");
        return CreateWaitDecision(enemy.position);
    }

    private static EnemyDecision CreateWaitDecision(Vector2Int position)
    {
        return new EnemyDecision
        {
            actionType = EnemyDecision.ActionType.Wait,
            targetPosition = position,
            skillIndex = -1,
            affectedTiles = new List<Vector2Int>()
        };
    }

    private static int GetMoveSkillIndex(UnitBase enemy)
    {
        if (enemy.unitData == null || enemy.unitData.skills == null)
            return -1;

        for (int i = 0; i < enemy.unitData.skills.Length; i++)
        {
            if (enemy.unitData.skills[i].skillType == SkillType.Move)
                return i;
        }
        return -1;
    }
}