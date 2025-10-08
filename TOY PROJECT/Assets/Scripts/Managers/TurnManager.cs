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

        PausedSkill = null;
        PausedCaster = null;

        if (gridManager != null)
        {
            gridManager.ClearSelection();
        }

        DebugPrinter.LogColor(LogType.Turn, "TurnManager state cleared.");
    }

    public async void StartFirstWave()
    {
        currentWave = 1;
        turnInWave = 0;
        await stageManager.SpawnWave(currentWave);
        StartPlayerTurn();
    }

    public async void StartPlayerTurn()
    {
        CurrentTurn = Turn.Player;
        turnInWave++;
        SetPlayerState(PlayerTurnState.AwaitingUnitSelection);

        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;

        await stageManager.SpawnEnemiesForTurn(currentWave, turnInWave);

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

    public async void EndTurn()
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
                DebugPrinter.LogColor(LogType.Turn, "방송 시간 초과! 패널티 라운드에 돌입합니다!");
                currentWave++;
                await stageManager.SpawnWave(currentWave);
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
            DebugPrinter.LogColor(LogType.Turn, "모든 웨이브 클리어! 스테이지 완료!");
            SetPlayerState(PlayerTurnState.StageClear);
            Core.Instance.GameManager.ProceedToNextLayer();
        }
        else
        {
            DebugPrinter.LogColor(LogType.Turn, $"웨이브 {currentWave} 클리어! 다음 웨이브를 시작합니다.");
            currentWave++;
            turnInWave = 0;
            await stageManager.SpawnWave(currentWave);
            ShowNextTurnEnemyPreview();
        }
    }

    private void ShowNextTurnEnemyPreview()
    {
        int nextTurnNumber = turnInWave + 1;
        var upcomingEnemies = stageManager.GetEnemiesSpawningOnTurn(currentWave, nextTurnNumber);

        if (upcomingEnemies == null || upcomingEnemies.Count == 0)
        {
            DebugPrinter.LogColor(LogType.Turn, $"다음 턴({nextTurnNumber})에 스폰될 적이 없습니다.");
            return;
        }

        DebugPrinter.LogColor(LogType.Turn, $"[미리보기] 다음 턴({nextTurnNumber})에 {upcomingEnemies.Count}마리의 적이 등장합니다!");

        foreach (var enemySpawnInfo in upcomingEnemies)
        {
            DebugPrinter.LogColor(LogType.Turn, $"  - {enemySpawnInfo.enemyType} at {enemySpawnInfo.spawnPos}");
        }
    }


    public void SetPlayerState(PlayerTurnState newState)
    {
        CurrentPlayerState = newState;
        DebugPrinter.LogColor(LogType.Turn, $"Player state changed to: {newState}");
    }

    // Dummy methods for compilation. Implement actual logic as needed.
    public void SelectPlayerUnit(PlayerUnit unit)
    {
        if (selectedUnit != null) ClearSelection();

        selectedUnit = unit;
        SetPlayerState(PlayerTurnState.UnitSelected);
        DebugPrinter.LogColor(LogType.Turn, $"플레이어 유닛 선택됨: {unit.name}");

        ShowAvailableActionsForUnit(unit);

        Core.Instance.UIManager.ShowUnitInfo(unit);
        uiManager.skillPanelUI?.UpdateSkillDisplay(unit);
    }

    public void RequestSkillUse(int skillIndex)
    {
        if (CurrentTurn != Turn.Player || CurrentPlayerState != PlayerTurnState.UnitSelected)
        {
            DebugPrinter.LogColor(LogType.Turn, "스킬을 사용할 수 없는 상태입니다.");
            return;
        }

        if (selectedUnit == null)
        {
            DebugPrinter.LogColor(LogType.Turn, "선택된 유닛이 없습니다.");
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
            DebugPrinter.LogColor(LogType.Turn, $"{skill.skillMeta.nameKey}는 쿨다운 중입니다: {selectedUnit.GetSkillCooldown(skillIndex)}턴 남음");
            return;
        }

        if (selectedUnit.ap < skill.apCost)
        {
            DebugPrinter.LogColor(LogType.Turn, $"{skill.skillMeta.nameKey}를 사용하기에 AP가 부족합니다. 필요: {skill.apCost}, 현재: {selectedUnit.ap}");
            return;
        }

        switch (skill.skillType)
        {
            case SkillType.Move:
                DebugPrinter.LogColor(LogType.Turn, "이동 스킬은 직접 타일을 클릭하세요.");
                break;

            case SkillType.Attack:
                StartSkillTargeting(skillIndex);
                break;

            default:
                DebugPrinter.LogColor(LogType.Turn, $"지원하지 않는 스킬 타입: {skill.skillType}");
                break;
        }
    }

    private void StartSkillTargeting(int skillIndex)
    {
        currentSkillIndex = skillIndex;
        var skill = selectedUnit.unitData.skills[skillIndex];

        DebugPrinter.LogColor(LogType.Turn, $"{skill.skillMeta.nameKey} 타겟 선택 모드 시작");

        var allUnits = gridManager.GetAllUnits();
        var targetableTiles = new List<Vector2Int>();

        foreach (var potentialTarget in allUnits)
        {
            if (potentialTarget.factionData == selectedUnit.factionData) continue;

            int distance = GridUtils.ChebyshevDistance(selectedUnit.position, potentialTarget.position);

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
        var skill = selectedUnit.unitData.skills[skillIndex];

        int distance = Mathf.Max(Mathf.Abs(selectedUnit.position.x - targetCell.x), Mathf.Abs(selectedUnit.position.y - targetCell.y));

        if (distance > skill.range)
        {
            DebugPrinter.LogColor(LogType.Turn, $"타겟이 사거리 밖입니다. 거리: {distance}, 사거리: {skill.range}");
            SetPlayerState(PlayerTurnState.UnitSelected);
            ShowAvailableActionsForUnit(selectedUnit);
            currentSkillIndex = -1;
            return;
        }

        SetPlayerState(PlayerTurnState.PerformingAction);

        selectedUnit.UseSkill(skillIndex, targetCell);

        if (skill.skillType == SkillType.Attack)
        {
            hasAttackedThisTurn = true;
        }

        OnPlayerActionEnd?.Invoke();

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
                    int distance = GridUtils.ChebyshevDistance(unit.position, potentialTarget.position);
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

        DebugPrinter.LogColor(LogType.Turn, "선택 취소");
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
                    DebugPrinter.LogColor(LogType.Turn, $"적 정보 표시: {enemyUnit.name}");
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
                    var skill = selectedUnit.unitData.skills[currentSkillIndex];

                    int distance = GridUtils.ChebyshevDistance(selectedUnit.position, cell);

                    if (distance <= skill.range)
                    {
                        ExecuteSkillOnTarget(currentSkillIndex, cell);
                    }
                    else
                    {
                        DebugPrinter.LogColor(LogType.Turn, $"타겟이 사거리 밖입니다. 거리: {distance}, 사거리: {skill.range}");
                        SetPlayerState(PlayerTurnState.UnitSelected);
                        ShowAvailableActionsForUnit(selectedUnit);
                    }
                }
                else
                {
                    DebugPrinter.LogColor(LogType.Turn, "유효하지 않은 타겟입니다.");
                    SetPlayerState(PlayerTurnState.UnitSelected);
                    ShowAvailableActionsForUnit(selectedUnit);
                }

                break;
        }
    }

    public bool CanAttackTarget(PlayerUnit unit, Vector2Int targetCell)
    {
        if (unit == null || hasAttackedThisTurn) return false;

        var skills = unit.GetSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (skill.data.skillType == SkillType.Attack)
            {
                if (skill.data.initialBehaviors.Length > 0 && skill.data.initialBehaviors[0].CanExecute(unit, targetCell, skill))
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
        var skills = unit.GetSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (skill.data.skillType == SkillType.Attack)
            {
                if (skill.data.initialBehaviors.Length > 0 && skill.data.initialBehaviors[0].CanExecute(unit, targetCell, skill))
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

            OnPlayerActionEnd?.Invoke();

            if (hasMovedThisTurn)
            {
                ClearSelection();
            }
            else
            {
                SetPlayerState(PlayerTurnState.UnitSelected);
                ;
                unit.Select();
            }
        }
    }

    private bool TryMoveUnit(PlayerUnit unit, Vector2Int targetCell)
    {
        if (hasMovedThisTurn) return false;

        int moveSkillIndex = unit.GetMoveSkillIndex();
        if (moveSkillIndex < 0) return false;

        var skills = unit.GetSkills();
        var moveSkill = skills[moveSkillIndex];

        if (moveSkill.data.initialBehaviors.Length == 0 || !moveSkill.data.initialBehaviors[0].CanExecute(unit, targetCell, moveSkill))
        {
            return false;
        }

        List<Vector2Int> walkableTiles = gridManager.GetWalkableTilesInRange(unit.position, moveSkill.GetRange());

        if (walkableTiles.Contains(targetCell))
        {
            MoveUnitToCell(unit, targetCell);
            return true;
        }

        return false;
    }

    private async void MoveUnitToCell(PlayerUnit unit, Vector2Int targetCell)
    {
        SetPlayerState(PlayerTurnState.PerformingAction);

        int moveSkillIndex = unit.GetMoveSkillIndex();
        if (moveSkillIndex < 0)
        {
            Debug.LogWarning("이동 스킬을 찾을 수 없습니다.");
            SetPlayerState(PlayerTurnState.UnitSelected);
            return;
        }

        float duration = unit.UseSkill(moveSkillIndex, targetCell);

        if (duration > 0)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(duration));
        }

        hasMovedThisTurn = true;
        OnPlayerActionEnd?.Invoke();

        if (hasAttackedThisTurn)
        {
            ClearSelection();
        }
        else
        {
            SetPlayerState(PlayerTurnState.UnitSelected);
            ShowAvailableActionsForUnit(unit);
        }
    }

    public void CancelSelection()
    {
        ClearSelection();
    }


    public async void FinalizeRewardSelection()
    {
        DebugPrinter.LogColor(LogType.Turn, "보상 선택이 완료되었습니다.");

        if (currentWave >= stageManager.GetCurrentStageData().waves.Length)
        {
            Core.Instance.GameManager.ProceedToNextLayer();
        }
        else
        {
            currentWave++;
            await stageManager.SpawnWave(currentWave);
            StartPlayerTurn();
        }
    }

    #region ACTION EVENT API

    public void TriggerUnitActionEnd()
    {
        OnPlayerActionEnd?.Invoke();
    }

    #endregion

    public bool HasUsableSkills()
    {
        if (selectedUnit == null) return false;

        return selectedUnit.GetSkills().Any(skill => skill.currentCooldown == 0 && selectedUnit.ap >= skill.GetAPCost());
    }
}
