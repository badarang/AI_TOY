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
    private List<Skill> _skills = new List<Skill>();

protected virtual void Awake()
    {
        if (unitData != null)
        {
            hp = unitData.maxHp;
            ap = unitData.maxAp;
            
            // SkillData로부터 Skill 인스턴스 생성
            foreach (var skillData in unitData.skills)
            {
                _skills.Add(new Skill(skillData));
            }
        }
    }

public virtual float UseSkill(int skillIndex, Vector2Int targetPos)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Count)
        {
            Debug.LogError("Invalid skill index.");
            return 0f;
        }

        var skill = _skills[skillIndex];
        
        // 쿨다운 체크
        if (skill.currentCooldown > 0)
        {
            Debug.LogWarning($"Skill {skill.data.skillMeta.nameKey} is on cooldown for {skill.currentCooldown} more turns.");
            return 0f;
        }

        // AP 체크
        int apCost = skill.GetAPCost();
        if (ap < apCost)
        {
            Debug.LogWarning("Not enough AP to use this skill.");
            return 0f;
        }

        DebugPrinter.LogColor(LogType.Unit, $"Using skill '{skill.data.skillMeta.nameKey}' on {targetPos}. AP before: {ap}, Cost: {apCost}");
        
        // AP 소모
        ap -= apCost;
        
        // 쿨다운 설정
        if (skill.data.cooldown > 0) skill.currentCooldown = skill.data.cooldown;

        DebugPrinter.LogColor(LogType.Unit, $"{name} used {skill.data.skillMeta.nameKey} on target at {targetPos}. AP left: {ap}");

        // 스킬 실행
        return skill.Execute(this, targetPos);
    }

public int GetMoveSkillIndex()
    {
        for (int i = 0; i < _skills.Count; i++)
        {
            var skill = _skills[i];
            if (skill.data.skillType == SkillType.Move)
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
        foreach (var skill in _skills)
        {
            if (skill.currentCooldown > 0)
            {
                skill.currentCooldown--;
            }
        }
    }



    /// <summary>
    /// 스킬에 업그레이드를 적용합니다. (로그라이크용)
    /// </summary>
    public void ApplySkillUpgrade(int skillIndex, string modifierKey, float value)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Count) return;
        
        var skill = _skills[skillIndex];
        if (skill.modifiers.ContainsKey(modifierKey))
            skill.modifiers[modifierKey] += value;
        else
            skill.modifiers[modifierKey] = value;
        
        Debug.Log($"Skill upgraded: {skill.data.skillMeta.nameKey} - {modifierKey} +{value}");
    }
    
    /// <summary>
    /// 스킬 이름으로 인덱스를 찾습니다.
    /// </summary>
    public int FindSkillIndexByName(string skillName)
    {
        for (int i = 0; i < _skills.Count; i++)
        {
            if (_skills[i].data.skillMeta.nameKey == skillName)
                return i;
        }
        return -1;
    }
    
    /// <summary>
    /// 현재 보유한 Skill 목록을 반환합니다.
    /// </summary>
    public List<Skill> GetSkills() => _skills;
public int GetSkillCooldown(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Count) return -1;
        return _skills[skillIndex].currentCooldown;
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

        for (int i = 0; i < _skills.Count; i++)
        {
            var skill = _skills[i];

            if (skill.currentCooldown > 0 || ap < skill.GetAPCost()) continue;

            if (skill.data.skillType == SkillType.Attack)
            {
                hasAttackAction = true;
                foreach (var potentialTarget in allUnits)
                {
                    if (potentialTarget.factionData == this.factionData) continue;
                    
                    if (skill.data.initialBehaviors.Length > 0 && skill.data.initialBehaviors[0].CanExecute(this, potentialTarget.position, skill))
                    {
                        if (!potentialTargets.Contains(potentialTarget)) potentialTargets.Add(potentialTarget);
                    }
                }
            }

            if (skill.data.skillType == SkillType.Move)
            {
                hasMoveAction = true;
                foreach (var offset in skill.data.movementPattern)
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
                    else if (skill.data.initialBehaviors.Length > 0 && skill.data.initialBehaviors[0].CanExecute(this, destination, skill))
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
