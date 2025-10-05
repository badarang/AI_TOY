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
            Core.Instance.TurnManager.EndTurn();
            Debug.Log("[DEBUG] Turn manually ended.");
        }
        else
        {
            Debug.LogError("[DEBUG] Core, or TurnManager not found.");
        }
    }

    [Category("Game Management")]
    [DisplayName("Add Player AP +1")]
    public void AddPlayerAP()
    {
        var player = Core.Instance?.StageManager?.GetPlayer();
        if (player != null)
        {
            player.ap += 1;
            Debug.Log($"[DEBUG] Player AP is now {player.ap}");

            // If player is selected, refresh highlights
            // if (Core.Instance.GridManager.GetSelectedUnit() == player)
            // {
            //     player.ShowAvailableActions();
            // }
        }
        else
        {
            Debug.LogError("[DEBUG] Player not found.");
        }
    }

    [Category("Game Management")]
    [DisplayName("Damage All Enemies (10)")]
    public void DamageAllEnemies()
    {
        var enemies = Core.Instance?.StageManager?.GetEnemies();
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
            Debug.Log($"[DEBUG] Damaged {count} enemies by 10.");
        }
        else
        {
            Debug.LogError("[DEBUG] StageManager or enemies not found.");
        }
    }
}

