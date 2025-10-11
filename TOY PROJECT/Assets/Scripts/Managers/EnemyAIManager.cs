using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 모든 적 유닛의 행동을 총괄하는 AI 관리자
public class EnemyAIManager : MonoBehaviour, IManager
{

public void BeforeInit()
    {
    }

    public void AfterInit()
    {
    }

    // 적 턴 시작 시 TurnManager에 의해 호출됨
    public IEnumerator ExecuteEnemyTurns()
    {
        Debug.Log("--- Enemy Turn Start ---");
        var enemies = Core.Instance.UnitManager.GetEnemies();
        var players = Core.Instance.UnitManager.GetAllPlayers();

        if (players == null || players.Count == 0)
        {
            Debug.LogWarning("No players found, ending enemy turn.");
            yield break;
        }

        // Simple AI: Always target the first player (usually the host).
        var targetPlayer = players[0];

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            DecideAndExecuteAction(enemy, targetPlayer);

            // 각 적의 행동 사이에 딜레이
            yield return new WaitForSeconds(1.0f);
        }

        Debug.Log("--- Enemy Turn End ---");
    }

private void DecideAndExecuteAction(EnemyUnit enemy, PlayerUnit player)
    {
        var decision = EnemyDecisionLogic.DecideAction(enemy, player.position);

        switch (decision.actionType)
        {
            case EnemyDecision.ActionType.Attack:
                Debug.Log($"[AI ACTION] {enemy.name} is in range. Attempting to attack {player.name}.");
                enemy.UseSkill(decision.skillIndex, decision.targetPosition);
                break;

            case EnemyDecision.ActionType.Move:
                Debug.Log($"[AI ACTION] {enemy.name} moving to {decision.targetPosition} using skill index {decision.skillIndex}");
                enemy.UseSkill(decision.skillIndex, decision.targetPosition);
                break;

            case EnemyDecision.ActionType.Wait:
            default:
                Debug.Log($"[AI DECISION] {enemy.name} will wait.");
                break;
        }
    }


}
