using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 모든 적 유닛의 행동을 총괄하는 AI 관리자
public class EnemyAIManager : MonoBehaviour, IManager
{
    public void BeforeInit() { }

    public void AfterInit() { }

    // 적 턴 시작 시 TurnManager에 의해 호출됨
    public async UniTask ExecuteEnemyTurns()
    {
        Debug.Log("--- Enemy Turn Start ---");
        var enemies = Core.Instance.UnitManager.GetEnemies();
        var players = Core.Instance.UnitManager.GetAllPlayers();

        if (players == null || players.Count == 0)
        {
            Debug.LogWarning("No players found, ending enemy turn.");
            return;
        }

        // Simple AI: Always target the first player (usually the host).
        var targetPlayer = players[0];

        foreach (var enemy in enemies)
        {
            if (enemy == null)
                continue;

            await DecideAndExecuteAction(enemy, targetPlayer);

            await UniTask.Delay(500);
        }

        Debug.Log("--- Enemy Turn End ---");
    }

    private async UniTask DecideAndExecuteAction(EnemyUnit enemy, PlayerUnit player)
    {
        var decision = EnemyDecisionLogic.DecideAction(enemy, player.position);

        if (Core.Instance?.PreviewManager != null)
        {
            Core.Instance.PreviewManager.ClearPreviewForEnemy(enemy);
        }

        switch (decision.actionType)
        {
            case EnemyDecision.ActionType.Attack:
                Debug.Log($"[AI ACTION] {enemy.name} is in range. Attempting to attack {player.name}.");
                await enemy.UseSkillAsync(decision.skillIndex, decision.targetPosition);
                break;

            case EnemyDecision.ActionType.Move:
                Debug.Log($"[AI ACTION] {enemy.name} moving to {decision.targetPosition} using skill index {decision.skillIndex}");
                await enemy.UseSkillAsync(decision.skillIndex, decision.targetPosition);
                break;

            case EnemyDecision.ActionType.Wait:
            default:
                Debug.Log($"[AI DECISION] {enemy.name} will wait.");
                break;
        }
    }}
