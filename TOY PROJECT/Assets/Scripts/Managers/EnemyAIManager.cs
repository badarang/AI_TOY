using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

// 모든 적 유닛의 행동을 총괄하는 AI 관리자
public class EnemyAIManager : NetworkBehaviour, IManager
{
    public void BeforeInit() { }

    public void AfterInit() { }

    public void Dispose() { }

    // 적 턴 시작 시 TurnManager에 의해 호출됨
    // 서버에서만 실행되어야 함
    public async UniTask ExecuteEnemyTurns()
    {
        // 서버 권한 체크 - 클라이언트에서는 실행하지 않음
        if (!HasStateAuthority)
        {
            Debug.LogWarning("[EnemyAIManager] ExecuteEnemyTurns called on client, ignoring.");
            return;
        }

        Debug.Log("--- Enemy Turn Start (Server) ---");
        var enemies = Core.Instance.UnitManager.GetEnemies();
        var players = Core.Instance.UnitManager.GetAllPlayers();

        if (players == null || players.Count == 0)
        {
            Debug.LogWarning("No players found, ending enemy turn.");
            return;
        }

        var targetPlayer = players[0];

        if (targetPlayer == null || targetPlayer.hp <= 0)
        {
            Debug.Log("Target player is dead, ending enemy turn.");
            return;
        }

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
        if (player == null || player.hp <= 0)
        {
            Debug.Log("Target player died, skipping remaining enemy actions.");
            return;
        }

        var decision = EnemyDecisionLogic.DecideAction(enemy, player.position);

        if (Core.Instance?.PreviewManager != null)
        {
            Core.Instance.PreviewManager.ClearPreviewForEnemy(enemy);
        }

        switch (decision.actionType)
        {
            case EnemyDecision.ActionType.Attack:
                Debug.Log(
                    $"[AI ACTION] {enemy.name} is in range. Attempting to attack {player.name}."
                );
                await enemy.UseSkillAsync(decision.skillIndex, decision.targetPosition);
                break;

            case EnemyDecision.ActionType.Move:
                Debug.Log(
                    $"[AI ACTION] {enemy.name} moving to {decision.targetPosition} using skill index {decision.skillIndex}"
                );
                await enemy.UseSkillAsync(decision.skillIndex, decision.targetPosition);
                break;

            case EnemyDecision.ActionType.Wait:
            default:
                Debug.Log($"[AI DECISION] {enemy.name} will wait.");
                break;
        }
    }
}
