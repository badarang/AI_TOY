using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public class TurnManager : MonoBehaviour, IManager
{
    public void BeforeInit()
    {
    }

    public void AfterInit()
    {
        uiManager = Core.Instance.UIManager;
        stageManager = Core.Instance.StageManager;
        gridManager = Core.Instance.GridManager;
    }

    public event Action OnPlayerTurnStart;
    public event Action OnPlayerActionEnd; // 플레이어가 턴 안에서 무언가 액션을 했을 때
    public event Action OnEnemyTurnStart;

    public enum Turn
    {
        Player,
        Enemy
    }

    public Turn CurrentTurn { get; private set; }

    public enum PlayerTurnState
    {
        AwaitingUnitSelection,
        UnitSelected,
        PerformingAction,
        AwaitingSkillSubTarget,
        StageClear
    }

    public PlayerTurnState CurrentPlayerState { get; private set; }

    // Wave & Turn Limit System
    private int currentWave = 1;
    private int turnInWave = 0;

    // Paused Skill State
    public Skill PausedSkill { get; set; }
    public UnitBase PausedCaster { get; set; }

    // Manager References
    private UIManager uiManager;
    private StageManager stageManager;
    private GridManager gridManager;

    private bool hasMovedThisTurn = false;
    private bool hasAttackedThisTurn = false;
    private PlayerUnit selectedUnit;
    public PlayerUnit SelectedUnit => selectedUnit;
    private int currentSkillIndex = -1;

    public void ClearTurn()
    {
        StopAllCoroutines();
        CurrentTurn = Turn.Player;
        CurrentPlayerState = PlayerTurnState.AwaitingUnitSelection;
        selectedUnit = null;
        currentWave = 1;
        turnInWave = 0;
        PausedSkill = null;
        PausedCaster = null;
        if (gridManager != null) gridManager.ClearSelection();
        Debug.Log("TurnManager state cleared.");
    }

    public void StartFirstWave()
    {
        currentWave = 1;
        turnInWave = 0;
        StartPlayerTurn();
    }

    public async void StartPlayerTurn()
    {
        CurrentTurn = Turn.Player;
        turnInWave++;
        SetPlayerState(PlayerTurnState.AwaitingUnitSelection);

        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;

        await stageManager.SpawnEnemiesForTurn(turnInWave);

        var player = stageManager.GetPlayer();
        if (player != null) player.OnTurnStart();

        OnPlayerTurnStart?.Invoke();

        int turnLimit = stageManager.GetCurrentStageData()?.waves[currentWave - 1].clearTurnLimit ?? 5;
        uiManager.UpdateTurnUI(turnInWave, turnLimit);

        ShowNextTurnEnemyPreview();
    }

    public void StartEnemyTurn()
    {
        CurrentTurn = Turn.Enemy;
        SetPlayerState(PlayerTurnState.PerformingAction);

        var enemies = stageManager.GetEnemies();
        foreach (var enemy in enemies)
            if (enemy != null)
                enemy.OnTurnStart();

        OnEnemyTurnStart?.Invoke();
        StartCoroutine(EnemyTurnRoutine());
    }

    public void EndTurn()
    {
        if (CurrentPlayerState == PlayerTurnState.StageClear) return;

        if (CurrentTurn == Turn.Player)
        {
            if (HasUsableSkills())
            {
                uiManager.ShowEndTurnConfirmPopup(() => { StartEnemyTurn(); });
                return;
            }
            StartEnemyTurn();
        }
        else
        {
            int turnLimit = stageManager.GetCurrentStageData()?.waves[currentWave - 1].clearTurnLimit ?? 5;
            if (turnInWave >= turnLimit)
            {
                Debug.Log("Turn limit reached! Advancing to penalty wave.");
                stageManager.AdvanceToNextWave(); // Server prepares next wave data
                currentWave++;
                turnInWave = 0;
            }
            StartPlayerTurn();
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return Core.Instance.EnemyAIManager.ExecuteEnemyTurns();
        yield return CheckForWaveClearAsync().ToCoroutine();
    }

    private async UniTask CheckForWaveClearAsync()
    {
        if (stageManager.GetEnemies().Count > 0)
        {
            EndTurn();
            return;
        }

        if (currentWave >= stageManager.GetCurrentStageData().waves.Length)
        {
            Debug.Log("All waves cleared! Stage complete!");
            SetPlayerState(PlayerTurnState.StageClear);
            Core.Instance.GameManager.ProceedToNextLayer();
        }
        else
        {
            Debug.Log($"Wave {currentWave} cleared! Starting next wave.");
            stageManager.AdvanceToNextWave(); // Server prepares next wave data
            currentWave++;
            turnInWave = 0;
            StartPlayerTurn();
        }
    }

    private void ShowNextTurnEnemyPreview()
    {
        int nextTurnNumber = turnInWave + 1;
        var upcomingEnemies = stageManager.GetEnemySpawnsForTurn(nextTurnNumber);

        if (upcomingEnemies == null || upcomingEnemies.Count == 0)
        {
            Debug.Log($"No enemies spawning on next turn ({nextTurnNumber}).");
            return;
        }

        Debug.Log($"[Preview] {upcomingEnemies.Count} enemies will spawn on turn {nextTurnNumber}!");

        foreach (var enemySpawnInfo in upcomingEnemies)
        {
            Debug.Log($"  - {(UnitType)enemySpawnInfo.enemyTypeIndex} at {enemySpawnInfo.spawnPos}");
        }
    }

    public void SetPlayerState(PlayerTurnState newState)
    {
        CurrentPlayerState = newState;
        Debug.Log($"Player state changed to: {newState}");
    }

    public void SelectPlayerUnit(PlayerUnit unit)
    {
        if (selectedUnit != null) ClearSelection();
        selectedUnit = unit;
        SetPlayerState(PlayerTurnState.UnitSelected);
        Debug.Log($"Player unit selected: {unit.name}");
        ShowAvailableActionsForUnit(unit);
        Core.Instance.UIManager.ShowUnitInfo(unit);
        uiManager.skillPanelUI?.UpdateSkillDisplay(unit);
    }

    public void RequestSkillUse(int skillIndex)
    {
        if (CurrentTurn != Turn.Player || CurrentPlayerState != PlayerTurnState.UnitSelected) return;
        if (selectedUnit == null) return;
        if (skillIndex < 0 || skillIndex >= selectedUnit.unitData.skills.Length) return;

        var skill = selectedUnit.unitData.skills[skillIndex];
        if (selectedUnit.GetSkillCooldown(skillIndex) > 0) return;
        if (selectedUnit.ap < skill.apCost) return;

        if (skill.skillType == SkillType.Attack) StartSkillTargeting(skillIndex);
    }

    private void StartSkillTargeting(int skillIndex)
    {
        currentSkillIndex = skillIndex;
        var skill = selectedUnit.unitData.skills[skillIndex];
        var targetableTiles = new List<Vector2Int>();

        foreach (var potentialTarget in gridManager.GetAllUnits())
        {
            if (potentialTarget.factionData == selectedUnit.factionData) continue;
            if (GridUtils.ChebyshevDistance(selectedUnit.position, potentialTarget.position) <= skill.range)
            {
                targetableTiles.Add(potentialTarget.position);
            }
        }

        gridManager.ClearAllHighlights();
        gridManager.HighlightAttackableTiles(targetableTiles);
        SetPlayerState(PlayerTurnState.AwaitingSkillSubTarget);
    }

    private void ExecuteSkillOnTarget(int skillIndex, Vector2Int targetCell)
    {
        var skill = selectedUnit.unitData.skills[skillIndex];
        if (GridUtils.ChebyshevDistance(selectedUnit.position, targetCell) > skill.range)
        {
            SetPlayerState(PlayerTurnState.UnitSelected);
            ShowAvailableActionsForUnit(selectedUnit);
            currentSkillIndex = -1;
            return;
        }

        SetPlayerState(PlayerTurnState.PerformingAction);
        selectedUnit.UseSkill(skillIndex, targetCell);
        if (skill.skillType == SkillType.Attack) hasAttackedThisTurn = true;
        OnPlayerActionEnd?.Invoke();
        currentSkillIndex = -1;

        if (hasMovedThisTurn && hasAttackedThisTurn) ClearSelection();
        else
        {
            SetPlayerState(PlayerTurnState.UnitSelected);
            ShowAvailableActionsForUnit(selectedUnit);
        }
    }

    private void ShowAvailableActionsForUnit(PlayerUnit unit)
    {
        var allUnits = gridManager.GetAllUnits();
        var attackableTiles = new List<Vector2Int>();
        var movableTiles = new List<Vector2Int>();

        if (!hasMovedThisTurn)
        {
            int moveSkillIndex = unit.GetMoveSkillIndex();
            if (moveSkillIndex >= 0)
            {
                var moveSkill = unit.unitData.skills[moveSkillIndex];
                if (unit.GetSkillCooldown(moveSkillIndex) == 0 && unit.ap >= moveSkill.apCost)
                {
                    movableTiles = gridManager.GetWalkableTilesInRange(unit.position, moveSkill.range);
                }
            }
        }

        if (!hasAttackedThisTurn)
        {
            for (int i = 0; i < unit.unitData.skills.Length; i++)
            {
                var skill = unit.unitData.skills[i];
                if (skill.skillType != SkillType.Attack || unit.GetSkillCooldown(i) > 0 || unit.ap < skill.apCost) continue;

                foreach (var potentialTarget in allUnits)
                {
                    if (potentialTarget.factionData != unit.factionData && GridUtils.ChebyshevDistance(unit.position, potentialTarget.position) <= skill.range)
                    {
                        if (!attackableTiles.Contains(potentialTarget.position)) attackableTiles.Add(potentialTarget.position);
                    }
                }
            }
        }

        var pureMovableTiles = movableTiles.Where(tile => !attackableTiles.Contains(tile)).ToList();
        gridManager.HighlightMovableTiles(pureMovableTiles);
        gridManager.HighlightAttackableTiles(attackableTiles);
    }

    public void ClearSelection()
    {
        if (CurrentPlayerState == PlayerTurnState.StageClear) return;
        selectedUnit = null;
        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;
        SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        gridManager.ClearSelection();
        Core.Instance.UIManager.HideUnitInfo();
        Core.Instance.UIManager.HideSkillPanel();
    }

    public void HandleCellClick(Vector2Int cell)
    {
        if (CurrentTurn != Turn.Player) return;
        var unitAtCell = gridManager.GetUnitAt(cell);

        switch (CurrentPlayerState)
        {
            case PlayerTurnState.AwaitingUnitSelection:
                if (unitAtCell is PlayerUnit playerUnit) SelectPlayerUnit(playerUnit);
                else if (unitAtCell is EnemyUnit enemyUnit)
                {
                    gridManager.ClearAllHighlights();
                    Core.Instance.UIManager.ShowUnitInfo(enemyUnit);
                }
                else ClearSelection();
                break;

            case PlayerTurnState.UnitSelected:
                if (selectedUnit == null) { SetPlayerState(PlayerTurnState.AwaitingUnitSelection); return; }
                gridManager.ClearAllHighlights();
                if (unitAtCell == null) { if (!TryMoveUnit(selectedUnit, cell)) ClearSelection(); }
                else if (unitAtCell is PlayerUnit p) SelectPlayerUnit(p);
                else if (unitAtCell.factionData != selectedUnit.factionData) TryAttackUnit(selectedUnit, cell);
                break;

            case PlayerTurnState.AwaitingSkillSubTarget:
                if (selectedUnit == null || currentSkillIndex < 0) { SetPlayerState(PlayerTurnState.AwaitingUnitSelection); return; }
                gridManager.ClearAllHighlights();
                if (unitAtCell != null && unitAtCell.factionData != selectedUnit.factionData) ExecuteSkillOnTarget(currentSkillIndex, cell);
                else
                {
                    SetPlayerState(PlayerTurnState.UnitSelected);
                    ShowAvailableActionsForUnit(selectedUnit);
                }
                break;
        }
    }

    private void TryAttackUnit(PlayerUnit unit, Vector2Int targetCell)
    {
        if (hasAttackedThisTurn) return;
        int attackSkillIndex = -1;
        for (int i = 0; i < unit.unitData.skills.Length; i++)
        {
            var skill = unit.unitData.skills[i];
            if (skill.skillType == SkillType.Attack && GridUtils.ChebyshevDistance(unit.position, targetCell) <= skill.range)
            {
                attackSkillIndex = i;
                break;
            }
        }

        if (attackSkillIndex >= 0)
        {
            SetPlayerState(PlayerTurnState.PerformingAction);
            unit.UseSkill(attackSkillIndex, targetCell);
            hasAttackedThisTurn = true;
            OnPlayerActionEnd?.Invoke();
            if (hasMovedThisTurn) ClearSelection();
            else
            {
                SetPlayerState(PlayerTurnState.UnitSelected);
                ShowAvailableActionsForUnit(unit);
            }
        }
    }

    private bool TryMoveUnit(PlayerUnit unit, Vector2Int targetCell)
    {
        if (hasMovedThisTurn) return false;
        int moveSkillIndex = unit.GetMoveSkillIndex();
        if (moveSkillIndex < 0) return false;

        var moveSkill = unit.unitData.skills[moveSkillIndex];
        if (!gridManager.GetWalkableTilesInRange(unit.position, moveSkill.range).Contains(targetCell)) return false;

        MoveUnitToCell(unit, targetCell);
        return true;
    }

    private async void MoveUnitToCell(PlayerUnit unit, Vector2Int targetCell)
    {
        SetPlayerState(PlayerTurnState.PerformingAction);
        int moveSkillIndex = unit.GetMoveSkillIndex();
        if (moveSkillIndex < 0) { SetPlayerState(PlayerTurnState.UnitSelected); return; }

        float duration = unit.UseSkill(moveSkillIndex, targetCell);
        if (duration > 0) await UniTask.Delay(TimeSpan.FromSeconds(duration));

        hasMovedThisTurn = true;
        OnPlayerActionEnd?.Invoke();

        if (hasAttackedThisTurn) ClearSelection();
        else
        {
            SetPlayerState(PlayerTurnState.UnitSelected);
            ShowAvailableActionsForUnit(unit);
        }
    }

    public void CancelSelection() => ClearSelection();

    public void FinalizeRewardSelection()
    {
        Debug.Log("Reward selection finalized.");
        if (currentWave >= stageManager.GetCurrentStageData().waves.Length)
        {
            Core.Instance.GameManager.ProceedToNextLayer();
        }
        else
        {
            stageManager.AdvanceToNextWave();
            currentWave++;
            StartPlayerTurn();
        }
    }

    public void TriggerUnitActionEnd() => OnPlayerActionEnd?.Invoke();

    public bool CanAttackTarget(PlayerUnit unit, Vector2Int targetCell)
    {
        if (unit == null || hasAttackedThisTurn) return false;

        var targetUnit = gridManager.GetUnitAt(targetCell);
        if (targetUnit == null || targetUnit.factionData == unit.factionData)
        {
            return false;
        }

        for (int i = 0; i < unit.unitData.skills.Length; i++)
        {
            var skill = unit.unitData.skills[i];
            if (skill.skillType == SkillType.Attack)
            {
                if (unit.GetSkillCooldown(i) > 0 || unit.ap < skill.apCost)
                {
                    continue;
                }

                if (GridUtils.ChebyshevDistance(unit.position, targetCell) <= skill.range)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool HasUsableSkills()
    {
        var player = stageManager.GetPlayer();
        if (player == null) return false;
        for (int i = 0; i < player.unitData.skills.Length; i++)
        {
            var skill = player.unitData.skills[i];
            if (player.GetSkillCooldown(i) == 0 && player.ap >= skill.apCost)
            {
                return true;
            }
        }
        return false;
    }
}
