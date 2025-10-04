using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class TurnManager : MonoBehaviour
{
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;

    public enum Turn { Player, Enemy }
    public Turn CurrentTurn { get; private set; }

    public enum PlayerTurnState { AwaitingUnitSelection, UnitSelected, PerformingAction, AwaitingSkillSubTarget }
    public PlayerTurnState CurrentPlayerState { get; private set; }

    public SkillBase PausedSkill { get; set; }
    public SkillData PausedSkillData { get; set; }
    public SkillContext PausedSkillContext { get; set; }

    public UIManager uiManager;
    public StageManager stageManager;

    public void StartPlayerTurn()
    {
        CurrentTurn = Turn.Player;
        CurrentPlayerState = PlayerTurnState.AwaitingUnitSelection;
        
        var player = stageManager.GetPlayer();
        if (player != null)
        {
            player.OnTurnStart();
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
            if (enemy != null)
            {
                enemy.OnTurnStart();
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
    
    // --- Logic moved from InputManager ---

    public void HandleCellClick(Vector2Int cell)
    {
        switch (CurrentPlayerState)
        {
            case PlayerTurnState.AwaitingUnitSelection:
                HandleUnitSelection(cell);
                break;

            case PlayerTurnState.UnitSelected:
                HandleActionSelection(cell);
                break;
            
            case PlayerTurnState.AwaitingSkillSubTarget:
                HandleSkillSubTargetSelection(cell);
                break;
        }
    }

    public void CancelSelection()
    {
        if (CurrentPlayerState == PlayerTurnState.UnitSelected || CurrentPlayerState == PlayerTurnState.AwaitingSkillSubTarget)
        {
            CancelActionState();
        }
    }

    private void HandleUnitSelection(Vector2Int cell)
    {
        GridManager.Instance.ClearAllHighlights();
        
        UnitBase unit = GridManager.Instance.GetUnitAt(cell);
        if (unit != null && unit is PlayerUnit)
        {
            GridManager.Instance.TrySelectUnitAtCell(cell);
            
            UnitBase selectedUnit = GridManager.Instance.GetSelectedUnit();
            if (selectedUnit != null)
            {
                SetPlayerState(PlayerTurnState.UnitSelected);
                selectedUnit.ShowAvailableActions();
            }
        }
        uiManager.UpdateSkillPanel();
    }

    private async void HandleActionSelection(Vector2Int cell)
    {
        DebugPrinter.DebugColor(DebugType.Input, $"HandleActionSelection: cell {cell} 클릭됨");
        UnitBase selectedUnit = GridManager.Instance.GetSelectedUnit();
        if (selectedUnit == null)
        {
            DebugPrinter.DebugColor(DebugType.Input, "HandleActionSelection: 선택된 유닛이 없어 종료됨");
            return;
        }
        DebugPrinter.DebugColor(DebugType.Input, $"HandleActionSelection: 현재 선택된 유닛 = {selectedUnit.name}");

        float skillDuration = 0f;

        // 1. Check for Attack Target
        UnitBase targetUnit = GridManager.Instance.GetTargetAt(cell);
        DebugPrinter.DebugColor(DebugType.Input, $"HandleActionSelection: GetTargetAt({cell}) 결과 = {(targetUnit != null ? targetUnit.name : "null")}");
        if (targetUnit != null)
        {
            // Find first attack skill and use it
            for (int i = 0; i < selectedUnit.unitData.skills.Length; i++)
            {
                if (selectedUnit.unitData.skills[i].skillType == SkillType.Attack)
                {
                    skillDuration = selectedUnit.UseSkill(i, cell);
                    await PostActionUpdate(selectedUnit, skillDuration);
                    return; // Action taken
                }
            }
            return; // No attack skill found
        }

        // 2. Check for Move
        if (GridManager.Instance.IsMovableTile(cell))
        {
            // Find the first move skill and use it
            for (int i = 0; i < selectedUnit.unitData.skills.Length; i++)
            {
                var skill = selectedUnit.unitData.skills[i];
                if (skill.skillType == SkillType.Move)
                {
                    skillDuration = selectedUnit.UseSkill(i, cell);
                    await PostActionUpdate(selectedUnit, skillDuration);
                    return; // Action taken
                }
            }
            return; // No move skill found
        }

        // 3. Check for clicking another friendly unit to switch selection
        UnitBase clickedUnit = GridManager.Instance.GetUnitAt(cell);
        if (clickedUnit != null && clickedUnit is PlayerUnit && clickedUnit != selectedUnit)
        {
            HandleUnitSelection(cell); // Reselect to the new unit
            return;
        }
        
        // 4. If nothing else, cancel selection
        CancelActionState();
    }

    private async void HandleSkillSubTargetSelection(Vector2Int cell)
    {
        var pausedSkillData = PausedSkillData;
        var context = PausedSkillContext;

        if (pausedSkillData == null || context == null) return;

        UnitBase clickedUnit = GridManager.Instance.GetUnitAt(cell);

        // TODO: Add validation to check if the clicked unit is a valid sub-target
        if (clickedUnit != null)
        {
            GridManager.Instance.ClearAllHighlights();
            context.SubTargetUnit = clickedUnit;
            await ExecuteSubSkills(pausedSkillData, context);
        }
        else
        {
            CancelActionState();
        }
    }

    private async UniTask ExecuteSubSkills(SkillData skillData, SkillContext context)
    {
        if (skillData.subTargetBehaviors != null)
        {
            foreach (var behavior in skillData.subTargetBehaviors)
            {
                if (behavior != null) 
                {
                    float duration = behavior.Execute(context);
                    await UniTask.Delay(TimeSpan.FromSeconds(duration));
                }
            }
        }

        // Clean up and reset state after all sub-skills are done
        PausedSkill = null;
        PausedSkillData = null;
        PausedSkillContext = null;
        SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        uiManager.UpdateSkillPanel();
    }

    private async UniTask PostActionUpdate(UnitBase unit, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));

        if (unit == null) return; // Unit might have died during the action

        if (unit.ap > 0)
        {
            unit.ShowAvailableActions();
        }
        else
        {
            GridManager.Instance.ClearSelection();
            SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
            uiManager.UpdateSkillPanel();
        }
    }

    private void CancelActionState()
    {
        DebugPrinter.DebugColor(DebugType.Input, "Selection cancelled.");
        GridManager.Instance.ClearAllHighlights();
        
        PausedSkill = null;
        PausedSkillData = null;
        PausedSkillContext = null;
        SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        uiManager.UpdateSkillPanel();
    }
}