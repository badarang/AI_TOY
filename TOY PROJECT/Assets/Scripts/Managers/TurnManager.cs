using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;

// TurnManager is now a NetworkBehaviour to manage and sync turn state.
public class TurnManager : NetworkBehaviour, IManager
{
    // --- NETWORKED STATE ---
    [Networked] public PlayerRef CurrentTurnPlayer { get; private set; }
    [Networked] public int TurnNumber { get; private set; }
    [Networked] public NetworkBool IsPlayerTurn { get; set; }

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
        StageClear
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
    }

    public override void Spawned()
    {
        // This component is part of the scene, so it will be spawned on all clients.
        // We can initialize UI and other non-networked things here.
        if (Core.Instance != null)
        {
            AfterInit();
        }
    }

public override void FixedUpdateNetwork()
    {
        // Update the local player's state based on the synced network state
        bool wasMyTurn = _isMyTurn;
        _isMyTurn = (CurrentTurnPlayer == Runner.LocalPlayer);
        
        if (_isMyTurn && CurrentPlayerState == PlayerTurnState.AwaitingTurn)
        {
            Debug.Log("It is now my turn!");
            SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
            
            // Refresh AP for all of this player's units
            var myUnits = stageManager.GetAllPlayers().Where(p => p.Owner == Runner.LocalPlayer);
            foreach (var unit in myUnits)
            {
                unit.OnTurnStart();
            }
            OnPlayerTurnStart?.Invoke();
            
            // Update UI to enable EndTurn button
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
            
            // Update UI to disable EndTurn button
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

    public void StartFirstWave()
    {
        if (!HasStateAuthority) return;

        TurnNumber = 0;
        
        // Build the turn order
        _playerTurnOrder.Clear();
        var allPlayers = stageManager.GetAllPlayers().OrderBy(p => p.Owner.PlayerId).ToList();
        foreach (var player in allPlayers)
        {
            _playerTurnOrder.Add(player.Owner);
        }
        
        Debug.Log($"Starting first wave with {_playerTurnOrder.Count} players.");
        _turnIndex = -1;
        StartNextTurn();
    }

private async void StartNextTurn()
    {
        if (!HasStateAuthority) return;

        _turnIndex++;

        if (_turnIndex < _playerTurnOrder.Count)
        {
            // It's a player's turn
            IsPlayerTurn = true;
            CurrentTurnPlayer = _playerTurnOrder[_turnIndex];
            Debug.Log($"[SERVER] Starting turn for Player {CurrentTurnPlayer}. Index: {_turnIndex}");
            UpdateTurnOrderDisplay();
        }
        else
        {
            // It's the AI's turn
            IsPlayerTurn = false;
            CurrentTurnPlayer = PlayerRef.None;
            Debug.Log("[SERVER] All players have acted. Starting enemy turn.");
            UpdateTurnOrderDisplay();
            await StartEnemyTurn();
            
            // When enemy turn is over, start the next round
            _turnIndex = -1;
            TurnNumber++;
            await stageManager.SpawnEnemiesForTurn(TurnNumber);
            StartNextTurn();
        }
    }

    private async UniTask StartEnemyTurn()
    {
        OnEnemyTurnStart?.Invoke();
        
        var enemies = stageManager.GetEnemies();
        foreach (var enemy in enemies)
        {
            if (enemy != null) enemy.OnTurnStart();
        }

        await Core.Instance.EnemyAIManager.ExecuteEnemyTurns().ToUniTask(this);
        
        await CheckForWaveClearAsync();
    }

    private async UniTask CheckForWaveClearAsync()
    {
        if (stageManager.GetEnemies().Count == 0)
        {
            // For simplicity, we'll just log this. A full implementation would
            // show rewards and proceed to the next stage.
            Debug.Log("All waves cleared! Stage complete!");
            SetPlayerState(PlayerTurnState.StageClear);
            // In a real game, you'd have an RPC to notify clients of stage clear.
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
        var unitAtCell = gridManager.GetUnitAt(cell);

        // 다른 플레이어의 턴일 때는 유닛 정보만 볼 수 있음
        if (CurrentTurnPlayer != Runner.LocalPlayer)
        {
            if (unitAtCell != null)
            {
                Debug.Log($"Viewing unit info (not my turn): {unitAtCell.name}");
                uiManager.ShowUnitInfo(unitAtCell);
            }
            else
            {
                uiManager.HideUnitInfo();
            }
            return;
        }

        // 내 턴일 때는 정상적으로 유닛 선택 및 행동 가능
        switch (CurrentPlayerState)
        {
            case PlayerTurnState.AwaitingUnitSelection:
                if (unitAtCell is PlayerUnit playerUnit && playerUnit.Owner == Runner.LocalPlayer)
                {
                    SelectPlayerUnit(playerUnit);
                }
                else if (unitAtCell != null)
                {
                    // 적 유닛이나 다른 플레이어 유닛 정보 보기
                    uiManager.ShowUnitInfo(unitAtCell);
                }
                else
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
                    // Clicked on an empty cell - try to move
                    TryMoveUnit(_localSelectedUnit, cell);
                }
                else if (unitAtCell.Owner != Runner.LocalPlayer)
                {
                    // Clicked on an enemy or other player's unit - try to attack
                    TryAttackUnit(_localSelectedUnit, cell);
                }
                else if (unitAtCell is PlayerUnit friendlyUnit && friendlyUnit.Owner == Runner.LocalPlayer)
                {
                    // Clicked on another friendly unit - switch selection
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
            Debug.LogWarning("Selected unit has no move skill.");
            return;
        }
        
        // Note: We are not checking range on the client. The server will validate this.
        // The client simply requests the action.
        Debug.Log($"Requesting move for {unit.name} to {targetCell}");
        unit.RequestSkillUse(moveSkillIndex, targetCell);
        
        // After an action, we deselect and wait for the server to update the state.
        ClearSelection();
        SetPlayerState(PlayerTurnState.PerformingAction);
    }

    private void TryAttackUnit(PlayerUnit unit, Vector2Int targetCell)
    {
        // For simplicity, find the first available attack skill.
        int attackSkillIndex = -1;
        for (int i = 0; i < unit.GetSkills().Count; i++)
        {
            if (unit.GetSkills()[i].data.skillType == SkillType.Attack)
            {
                attackSkillIndex = i;
                break;
            }
        }

        if (attackSkillIndex < 0)
        {
            Debug.LogWarning("Selected unit has no attack skill.");
            return;
        }
        
        Debug.Log($"Requesting attack for {unit.name} on {targetCell}");
        unit.RequestSkillUse(attackSkillIndex, targetCell);

        // After an action, we deselect and wait for the server to update the state.
        ClearSelection();
        SetPlayerState(PlayerTurnState.PerformingAction);
    }

    public void SelectPlayerUnit(PlayerUnit unit)
    {
        if (unit.Owner != Runner.LocalPlayer)
        {
            Debug.LogWarning("Cannot select a unit that is not mine.");
            return;
        }
        
        if (_localSelectedUnit != null)
        {
            _localSelectedUnit.Deselect();
        }
        
        _localSelectedUnit = unit;
        _localSelectedUnit.Select();
        
        SetPlayerState(PlayerTurnState.UnitSelected);
        Debug.Log($"Player unit selected: {unit.name}");
        
        uiManager.ShowUnitInfo(unit);
        uiManager.skillPanelUI?.UpdateSkillDisplay(unit);
    }

    public void ClearSelection()
    {
        if (_localSelectedUnit != null)
        {
            _localSelectedUnit.Deselect();
        }
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
        Debug.Log($"Local player state changed to: {newState}");
    }
    
public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
{
}

public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
{
}

public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
{
}

public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
{
}

public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
{
}
    #endregion


public void UpdateTurnOrderDisplay()
    {
        if (uiManager != null && uiManager.turnOrderUI != null)
        {
            // Display current turn information
            string turnInfo = $"Turn {TurnNumber + 1}\n";
            
            if (IsPlayerTurn)
            {
                if (_isMyTurn)
                {
                    turnInfo += "<color=green>Your Turn</color>";
                }
                else
                {
                    turnInfo += $"<color=yellow>Player {CurrentTurnPlayer.PlayerId + 1}'s Turn</color>";
                }
            }
            else
            {
                turnInfo += "<color=red>Enemy Turn</color>";
            }
            
            Debug.Log($"[TurnManager] {turnInfo}");
        }
    }


public void TriggerUnitActionEnd()
    {
        // Called when a unit finishes an action (like using a skill)
        if (CurrentPlayerState == PlayerTurnState.PerformingAction)
        {
            SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        }
        OnPlayerActionEnd?.Invoke();
    }
}