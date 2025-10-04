using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class UnitBase : MonoBehaviour
{
    public UnitData unitData;
    public FactionData factionData;
    public int hp;
    public int ap; // 행동력
    public Vector2Int position;
    private Action OnSelected;
    private Action OnDeselected;
    private List<int> _skillCooldowns = new List<int>();

    protected virtual void Awake()
    {
        if (unitData != null)
        {
            hp = unitData.maxHp;
            ap = unitData.maxAp;
            for (int i = 0; i < unitData.skills.Length; i++)
            {
                _skillCooldowns.Add(0);
            }
        }
    }

    public virtual float UseSkill(int skillIndex, Vector2Int targetPos)
    {
        if (skillIndex < 0 || skillIndex >= unitData.skills.Length)
        {
            Debug.LogError("Invalid skill index.");
            return 0f;
        }

        var skill = unitData.skills[skillIndex];
        if (_skillCooldowns[skillIndex] > 0)
        {
            Debug.LogWarning($"Skill {skill.skillMeta.nameKey} is on cooldown for {_skillCooldowns[skillIndex]} more turns.");
            return 0f;
        }

        if (ap < skill.apCost)
        {
            Debug.LogWarning("Not enough AP to use this skill.");
            return 0f;
        }

        DebugPrinter.DebugColor(DebugType.Unit, $"Using skill '{skill.skillMeta.nameKey}' on {targetPos}. AP before: {ap}, Cost: {skill.apCost}");
        ap -= skill.apCost;
        
        if (skill.cooldown == 0 && skill.apCost > 0)
        {
            _skillCooldowns[skillIndex] = 1;
        }
        else
        {
            _skillCooldowns[skillIndex] = skill.cooldown;
        }

        DebugPrinter.DebugColor(DebugType.Unit, $"{name} used {skill.skillMeta.nameKey} on target at {targetPos}. AP left: {ap}");

        float totalDuration = 0f;
        var context = new SkillContext(this, targetPos);
        foreach (var behavior in skill.initialBehaviors)
        {
            totalDuration += behavior.Execute(context);
        }

        if (skill.subTargetBehaviors != null && skill.subTargetBehaviors.Length > 0)
        {
            Core.Instance.TurnManager.PausedSkillData = skill;
            Core.Instance.TurnManager.PausedSkillContext = context;
            Core.Instance.TurnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingSkillSubTarget);
        }

        return totalDuration;
    }

    public int GetMoveSkillIndex()
    {
        for (int i = 0; i < unitData.skills.Length; i++)
        {
            var skill = unitData.skills[i];
            if (skill.skillType == SkillType.Move)
            {
                return i;
            }
        }
        return -1; // No move skill found
    }

    public virtual void TakeDamage(int amount)
    {
        hp -= amount;
        DebugPrinter.DebugColor(DebugType.Unit, $"{name} took {amount} damage, remaining HP: {hp}");

        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        DebugPrinter.DebugColor(DebugType.Unit, $"{name} has died.");
        Core.Instance.GridManager.UnregisterUnit(position);

        // Check if the dying unit is an enemy and unregister it from the StageManager
        if (this is EnemyUnit enemyUnit)
        {
            Core.Instance.StageManager.UnregisterEnemy(enemyUnit);
        }

        Destroy(gameObject);
    }

    public virtual void OnTurnStart()
    {
        DebugPrinter.DebugColor(DebugType.Unit, $"{name}'s turn starts, AP reset.");
        ap = unitData.maxAp;
        ReduceSkillCooldowns();
    }

    public virtual void ReduceSkillCooldowns()
    {
        for (int i = 0; i < _skillCooldowns.Count; i++)
        {
            if (_skillCooldowns[i] > 0)
            {
                _skillCooldowns[i]--;
            }
        }
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

        gridManager.ClearMovableHighlights();
        gridManager.ClearTargetHighlights();

        if (ap <= 0) return;

        var allUnits = gridManager.GetAllUnits();
        var potentialTargets = new List<UnitBase>();
        var movableTiles = new List<Vector2Int>();

        for (int i = 0; i < unitData.skills.Length; i++)
        {
            var skill = unitData.skills[i];

            if (_skillCooldowns[i] > 0 || ap < skill.apCost) continue;

            // Find potential attack targets from skill range
            if (skill.skillType == SkillType.Attack)
            {
                foreach (var potentialTarget in allUnits)
                {
                    if (potentialTarget.factionData == this.factionData) continue;
                    int distance = Mathf.Abs(position.x - potentialTarget.position.x) + Mathf.Abs(position.y - potentialTarget.position.y);
                    if (distance <= skill.range)
                    {
                        if (!potentialTargets.Contains(potentialTarget)) potentialTargets.Add(potentialTarget);
                    }
                }
            }

            // Find movable tiles AND enemies on move path
            if (skill.skillType == SkillType.Move)
            {
                foreach (var offset in skill.movementPattern)
                {
                    Vector2Int destination = position + offset;
                    if (!gridManager.IsValidTile(destination)) continue;
                    
                    UnitBase unitOnTile = gridManager.GetUnitAt(destination);
                    if (unitOnTile != null) // If a unit is on the destination tile
                    {
                        if (unitOnTile.factionData != this.factionData && !potentialTargets.Contains(unitOnTile))
                        {
                            potentialTargets.Add(unitOnTile); // Add enemy as a potential target
                        }
                    }
                    else // If the tile is empty
                    {
                        if (!movableTiles.Contains(destination)) movableTiles.Add(destination);
                    }
                }
            }
        }

        // Prioritize targets: if a tile is both movable and has a target, only show it as a target.
        // This logic is implicitly handled now because an occupied tile is never added to movableTiles.

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