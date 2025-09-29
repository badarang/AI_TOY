using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public abstract class UnitBase : MonoBehaviour
{
    public UnitData unitData;
    public FactionData factionData;
    public int hp;
    public int ap; // 행동력
    public Vector2Int position;
    private Action OnSelected;
    private Action OnDeselected;

    protected virtual void Awake()
    {
        if (unitData != null)
        {
            hp = unitData.maxHp;
            ap = unitData.maxAp;
        }
    }

    public abstract void UseSkill(int skillIndex, Vector2Int targetPos);

    public int GetMoveSkillIndex()
    {
        for (int i = 0; i < unitData.skills.Length; i++)
        {
            var skill = unitData.skills[i];
            if (skill.movementPattern != null && skill.movementPattern.Count > 0)
            {
                return i;
            }
        }
        return -1; // No move skill found
    }

    public virtual void TakeDamage(int amount)
    {
        hp -= amount;
        Debug.Log($"{name} took {amount} damage, remaining HP: {hp}");

        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"{name} has died.");
        Core.Instance.GridManager.UnregisterUnit(position);
        Destroy(gameObject);
    }

    public virtual void OnTurnStart()
    {
        Debug.Log($"{name}'s turn starts, AP reset.");
        ap = unitData.maxAp;
    }

    public virtual void OnEnable()
    {
        OnSelected += () => { DebugPrinter.DebugColor(DebugType.Unit, $"{factionData.factionName} is Selected"); };
        OnDeselected += () => { DebugPrinter.DebugColor(DebugType.Unit, $"{factionData.factionName} is DeSelected"); };
    }

    public virtual void OnDisable()
    {
        OnSelected = null;
        OnDeselected = null;
    }

    public virtual void Select()
    {
        var outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = true;
        
        ShowAvailableActions();
        OnSelected?.Invoke();
    }

    public virtual void ShowAvailableActions()
    {
        var gridManager = Core.Instance?.GridManager;
        if (gridManager == null) return;

        // Clear previous highlights
        gridManager.ClearMovableHighlights();
        gridManager.ClearTargetHighlights();

        if (ap <= 0) return;

        var allUnits = gridManager.GetAllUnits();
        var potentialTargets = new List<UnitBase>();
        var movableTiles = new List<Vector2Int>();

        // Iterate through all skills to find possible actions
        foreach (var skill in unitData.skills)
        {
            // Check for Attack capabilities
            if (skill.range > 0)
            {
                foreach (var potentialTarget in allUnits)
                {
                    if (potentialTarget == this || potentialTarget.factionData == this.factionData) continue;
                    int distance = Mathf.Abs(position.x - potentialTarget.position.x) + Mathf.Abs(position.y - potentialTarget.position.y);
                    if (distance <= skill.range)
                    {
                        if (!potentialTargets.Contains(potentialTarget))
                        {
                            potentialTargets.Add(potentialTarget);
                        }
                    }
                }
            }

            // Check for Movement capabilities
            if (skill.movementPattern != null && skill.movementPattern.Count > 0)
            {
                var tiles = gridManager.FindMovableTiles(position, skill.movementPattern);
                foreach (var tile in tiles)
                {
                    if (!movableTiles.Contains(tile))
                    {
                        movableTiles.Add(tile);
                    }
                }
            }
        }

        gridManager.HighlightMovableTiles(movableTiles);
        gridManager.HighlightTargets(potentialTargets);
    }

    public virtual void Deselect()
    {
        var outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;

        if (Core.Instance?.GridManager != null)
        {
            Core.Instance.GridManager.ClearMovableHighlights();
            Core.Instance.GridManager.ClearTargetHighlights();
        }

        OnDeselected?.Invoke();
    }
} 