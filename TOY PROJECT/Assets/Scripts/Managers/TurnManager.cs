using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class TurnManager : MonoBehaviour
{
    // Events
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;

    // Game State
    public enum Turn { Player, Enemy }
    public Turn CurrentTurn { get; private set; }
    public enum PlayerTurnState { AwaitingUnitSelection, UnitSelected, PerformingAction, AwaitingSkillSubTarget, StageClear }
    public PlayerTurnState CurrentPlayerState { get; private set; }

    // Wave & Turn Limit System
    private int currentWave = 1;
    private int turnInWave = 0;

    // Paused Skill State
    public SkillData PausedSkillData { get; set; }
    public SkillContext PausedSkillContext { get; set; }

    // Manager References
    private UIManager uiManager;
    private StageManager stageManager;
    private GridManager gridManager;

    void Start()
    {
        uiManager = Core.Instance.UIManager;
        stageManager = Core.Instance.StageManager;
        gridManager = Core.Instance.GridManager;
    }

    /// <summary>
    /// Clears all turn-related data. Called when a new stage is loaded.
    /// </summary>
    public void ClearTurn()
    {
        StopAllCoroutines(); // Stop any running enemy turn routines

        CurrentTurn = Turn.Player;
        CurrentPlayerState = PlayerTurnState.AwaitingUnitSelection;
        
        currentWave = 0;
        turnInWave = 0;
        
        PausedSkillData = null;
        PausedSkillContext = null;

        if (gridManager != null)
        {
            gridManager.ClearSelection();
        }
        
        Debug.Log("TurnManager state cleared.");
    }

    public void StartFirstWave()
    {
        currentWave = 1;
        turnInWave = 0;
        stageManager.SpawnWave(currentWave);
        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        CurrentTurn = Turn.Player;
        turnInWave++;
        SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        
        var player = stageManager.GetPlayer();
        if (player != null) player.OnTurnStart();

        OnPlayerTurnStart?.Invoke();
        
        int turnLimit = stageManager.GetCurrentStageData()?.waves[currentWave - 1].clearTurnLimit ?? 5;
        uiManager.UpdateTurnUI(turnInWave, turnLimit);
    }

    public void StartEnemyTurn()
    {
        CurrentTurn = Turn.Enemy;
        SetPlayerState(PlayerTurnState.PerformingAction);

        var enemies = stageManager.GetEnemies();
        foreach (var enemy in enemies) if (enemy != null) enemy.OnTurnStart();

        OnEnemyTurnStart?.Invoke();
        StartCoroutine(EnemyTurnRoutine());
    }

    public void EndTurn()
    {
        if (CurrentPlayerState == PlayerTurnState.StageClear) return;

        if (CurrentTurn == Turn.Player)
        {
            StartEnemyTurn();
        }
        else // Enemy turn is ending
        {
            int turnLimit = stageManager.GetCurrentStageData()?.waves[currentWave - 1].clearTurnLimit ?? 5;
            if (turnInWave >= turnLimit)
            {
                Debug.Log("방송 시간 초과! 패널티 라운드에 돌입합니다!");
                currentWave++;
                stageManager.SpawnWave(currentWave);
                turnInWave = 0;
            }
            StartPlayerTurn();
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return Core.Instance.EnemyAIManager.ExecuteEnemyTurns();
        if (CheckForWaveClear()) yield break;
        EndTurn();
    }

    private bool CheckForWaveClear()
    {
        if (stageManager.GetEnemies().Count > 0) return false;

        // Wave is cleared. Check if it was the last wave.
        if (currentWave >= stageManager.GetCurrentStageData().waves.Length)
        {
            // LAST WAVE CLEARED - STAGE IS COMPLETE
            Debug.Log("모든 웨이브 클리어! 스테이지 완료!");
            SetPlayerState(PlayerTurnState.StageClear); // Set state to prevent further actions
            Core.Instance.GameManager.ProceedToNextLayer(); // Tell GM to create portals
        }
        else
        {
            // INTERMEDIATE WAVE CLEARED - START NEXT WAVE
            Debug.Log($"웨이브 {currentWave} 클리어! 다음 웨이브를 시작합니다.");
            currentWave++;
            stageManager.SpawnWave(currentWave);
        }
        return true; // Wave was cleared
    }
    
    public void SetPlayerState(PlayerTurnState newState)
    {
        CurrentPlayerState = newState;
        Debug.Log($"Player state changed to: {newState}");
    }

    // Dummy methods for compilation. Implement actual logic as needed.
    public void SelectPlayerUnit(PlayerUnit unit) { 
        // TODO: Implement selection logic
    }
    public void ClearSelection() { 
        // TODO: Implement deselection logic
    }
}
