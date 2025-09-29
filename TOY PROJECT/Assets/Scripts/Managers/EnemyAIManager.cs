using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 모든 적 유닛의 행동을 총괄하는 AI 관리자
public class EnemyAIManager : MonoBehaviour
{
    // 적 턴 시작 시 TurnManager에 의해 호출됨
    public IEnumerator ExecuteEnemyTurns()
    {
        Debug.Log("--- Enemy Turn Start ---");
        var enemies = Core.Instance.StageManager.GetEnemies();
        var player = Core.Instance.StageManager.GetPlayer();

        if (player == null)
        {
            Debug.LogWarning("Player not found, ending enemy turn.");
            yield break;
        }

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            DecideAndExecuteAction(enemy, player);

            // 각 적의 행동 사이에 딜레이
            yield return new WaitForSeconds(1.0f);
        }

        Debug.Log("--- Enemy Turn End ---");
    }

    private void DecideAndExecuteAction(EnemyUnit enemy, PlayerUnit player)
    {
        // 1. 사용할 수 있는 공격 스킬이 있는지 확인 (0번 스킬로 가정)
        if (enemy.unitData == null || enemy.unitData.skills == null || enemy.unitData.skills.Length == 0 || enemy.unitData.skills[0] == null)
        {
            Debug.Log($"{enemy.name} has no skills to use. Skipping turn.");
            return;
        }
        var attackSkill = enemy.unitData.skills[0];

        // 2. 플레이어가 공격 스킬의 사거리 내에 있는지 확인 (체비쇼프 거리 사용)
        int distance = GridUtils.ChebyshevDistance(enemy.position, player.position);
        Debug.Log($"[AI DECISION] {enemy.name}: Distance to player is {distance}. Attack range is {attackSkill.range}.");

        if (distance <= attackSkill.range)
        {
            // 3. 범위 내에 있으면 스킬 사용
            Debug.Log($"[AI ACTION] {enemy.name} is in range. Attempting to attack {player.name}.");
            enemy.UseSkill(0, player.position);
        }
        else
        {
            // 4. 범위 밖에 있으면 플레이어에게 이동
            MoveTowards(enemy, player);
        }
    }

    private void MoveTowards(EnemyUnit enemy, PlayerUnit player)
    {
        List<Vector2Int> path = Core.Instance.GridManager.FindPath(enemy.position, player.position);

        if (path != null && path.Count > 0)
        {
            Vector2Int targetPos = path[0];
            Debug.Log($"{enemy.name} moves towards {player.name} to {targetPos}.");
            enemy.Move(targetPos);
        }
        else
        {
            Debug.Log($"{enemy.name} can't find a path to the player.");
        }
    }
}
