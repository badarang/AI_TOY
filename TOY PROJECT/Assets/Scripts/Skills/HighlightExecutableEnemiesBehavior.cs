using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "SkillBehaviors/HighlightExecutableEnemiesBehavior")]
public class HighlightExecutableEnemiesBehavior : SkillBehavior
{
    public int requiredHp = 1;

    public override void Execute(SkillContext context)
    {
        List<UnitBase> executableTargets = new List<UnitBase>();
        Vector2Int casterPos = context.Caster.position;

        // 8방향 탐색
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Skip self

                Vector2Int checkPos = casterPos + new Vector2Int(x, y);
                UnitBase targetUnit = Core.Instance.GridManager.GetUnitAt(checkPos);

                // HP가 1 이하인 적 유닛을 찾음
                if (targetUnit != null && targetUnit is EnemyUnit && targetUnit.hp <= requiredHp)
                {
                    executableTargets.Add(targetUnit);
                }
            }
        }

        if (executableTargets.Count > 0)
        {
            Debug.Log($"Found {executableTargets.Count} executable targets.");
            // GridManager를 통해 타겟들을 하이라이트
            Core.Instance.GridManager.HighlightTargets(executableTargets);
            
            // 컨텍스트에 하이라이트된 타겟 정보 저장
            context.HighlightedTargets = executableTargets;

            // TurnManager의 상태를 변경하여 추가 입력 대기
            Core.Instance.TurnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingSkillSubTarget);
        }
        else
        {
            // 처형할 대상이 없으면 즉시 턴 종료
            Debug.Log("No executable targets found. Ending turn.");
            Core.Instance.TurnManager.PausedSkill = null;
            Core.Instance.TurnManager.PausedSkillContext = null;
            Core.Instance.TurnManager.EndTurn();
        }
    }
}
