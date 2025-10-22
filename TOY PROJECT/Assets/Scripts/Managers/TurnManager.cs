using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

// TurnManager is now a NetworkBehaviour to manage and sync turn state.
public class TurnManager : NetworkBehaviour, IManager
{
    // --- NETWORKED STATE ---
    [Networked]
    public PlayerRef CurrentTurnPlayer { get; private set; }

    [Networked]
    public int TurnNumber { get; private set; }

    [Networked]
    public NetworkBool IsPlayerTurn { get; set; }

    // --- SERVER-ONLY STATE ---
    private List<PlayerRef> _playerTurnOrder = new List<PlayerRef>();
    private int _turnIndex = -1;

    // --- LOCAL/CLIENT STATE ---
    public enum PlayerTurnState
    {
        AwaitingTurn, // Not this player's turn
        AwaitingUnitSelection,
        UnitSelected,
        PerformingAction,
        StageClear,
    }

    public PlayerTurnState CurrentPlayerState { get; private set; }

    // --- EVENTS ---
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;
    public event Action OnPlayerSkillEnd;

    // --- MANAGER REFS ---
    private UIManager uiManager;
    private bool _isMyTurn = false;
    public bool IsMyTurn => _isMyTurn;

    private StageManager stageManager;
    private UnitManager unitManager;

    private GridManager gridManager;

    private PlayerUnit _localSelectedUnit;
    public PlayerUnit SelectedUnit => _localSelectedUnit;

    private bool _battleEnded = false;

    public bool BattleEnded => _battleEnded;

    #region IManager & Lifecycle

    public void BeforeInit() { }

    public void AfterInit()
    {
        uiManager = Core.Instance.UIManager;
        stageManager = Core.Instance.StageManager;
        gridManager = Core.Instance.GridManager;
        unitManager = Core.Instance.UnitManager;
    }

    public void Dispose() { }

    public override void Spawned()
    {
        if (Core.Instance != null)
        {
            AfterInit();
        }
    }

    public override void FixedUpdateNetwork()
    {
        bool wasMyTurn = _isMyTurn;
        _isMyTurn = (CurrentTurnPlayer == Runner.LocalPlayer);

        if (_isMyTurn && !wasMyTurn)
        {
            Debug.Log("It is now my turn!");
            SetPlayerState(PlayerTurnState.AwaitingUnitSelection);

            var myUnits = unitManager.GetAllPlayers().Where(p => p.Owner == Runner.LocalPlayer);
            foreach (var unit in myUnits)
            {
                unit.OnTurnStart();
            }
            OnPlayerTurnStart?.Invoke();

            if (uiManager != null)
            {
                uiManager.SetEndTurnButtonActive(true);
            }
        }
        else if (!_isMyTurn && wasMyTurn)
        {
            Debug.Log($"It is now Player {CurrentTurnPlayer}'s turn. I am awaiting.");
            SetPlayerState(PlayerTurnState.AwaitingTurn);
            ClearSelection();

            if (uiManager != null)
            {
                uiManager.SetEndTurnButtonActive(false);
            }
        }
    }

    #endregion

    #region Turn Flow Control (Server-Only)

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void EndTurnRpc()
    {
        Debug.Log($"Server received EndTurn RPC from {CurrentTurnPlayer}.");
        StartNextTurn().Forget();
    }

    public async UniTask StartCombat()
    {
        if (!HasStateAuthority)
            return;

        TurnNumber = 1; // Start from turn 1

        _playerTurnOrder.Clear();
        var allPlayers = unitManager.GetAllPlayers();
        if (allPlayers.Count == 0)
        {
            Debug.LogError("[TurnManager] No players found! Cannot start combat.");
            return;
        }

        var sortedPlayers = allPlayers.OrderBy(p => p.Owner.PlayerId).ToList();
        foreach (var player in sortedPlayers)
        {
            if (!_playerTurnOrder.Contains(player.Owner))
                _playerTurnOrder.Add(player.Owner);
        }

        Debug.Log($"[TurnManager] Starting combat with {_playerTurnOrder.Count} players.");
        _turnIndex = -1;
        StartNextTurn().Forget();
    }

    private async UniTask StartNextTurn()
    {
        if (!HasStateAuthority)
            return;

        _turnIndex++;

        if (_turnIndex < _playerTurnOrder.Count)
        {
            IsPlayerTurn = true;
            CurrentTurnPlayer = _playerTurnOrder[_turnIndex];
            Debug.Log(
                $"[SERVER] Starting turn for Player {CurrentTurnPlayer}. Index: {_turnIndex}"
            );
            UpdateTurnOrderDisplay();

            await UniTask.NextFrame();
        }
        else
        {
            IsPlayerTurn = false;
            CurrentTurnPlayer = PlayerRef.None;
            Debug.Log("[SERVER] All players have acted. Starting enemy turn.");
            UpdateTurnOrderDisplay();
            await StartEnemyTurn();

            _turnIndex = -1;
            TurnNumber++;

            await Core.Instance.StageManager.IncrementTurn();

            StartNextTurn().Forget();
        }
    }

