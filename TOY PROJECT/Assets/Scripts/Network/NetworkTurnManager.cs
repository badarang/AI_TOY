using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Linq;

public class NetworkTurnManager : NetworkBehaviour
{
    [Networked] public int CurrentTurnIndex { get; set; }
    [Networked] public int CurrentWave { get; set; }
    [Networked] public int TurnInWave { get; set; }

    private List<UnitBase> turnOrder = new List<UnitBase>();
    public UnitBase CurrentTurnUnit => turnOrder.Count > 0 && CurrentTurnIndex < turnOrder.Count 
        ? turnOrder[CurrentTurnIndex] 
        : null;

    public void InitializeTurnOrder()
    {
        if (!HasStateAuthority) return;

        turnOrder.Clear();

        var players = FindObjectsOfType<PlayerUnit>().OrderBy(p => p.GetComponent<NetworkObject>().InputAuthority.PlayerId).ToList();
        var enemies = FindObjectsOfType<EnemyUnit>().ToList();

        foreach (var player in players)
        {
            turnOrder.Add(player);
        }

        foreach (var enemy in enemies)
        {
            turnOrder.Add(enemy);
        }

        CurrentTurnIndex = 0;
        Debug.Log($"Turn order initialized: {turnOrder.Count} units");
    }

    public void StartNextTurn()
    {
        if (!HasStateAuthority) return;

        CurrentTurnIndex++;

        if (CurrentTurnIndex >= turnOrder.Count)
        {
            CurrentTurnIndex = 0;
            TurnInWave++;
            Debug.Log($"Round complete, starting new round. Turn {TurnInWave}");
        }

        var currentUnit = CurrentTurnUnit;
        if (currentUnit != null)
        {
            RPC_NotifyTurnStart(currentUnit.GetComponent<NetworkObject>().Id);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyTurnStart(NetworkId unitId)
    {
        Debug.Log($"Turn started for unit: {unitId}");
    }

    public void EndCurrentTurn()
    {
        if (!HasStateAuthority) return;

        var currentUnit = CurrentTurnUnit;
        if (currentUnit != null)
        {
            // currentUnit.OnTurnEnd();
        }

        StartNextTurn();
    }

    public bool IsMyTurn(UnitBase unit)
    {
        return CurrentTurnUnit == unit;
    }
}
