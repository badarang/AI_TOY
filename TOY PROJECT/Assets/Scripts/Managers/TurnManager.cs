using UnityEngine;
using System;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;

    public enum Turn { Player, Enemy }
    public Turn CurrentTurn { get; private set; }

    public enum PlayerTurnState { AwaitingUnitSelection, UnitSelected, PerformingAction }
    public PlayerTurnState CurrentPlayerState { get; private set; }

    public UIManager uiManager;
    public StageManager stageManager;

    public void StartPlayerTurn()
    {
        CurrentTurn = Turn.Player;
        CurrentPlayerState = PlayerTurnState.AwaitingUnitSelection;
        OnPlayerTurnStart?.Invoke();
        uiManager?.UpdateTurnOrder();
        // 플레이어 행동력 리셋 등 추가 가능
    }

    public void StartEnemyTurn()
    {
        CurrentTurn = Turn.Enemy;
        OnEnemyTurnStart?.Invoke();
        uiManager?.UpdateTurnOrder();
        // 적 AI 행동 시작
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
        // 간단한 적 턴 예시: 모든 적이 한 번씩 행동
        foreach (var enemy in stageManager.GetEnemies())
        {
            enemy.UseSkill(0, stageManager.GetPlayer().position); // 예시: 플레이어를 타겟
            yield return new WaitForSeconds(0.5f); // 행동 간 딜레이
        }
        yield return new WaitForSeconds(0.5f);
        EndTurn();
    }
}