    private async UniTask StartEnemyTurn()
    {
        var players = unitManager != null ? unitManager.GetAllPlayers() : new List<PlayerUnit>();
        if (players.Count == 0 || players[0].hp <= 0)
        {
            Debug.Log("[TurnManager] All players dead, ending battle.");
            return;
        }

        var enemies = unitManager != null ? unitManager.GetEnemies() : new List<EnemyUnit>();
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.OnTurnStart();
        }

        OnEnemyTurnStart?.Invoke();

        if (Core.Instance.EnemyAIManager != null)
            await Core.Instance.EnemyAIManager.ExecuteEnemyTurns();

        await CheckForWaveClearAsync();
    }

    private async UniTask CheckForWaveClearAsync()
    {
        await UniTask.Delay(500);

        if ((unitManager != null ? unitManager.GetEnemies().Count : 0) == 0)
        {
            Debug.Log("[TurnManager] All enemies cleared! Wave complete!");

            if (HasStateAuthority)
            {
                Core.Instance.StageManager.OnWaveComplete();
            }
        }
    }

    #endregion

    #region Input & Actions (Client-Side)

    public void RequestEndTurn()
    {
        if (CurrentTurnPlayer == Runner.LocalPlayer)
        {
            EndTurnRpc();
        }
        else
        {
            Debug.LogWarning("Cannot end turn when it's not my turn.");
        }
    }

    public void HandleCellClick(Vector2Int cell)
    {
        // 그리드 범위 체크 - 범위 밖이면 선택 해제
        if (!gridManager.IsValidTile(cell))
        {
            Debug.Log($"[TurnManager] Clicked outside grid bounds: {cell}");
            ClearSelection();
            return;
        }

        var unitAtCell = gridManager.GetUnitAt(cell);

        // 항상 유닛을 클릭하면 정보를 표시 (클라이언트 단 처리)
        if (unitAtCell != null)
        {
            uiManager.ShowUnitInfo(unitAtCell);
        }
        else
        {
            uiManager.HideUnitInfo();
        }



        // 내 턴이 아니면 여기서 종료 (정보 표시만 하고 다른 행동 불가)
        if (CurrentTurnPlayer != Runner.LocalPlayer)
        {
            return;
        }

        switch (CurrentPlayerState)
        {
            case PlayerTurnState.AwaitingUnitSelection:
                if (unitAtCell is PlayerUnit playerUnit && playerUnit.Owner == Runner.LocalPlayer)
                {
                    SelectPlayerUnit(playerUnit);
                }
                else if (unitAtCell == null)
                {
                    ClearSelection();
                }
                break;

            case PlayerTurnState.UnitSelected:
                if (_localSelectedUnit == null)
                {
                    SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
                    return;
                }

                if (unitAtCell == null)
                {
                    TryMoveUnit(_localSelectedUnit, cell);
                }
                else if (
                    unitAtCell is PlayerUnit friendlyUnit
                    && friendlyUnit.Owner == Runner.LocalPlayer
                )
                {
                    // 다른 아군 유닛 선택 - 이전 선택 완전히 해제 후 새로 선택
                    SelectPlayerUnit(friendlyUnit);
                }
                else if (unitAtCell is EnemyUnit)
                {
                    // 적 클릭 - 공격 시도
                    TryAttackUnit(_localSelectedUnit, cell);
                    // 적 Info는 위에서 이미 표시됨 - 선택 해제하지 않음
                }
                break;
        }
    }

    private void TryMoveUnit(PlayerUnit unit, Vector2Int targetCell)
    {
        int moveSkillIndex = unit.GetMoveSkillIndex();
        if (moveSkillIndex < 0)
        {
            Debug.LogWarning("[TurnManager] No move skill found");
            ClearSelection();
            return;
        }

        // 스킬 사용 가능 여부를 먼저 확인
        var skills = unit.GetSkills();
        if (moveSkillIndex >= skills.Count)
        {
            Debug.LogWarning("[TurnManager] Invalid move skill index");
            ClearSelection();
            return;
        }

        var moveSkill = skills[moveSkillIndex];
        if (!moveSkill.CanExecute(unit, targetCell))
        {
            Debug.LogWarning(
                $"[TurnManager] Cannot move to {targetCell}. Out of range or invalid tile."
            );
            // 빈 격자를 클릭했는데 이동 실패 시 선택 해제
            ClearSelection();
            return;
        }

        // 스킬 사용 가능하면 요청 및 상태 변경
        unit.RequestSkillUse(moveSkillIndex, targetCell);
        ClearSelection();
        SetPlayerState(PlayerTurnState.PerformingAction);
    }

    private void TryAttackUnit(PlayerUnit unit, Vector2Int targetCell)
    {
        int attackSkillIndex = -1;
        var skills = unit.GetSkills();

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].data.skillType == SkillType.Attack)
            {
                attackSkillIndex = i;
                break;
            }
        }

        if (attackSkillIndex < 0)
        {
            Debug.LogWarning("[TurnManager] No attack skill found");
            ClearSelection();
            return;
        }

        // 스킬 사용 가능 여부를 먼저 확인
        var attackSkill = skills[attackSkillIndex];
        if (!attackSkill.CanExecute(unit, targetCell))
        {
            Debug.LogWarning(
                $"[TurnManager] Cannot attack {targetCell}. Out of range or invalid target."
            );
            // 적이 사거리 밖일 때: 선택 해제 후 적 Info 다시 표시
            var target = gridManager.GetUnitAt(targetCell);
            ClearSelection();
            if (target != null)
            {
                uiManager.ShowUnitInfo(target);
            }
            return;
        }

        // 스킬 사용 가능하면 요청 및 상태 변경
        unit.RequestSkillUse(attackSkillIndex, targetCell);
        ClearSelection();
        SetPlayerState(PlayerTurnState.PerformingAction);
    }

    public void SelectPlayerUnit(PlayerUnit unit)
    {
        if (unit.Owner != Runner.LocalPlayer)
            return;

        // 이전 선택 완전히 해제
        if (_localSelectedUnit != null)
        {
            _localSelectedUnit.Deselect();
            if (gridManager != null)
            {
                gridManager.ClearMovableHighlights();
            }
        }

        _localSelectedUnit = unit;
        _localSelectedUnit.Select();

        SetPlayerState(PlayerTurnState.UnitSelected);
        uiManager.ShowUnitInfo(unit);
        uiManager.skillPanelUI?.UpdateSkillDisplay(unit);
    }

    public void ClearSelection()
    {
        if (_localSelectedUnit != null)
            _localSelectedUnit.Deselect();
        _localSelectedUnit = null;

        if (gridManager != null)
        {
            gridManager.ClearSelection();
        }

        if (uiManager != null)
        {
            uiManager.HideUnitInfo();
            uiManager.HideSkillPanel();
        }

        // Networked properties can only be accessed after Spawned().
        // Add a guard to prevent errors if called before initialization.
        if (Object == null || !Object.IsValid)
        {
            return;
        }

        if (
            CurrentTurnPlayer == Runner.LocalPlayer
            && CurrentPlayerState != PlayerTurnState.AwaitingTurn
        )
        {
            SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        }
    }

    public void SetBattleEnded()
    {
        Debug.Log("[TurnManager] Battle ended - disabling turn system");

        if (HasStateAuthority)
        {
            IsPlayerTurn = false;
            CurrentTurnPlayer = PlayerRef.None;
        }

        SetPlayerState(PlayerTurnState.StageClear);

        if (Core.Instance?.UIManager != null)
        {
            Core.Instance.UIManager.SetEndTurnButtonActive(false);
        }

        _battleEnded = true;
    }

    #endregion

    #region Helpers

    public void SetPlayerState(PlayerTurnState newState)
    {
        CurrentPlayerState = newState;
    }

    public void ClearTurn()
    {
        if (HasStateAuthority)
        {
            CurrentTurnPlayer = PlayerRef.None;
            TurnNumber = 0;
            IsPlayerTurn = false;
            _turnIndex = -1;
            _playerTurnOrder.Clear();
        }
        SetPlayerState(PlayerTurnState.AwaitingTurn);
        _isMyTurn = false;
    }

    public void UpdateTurnOrderDisplay()
    {
        if (uiManager != null && uiManager.turnOrderUI != null)
        {
            string turnInfo = $"Turn {TurnNumber}\n";

            if (IsPlayerTurn)
            {
                if (_isMyTurn)
                    turnInfo += "<color=green>Your Turn</color>";
                else
                    turnInfo += $"<color=yellow>Player {CurrentTurnPlayer.PlayerId}'s Turn</color>";
            }
            else
            {
                turnInfo += "<color=red>Enemy Turn</color>";
            }
        }
    }

    public void TriggerUnitSkillEnd()
    {
        if (CurrentPlayerState == PlayerTurnState.PerformingAction)
        {
            SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        }
        OnPlayerSkillEnd?.Invoke();
    }
    #endregion
}
