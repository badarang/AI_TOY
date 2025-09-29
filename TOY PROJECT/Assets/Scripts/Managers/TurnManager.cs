using UnityEngine;
using System;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;

    public enum Turn { Player, Enemy }
    public Turn CurrentTurn { get; private set; }

    public enum PlayerTurnState { AwaitingUnitSelection, UnitSelected, PerformingAction, AwaitingSkillSubTarget }
    public PlayerTurnState CurrentPlayerState { get; private set; }

    public SkillBase PausedSkill { get; set; }
    public SkillContext PausedSkillContext { get; set; }

    public UIManager uiManager;
    public StageManager stageManager;

    public void StartPlayerTurn()
    {
        CurrentTurn = Turn.Player;
        CurrentPlayerState = PlayerTurnState.AwaitingUnitSelection;
        
        // 플레이어 AP 회복
        var player = stageManager.GetPlayer();
        if (player != null && player.unitData != null)
        {
            player.ap = Mathf.Min(player.ap + 1, player.unitData.maxAp);
            Debug.Log($"{player.name} AP recovered. Current AP: {player.ap}");
        }

        OnPlayerTurnStart?.Invoke();
        uiManager?.UpdateTurnOrder();
    }

    public void StartEnemyTurn()
    {
        CurrentTurn = Turn.Enemy;

        // 모든 적 AP 회복
        var enemies = stageManager.GetEnemies();
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.unitData != null)
            {
                enemy.ap = Mathf.Min(enemy.ap + 1, enemy.unitData.maxAp);
                Debug.Log($"{enemy.name} AP recovered. Current AP: {enemy.ap}");
            }
        }

        OnEnemyTurnStart?.Invoke();
        uiManager?.UpdateTurnOrder();
        StartCoroutine(EnemyTurnRoutine());
    }

    public void EndTurn()
    {
        if (CurrentTurn == Turn.Player)
        {
            StartEnemyTurn();
        }
        else
        {
            StartPlayerTurn();
        }
    }

    public void SetPlayerState(PlayerTurnState newState)
    {
        CurrentPlayerState = newState;
        Debug.Log($"Player state changed to: {newState}");
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return Core.Instance.EnemyAIManager.ExecuteEnemyTurns();
        EndTurn();
    }
}
