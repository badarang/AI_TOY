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
    public event Action OnPlayerActionEnd;

    // --- MANAGER REFS ---
    private UIManager uiManager;
    private bool _isMyTurn = false;
    public bool IsMyTurn => _isMyTurn;

    private StageManager stageManager;
    private UnitManager unitManager;

    private GridManager gridManager;

    private PlayerUnit _localSelectedUnit;
    public PlayerUnit SelectedUnit => _localSelectedUnit;

    #region IManager & Lifecycle

    public void BeforeInit() { }

    public void AfterInit()
    {
        uiManager = Core.Instance.UIManager;
        stageManager = Core.Instance.StageManager;
        gridManager = Core.Instance.GridManager;
        unitManager = Core.Instance.UnitManager;
    }

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

        if (_isMyTurn && CurrentPlayerState == PlayerTurnState.AwaitingTurn)
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
    public void RPC_EndTurn()
    {
        Debug.Log($"Server received EndTurn RPC from {CurrentTurnPlayer}.");
        StartNextTurn();
    }

    public void StartCombat()
    {
        if (!HasStateAuthority) return;

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
            if(!_playerTurnOrder.Contains(player.Owner))
                _playerTurnOrder.Add(player.Owner);
        }

        Debug.Log($"[TurnManager] Starting combat with {_playerTurnOrder.Count} players.");
        _turnIndex = -1;
        StartNextTurn();
    }

    private async void StartNextTurn()
    {
        if (!HasStateAuthority) return;

        _turnIndex++;

        if (_turnIndex < _playerTurnOrder.Count)
        {
            IsPlayerTurn = true;
            CurrentTurnPlayer = _playerTurnOrder[_turnIndex];
            Debug.Log($"[SERVER] Starting turn for Player {CurrentTurnPlayer}. Index: {_turnIndex}");
            UpdateTurnOrderDisplay();
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

            // This was the problematic line. Enemy spawning is the StageManager's job at the start.
            // await unitManager.SpawnEnemiesForTurn(TurnNumber);

            StartNextTurn();
        }
    }

    private async UniTask StartEnemyTurn()
    {
        OnEnemyTurnStart?.Invoke();

        var enemies = unitManager != null ? unitManager.GetEnemies() : new List<EnemyUnit>();
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.OnTurnStart();
        }

        if(Core.Instance.EnemyAIManager != null)
            await Core.Instance.EnemyAIManager.ExecuteEnemyTurns().ToUniTask(this);

        await CheckForWaveClearAsync();
    }

    private async UniTask CheckForWaveClearAsync()
    {
        if ((unitManager != null ? unitManager.GetEnemies().Count : 0) == 0)
        {
            Debug.Log("All enemies cleared! Stage complete!");
            SetPlayerState(PlayerTurnState.StageClear);
        }
    }

    #endregion

    #region Input & Actions (Client-Side)

    public void RequestEndTurn()
    {
        if (CurrentTurnPlayer == Runner.LocalPlayer)
        {
            RPC_EndTurn();
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
                else if (unitAtCell is PlayerUnit friendlyUnit && friendlyUnit.Owner == Runner.LocalPlayer)
                {
                    SelectPlayerUnit(friendlyUnit);
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
            return;
        }

        // 스킬 사용 가능 여부를 먼저 확인
        var skills = unit.GetSkills();
        if (moveSkillIndex >= skills.Count)
        {
            Debug.LogWarning("[TurnManager] Invalid move skill index");
            return;
        }

        var moveSkill = skills[moveSkillIndex];
        if (!moveSkill.CanExecute(unit, targetCell))
        {
            Debug.LogWarning($"[TurnManager] Cannot move to {targetCell}. Out of range or invalid tile.");
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
        for (int i = 0; i < unit.GetSkills().Count; i++)
        {
            if (unit.GetSkills()[i].data.skillType == SkillType.Attack)
            {
                attackSkillIndex = i;
                break;
            }
        }

        if (attackSkillIndex < 0) return;

        unit.RequestSkillUse(attackSkillIndex, targetCell);
        ClearSelection();
        SetPlayerState(PlayerTurnState.PerformingAction);
    }

    public void SelectPlayerUnit(PlayerUnit unit)
    {
        if (unit.Owner != Runner.LocalPlayer) return;

        if (_localSelectedUnit != null) _localSelectedUnit.Deselect();

        _localSelectedUnit = unit;
        _localSelectedUnit.Select();

        SetPlayerState(PlayerTurnState.UnitSelected);
        uiManager.ShowUnitInfo(unit);
        uiManager.skillPanelUI?.UpdateSkillDisplay(unit);
    }

    public void ClearSelection()
    {
        if (_localSelectedUnit != null) _localSelectedUnit.Deselect();
        _localSelectedUnit = null;
        gridManager.ClearSelection();
        uiManager.HideUnitInfo();
        uiManager.HideSkillPanel();

        if (CurrentTurnPlayer == Runner.LocalPlayer && CurrentPlayerState != PlayerTurnState.AwaitingTurn)
        {
            SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        }
    }

    #endregion

    #region Helpers

    public void SetPlayerState(PlayerTurnState newState)
    {
        CurrentPlayerState = newState;
    }

    public void ClearTurn()
    {
        if(HasStateAuthority)
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
                if (_isMyTurn) turnInfo += "<color=green>Your Turn</color>";
                else turnInfo += $"<color=yellow>Player {CurrentTurnPlayer.PlayerId}'s Turn</color>";
            }
            else
            {
                turnInfo += "<color=red>Enemy Turn</color>";
            }
        }
    }

    public void TriggerUnitActionEnd()
    {
        if (CurrentPlayerState == PlayerTurnState.PerformingAction)
        {
            SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        }
        OnPlayerActionEnd?.Invoke();
    }
    #endregion
}