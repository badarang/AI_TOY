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
    public int ap;
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

        DebugPrinter.LogColor(LogType.Unit, $"Using skill '{skill.skillMeta.nameKey}' on {targetPos}. AP before: {ap}, Cost: {skill.apCost}");
        ap -= skill.apCost;
        
        if (skill.cooldown > 0) _skillCooldowns[skillIndex] = skill.cooldown;

        DebugPrinter.LogColor(LogType.Unit, $"{name} used {skill.skillMeta.nameKey} on target at {targetPos}. AP left: {ap}");

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
        return -1;
    }

    public virtual void TakeDamage(int amount)
    {
        hp -= amount;
        DebugPrinter.LogColor(LogType.Unit, $"{name} took {amount} damage, remaining HP: {hp}");

        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        DebugPrinter.LogColor(LogType.Unit, $"{name} has died.");
        Core.Instance.GridManager.UnregisterUnit(position);

        if (this is EnemyUnit enemyUnit)
        {
            Core.Instance.StageManager.UnregisterEnemy(enemyUnit);
        }

        Destroy(gameObject);
    }

    public virtual void OnTurnStart()
    {
        DebugPrinter.LogColor(LogType.Unit, $"{name}'s turn starts, AP reset.");
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

    public int GetSkillCooldown(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= _skillCooldowns.Count) return -1;
        return _skillCooldowns[skillIndex];
    }

    public virtual void OnEnable()
    {
        OnSelected += () => { DebugPrinter.LogColor(LogType.Unit, $"{factionData.factionName} is Selected"); };
        OnDeselected += () => { DebugPrinter.LogColor(LogType.Unit, $"{factionData.factionName} is DeSelected"); };
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

        bool hasMoveAction = false;
        bool hasAttackAction = false;

        for (int i = 0; i < unitData.skills.Length; i++)
        {
            var skill = unitData.skills[i];

            if (_skillCooldowns[i] > 0 || ap < skill.apCost) continue;

            if (skill.skillType == SkillType.Attack)
            {
                hasAttackAction = true;
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

            if (skill.skillType == SkillType.Move)
            {
                hasMoveAction = true;
                foreach (var offset in skill.movementPattern)
                {
                    Vector2Int destination = position + offset;
                    if (!gridManager.IsValidTile(destination)) continue;
                    
                    UnitBase unitOnTile = gridManager.GetUnitAt(destination);
                    if (unitOnTile != null)
                    {
                        if (unitOnTile.factionData != this.factionData && !potentialTargets.Contains(unitOnTile))
                        {
                            potentialTargets.Add(unitOnTile);
                        }
                    }
                    else
                    {
                        if (!movableTiles.Contains(destination)) movableTiles.Add(destination);
                    }
                }
            }
        }

        if (hasAttackAction && !hasMoveAction)
        {
            gridManager.HighlightTargets(potentialTargets);
        }
        else if (hasMoveAction && !hasAttackAction)
        {
            gridManager.HighlightMovableTiles(movableTiles);
        }
        else if (hasAttackAction && hasMoveAction)
        {
            gridManager.HighlightMovableTiles(movableTiles);
            gridManager.HighlightTargets(potentialTargets);
        }
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

    public virtual void MoveAlongPath(List<Vector2Int> path, Action onComplete)
    {
        if (path == null || path.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }
        
        StartCoroutine(MoveAlongPathCoroutine(path, onComplete));
    }

    private System.Collections.IEnumerator MoveAlongPathCoroutine(List<Vector2Int> path, Action onComplete)
    {
        float moveSpeed = 5f;
        
        foreach (var targetCell in path)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = new Vector3(targetCell.x + 0.5f, transform.position.y, targetCell.y + 0.5f);
            float distance = Vector3.Distance(startPos, endPos);
            float duration = distance / moveSpeed;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            
            transform.position = endPos;
            
            var gridManager = Core.Instance?.GridManager;
            if (gridManager != null)
            {
                gridManager.MoveUnit(position, targetCell);
                position = targetCell;
            }
        }
        
        onComplete?.Invoke();
    }
}
