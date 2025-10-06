using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public class TurnManager : MonoBehaviour
{
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;

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

        private bool hasMovedThisTurn = false;
    private bool hasAttackedThisTurn = false;
private PlayerUnit selectedUnit;
public PlayerUnit SelectedUnit => selectedUnit;
    private int currentSkillIndex = -1;


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
        StopAllCoroutines();

        CurrentTurn = Turn.Player;
        CurrentPlayerState = PlayerTurnState.AwaitingUnitSelection;
        
        selectedUnit = null;
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
        
        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;
        
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
public void SelectPlayerUnit(PlayerUnit unit)
    {
        if (selectedUnit != null) ClearSelection();
        
        selectedUnit = unit;
        SetPlayerState(PlayerTurnState.UnitSelected);
        Debug.Log($"플레이어 유닛 선택됨: {unit.name}");
        
        ShowAvailableActionsForUnit(unit);
        
        Core.Instance.UIManager.ShowUnitInfo(unit);
        uiManager.skillPanelUI?.UpdateSkillDisplay(unit);
    }

public void RequestSkillUse(int skillIndex)
    {
        if (CurrentTurn != Turn.Player || CurrentPlayerState != PlayerTurnState.UnitSelected)
        {
            Debug.Log("스킬을 사용할 수 없는 상태입니다.");
            return;
        }

        if (selectedUnit == null)
        {
            Debug.Log("선택된 유닛이 없습니다.");
            return;
        }

        if (skillIndex < 0 || skillIndex >= selectedUnit.unitData.skills.Length)
        {
            Debug.LogWarning($"잘못된 스킬 인덱스: {skillIndex}");
            return;
        }

        var skill = selectedUnit.unitData.skills[skillIndex];
        
        if (selectedUnit.GetSkillCooldown(skillIndex) > 0)
        {
            Debug.Log($"{skill.skillMeta.nameKey}는 쿨다운 중입니다: {selectedUnit.GetSkillCooldown(skillIndex)}턴 남음");
            return;
        }

        if (selectedUnit.ap < skill.apCost)
        {
            Debug.Log($"{skill.skillMeta.nameKey}를 사용하기에 AP가 부족합니다. 필요: {skill.apCost}, 현재: {selectedUnit.ap}");
            return;
        }

        switch (skill.skillType)
        {
            case SkillType.Move:
                Debug.Log("이동 스킬은 직접 타일을 클릭하세요.");
                break;
                
            case SkillType.Attack:
                StartSkillTargeting(skillIndex);
                break;
                
            default:
                Debug.Log($"지원하지 않는 스킬 타입: {skill.skillType}");
                break;
        }
    }

private void StartSkillTargeting(int skillIndex)
    {
        currentSkillIndex = skillIndex;
        var skill = selectedUnit.unitData.skills[skillIndex];
        
        Debug.Log($"{skill.skillMeta.nameKey} 타겟 선택 모드 시작");
        
        var allUnits = gridManager.GetAllUnits();
        var targetableTiles = new List<Vector2Int>();

        foreach (var potentialTarget in allUnits)
        {
            if (potentialTarget.factionData == selectedUnit.factionData) continue;
            
            int distance = Mathf.Max(
                Mathf.Abs(selectedUnit.position.x - potentialTarget.position.x), 
                Mathf.Abs(selectedUnit.position.y - potentialTarget.position.y)
            );
            
            if (distance <= skill.range)
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
        SetPlayerState(PlayerTurnState.PerformingAction);
        
        selectedUnit.UseSkill(skillIndex, targetCell);
        
        var skill = selectedUnit.unitData.skills[skillIndex];
        if (skill.skillType == SkillType.Attack)
        {
            hasAttackedThisTurn = true;
        }
        
        currentSkillIndex = -1;
        
        if (hasMovedThisTurn && hasAttackedThisTurn)
        {
            ClearSelection();
        }
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

        // 이동 가능한 타일 수집 (쿨타임 체크, 이동하지 않았을 때만)
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

        // 공격 가능한 타일 수집 (쿨타임 체크, 공격하지 않았을 때만)
        if (!hasAttackedThisTurn)
        {
            for (int i = 0; i < unit.unitData.skills.Length; i++)
            {
                var skill = unit.unitData.skills[i];
                if (skill.skillType != SkillType.Attack) continue;
                if (unit.GetSkillCooldown(i) > 0 || unit.ap < skill.apCost) continue;

                foreach (var potentialTarget in allUnits)
                {
                    if (potentialTarget.factionData == unit.factionData) continue;
                    // 체비쇼프 거리(대각선 포함 8방향)로 계산 방식 변경
                    int distance = Mathf.Max(Mathf.Abs(unit.position.x - potentialTarget.position.x), Mathf.Abs(unit.position.y - potentialTarget.position.y));
                    if (distance <= skill.range)
                    {
                        Vector2Int targetPos = potentialTarget.position;
                        if (!attackableTiles.Contains(targetPos))
                        {
                            attackableTiles.Add(targetPos);
                        }
                    }
                }
            }
        }

        // 우선순위: 공격 가능한 타일이 이동 가능한 타일과 겹치면 공격이 우선
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
        
        Debug.Log("선택 취소");
    }


public void HandleCellClick(Vector2Int cell)
    {
        if (CurrentTurn != Turn.Player) return;

        var unitAtCell = gridManager.GetUnitAt(cell);

        switch (CurrentPlayerState)
        {
            case PlayerTurnState.AwaitingUnitSelection:
                if (unitAtCell is PlayerUnit playerUnit)
                {
                    SelectPlayerUnit(playerUnit);
                }
                else if (unitAtCell is EnemyUnit enemyUnit)
                {
                    gridManager.ClearAllHighlights();
                    Core.Instance.UIManager.ShowUnitInfo(enemyUnit);
                    Debug.Log($"적 정보 표시: {enemyUnit.name}");
                }
                else
                {
                    gridManager.ClearAllHighlights();
                    ClearSelection();
                }
                break;

            case PlayerTurnState.UnitSelected:
                if (selectedUnit == null)
                {
                    SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
                    return;
                }

                if (unitAtCell == null)
                {
                    gridManager.ClearAllHighlights();
                    if (!TryMoveUnit(selectedUnit, cell))
                    {
                        ClearSelection();
                    }
                }
                else if (unitAtCell is PlayerUnit _playerUnit)
                {
                    gridManager.ClearAllHighlights();
                    SelectPlayerUnit(_playerUnit);
                }
                else if (unitAtCell.factionData != selectedUnit.factionData)
                {
                    gridManager.ClearAllHighlights();
                    TryAttackUnit(selectedUnit, cell);
                }
                break;

            case PlayerTurnState.AwaitingSkillSubTarget:
                if (selectedUnit == null || currentSkillIndex < 0)
                {
                    SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
                    return;
                }
                
                gridManager.ClearAllHighlights();
                
                if (unitAtCell != null && unitAtCell.factionData != selectedUnit.factionData)
                {
                    ExecuteSkillOnTarget(currentSkillIndex, cell);
                }
                else
                {
                    Debug.Log("유효하지 않은 타겟입니다.");
                    SetPlayerState(PlayerTurnState.UnitSelected);
                    ShowAvailableActionsForUnit(selectedUnit);
                }
                break;
        }
    }
public bool CanAttackTarget(PlayerUnit unit, Vector2Int targetCell)
    {
        if (unit == null || hasAttackedThisTurn) return false;

        for (int i = 0; i < unit.unitData.skills.Length; i++)
        {
            var skill = unit.unitData.skills[i];
            if (skill.skillType == SkillType.Attack 
                && unit.GetSkillCooldown(i) == 0 
                && unit.ap >= skill.apCost)
            {
                int distance = Mathf.Max(Mathf.Abs(unit.position.x - targetCell.x), Mathf.Abs(unit.position.y - targetCell.y));
                if (distance <= skill.range)
                {
                    return true;
                }
            }
        }
        return false;
    }

    
private void TryAttackUnit(PlayerUnit unit, Vector2Int targetCell)
    {
        if (hasAttackedThisTurn) return;

        int attackSkillIndex = -1;
        for (int i = 0; i < unit.unitData.skills.Length; i++)
        {
            var skill = unit.unitData.skills[i];
            if (skill.skillType == SkillType.Attack 
                && unit.GetSkillCooldown(i) == 0 
                && unit.ap >= skill.apCost)
            {
                // 체비쇼프 거리(대각선 포함 8방향)로 계산 방식 변경
                int distance = Mathf.Max(Mathf.Abs(unit.position.x - targetCell.x), Mathf.Abs(unit.position.y - targetCell.y));
                if (distance <= skill.range)
                {
                    attackSkillIndex = i;
                    break;
                }
            }
        }

        if (attackSkillIndex >= 0)
        {
            SetPlayerState(PlayerTurnState.PerformingAction);
            unit.UseSkill(attackSkillIndex, targetCell);
            hasAttackedThisTurn = true;
            
            if (hasMovedThisTurn)
            {
                ClearSelection();
            }
            else
            {
                SetPlayerState(PlayerTurnState.UnitSelected);
                ShowAvailableActionsForUnit(unit);
            }
        }
    }

private bool TryMoveUnit(PlayerUnit unit, Vector2Int targetCell)
    {
        Debug.Log($"TryMoveUnit 호출: {targetCell}, hasMovedThisTurn={hasMovedThisTurn}");
        
        if (hasMovedThisTurn)
        {
            Debug.Log("이미 이동했습니다.");
            return false;
        }

        int moveSkillIndex = unit.GetMoveSkillIndex();
        if (moveSkillIndex < 0)
        {
            Debug.Log("이동 스킬을 찾을 수 없습니다.");
            return false;
        }

        var moveSkill = unit.unitData.skills[moveSkillIndex];
        if (unit.GetSkillCooldown(moveSkillIndex) > 0 || unit.ap < moveSkill.apCost)
        {
            Debug.Log($"이동 불가: 쿨다운={unit.GetSkillCooldown(moveSkillIndex)}, AP={unit.ap}/{moveSkill.apCost}");
            return false;
        }

        List<Vector2Int> walkableTiles = gridManager.GetWalkableTilesInRange(unit.position, moveSkill.range);
        Debug.Log($"이동 가능한 타일 개수: {walkableTiles.Count}");
        foreach (var tile in walkableTiles)
        {
            Debug.Log($"  - {tile}");
        }

        if (walkableTiles.Contains(targetCell))
        {
            Debug.Log($"{targetCell}로 이동 시도");
            MoveUnitToCell(unit, targetCell);
            return true;
        }

        Debug.Log($"{targetCell}은 이동 가능한 타일이 아닙니다.");
        return false;
    }



    private void MoveUnitToCell(PlayerUnit unit, Vector2Int targetCell)
    {
        SetPlayerState(PlayerTurnState.PerformingAction);

        int moveSkillIndex = unit.GetMoveSkillIndex();
        if (moveSkillIndex < 0)
        {
            Debug.LogWarning("이동 스킬을 찾을 수 없습니다.");
            SetPlayerState(PlayerTurnState.UnitSelected);
            return;
        }

        // 이동 스킬 사용 처리 (AP 소모 및 쿨다운 설정)
        unit.UseSkill(moveSkillIndex, targetCell);

        List<Vector2Int> path = gridManager.FindPath(unit.position, targetCell);
        if (path != null && path.Count > 0)
        {
            unit.MoveAlongPath(path, () => {
                hasMovedThisTurn = true;

                if (hasAttackedThisTurn)
                {
                    ClearSelection();
                }
                else
                {
                    SetPlayerState(PlayerTurnState.UnitSelected);
                    ShowAvailableActionsForUnit(unit);
                }
            });
        }
        else
        {
            Debug.LogWarning("이동 경로를 찾을 수 없습니다.");
            // AP와 쿨다운을 원래대로 되돌리는 로직이 필요할 수 있습니다.
            // 현재는 간단하게 상태만 변경합니다.
            SetPlayerState(PlayerTurnState.UnitSelected);
        }
    }

    public void CancelSelection()
    {
        ClearSelection();
    }


    public void FinalizeRewardSelection()
    {
        Debug.Log("보상 선택이 완료되었습니다.");
        
        if (currentWave >= stageManager.GetCurrentStageData().waves.Length)
        {
            Core.Instance.GameManager.ProceedToNextLayer();
        }
        else
        {
            currentWave++;
            stageManager.SpawnWave(currentWave);
            StartPlayerTurn();
        }
    }
}
