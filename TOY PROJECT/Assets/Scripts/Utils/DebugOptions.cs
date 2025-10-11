using SRDebugger;
using UnityEngine;
using System.Collections.Generic;
using System.ComponentModel;

/// <summary>
/// SRDebugger에 표시될 디버그 옵션을 정의하는 partial 클래스입니다.
/// </summary>
public partial class SROptions
{
    [Category("Game Management")]
    [DisplayName("End Turn")]
    public void EndTurn()
    {
        if (Core.Instance != null && Core.Instance.TurnManager != null)
        {
            Core.Instance.TurnManager.RequestEndTurn();
            DebugPrinter.LogColor(LogType.Debug, "턴이 끝났습니다.");
        }
    }

    [Category("Game Management")]
    [DisplayName("Add Player AP +1")]
    public void AddPlayerAP()
    {
        var players = Core.Instance?.UnitManager?.GetAllPlayers();
        if (players != null)
        {
            foreach(var player in players)
            {
                player.ap += 1;
                DebugPrinter.LogColor(LogType.Debug, $"Player {player.name} AP is now {player.ap}");
            }
        }
    }

    [Category("Game Management")]
    [DisplayName("Damage All Enemies (10)")]
    public void DamageAllEnemies()
    {
        var enemies = Core.Instance?.UnitManager?.GetEnemies();
        if (enemies != null)
        {
            int count = 0;
            // Create a copy of the list to avoid modification issues if TakeDamage removes the unit
            foreach (var enemy in new List<EnemyUnit>(enemies))
            {
                if (enemy != null)
                {
                    enemy.TakeDamage(10);
                    count++;
                }
            }
            DebugPrinter.LogColor(LogType.Debug, $"Damaged {count} enemies by 10.");
        }
    }
}

