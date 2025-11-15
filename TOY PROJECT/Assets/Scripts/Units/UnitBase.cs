using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class UnitBase : NetworkBehaviour
{
    public UnitData unitData;
    public FactionData factionData;

    [Networked]
    public int HP { get; set; }

    [Networked]
    public int AP { get; set; }

    [Networked]
    public Vector2Int Position { get; set; }

    [Networked]
    public PlayerRef Owner { get; set; }

    [Networked]
    public NetworkBool HasActedThisTurn { get; set; }

    [Networked, Capacity(10)]
    private NetworkArray<int> SkillCooldowns => default;

    public Vector2Int position
    {
        get => Position;
        set => Position = value;
    }
    public int hp
    {
        get => HP;
        set => HP = value;
    }
    public int ap
    {
        get => AP;
        set => AP = value;
    }
    private Action OnSelected;
    private Action OnDeselected;
    private List<Skill> _skills = new List<Skill>();
    private Animator _animator;
    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    [Header("스킬 / 업그레이드")]
    [SerializeField]
    private UnitBuildData buildData;
    public UnitBuildData BuildData
    {
        get
        {
            if (buildData == null)
                buildData = new UnitBuildData();
            return buildData;
        }
    }

    protected GameAssetDatabase Database => Core.Instance?.GameDatabase;

    public override void Spawned()
    {
        // Initialize networked properties on the StateAuthority (server).
        // These values will be replicated to all clients.
        if (HasStateAuthority)
        {
            if (unitData != null)
            {
                HP = unitData.maxHp;
                AP = unitData.maxAp;
            }
        }
    }

    protected virtual void Awake()
    {
        if (unitData != null)
        {
            // Non-networked initialization can stay here.
            _animator = GetComponent<Animator>();
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            // Skills are local objects, so they can be created here.
            foreach (var skillData in unitData.skills)
            {
                _skills.Add(new Skill(skillData));
            }
        }
    }

    public void Initialize(Vector2Int spawnPos, PlayerRef owner)
    {
        Position = spawnPos;
        Owner = owner;
        position = spawnPos;
    }

    public bool IsLocalClient()
    {
        return HasInputAuthority;
    }

    public virtual async UniTask UseSkillAsync(int skillIndex, Vector2Int targetPos)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Count)
        {
            Debug.LogError("Invalid skill index.");
            return;
        }

        var skill = _skills[skillIndex];

        if (skill.currentCooldown > 0)
        {
            Debug.LogWarning(
                $"Skill {skill.data.skillMeta.nameKey} is on cooldown for {skill.currentCooldown} more turns."
            );
            return;
        }

        int apCost = skill.GetAPCost();
        if (ap < apCost)
        {
            Debug.LogWarning("Not enough AP to use this skill.");
            return;
        }

        DebugPrinter.LogColor(
            LogType.Unit,
            $"Using skill '{skill.data.skillMeta.nameKey}' on {targetPos}. AP before: {ap}, Cost: {apCost}"
        );

        ap -= apCost;

        if (skill.data.cooldown > 0)
            skill.currentCooldown = skill.data.cooldown;

        DebugPrinter.LogColor(
            LogType.Unit,
            $"{name} used {skill.data.skillMeta.nameKey} on target at {targetPos}. AP left: {ap}"
        );

        await skill.ExecuteAsync(this, targetPos);
    }

    public void RequestSkillUse(int skillIndex, Vector2Int targetPos)
    {
        if (!HasInputAuthority && !(this is EnemyUnit))
            return;

        UseSkillRpc(skillIndex, targetPos);
    }

    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    private async void UseSkillRpc(int skillIndex, Vector2Int targetPos)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Count)
        {
            Debug.LogError("Invalid skill index.");
            return;
        }

        var skill = _skills[skillIndex];

        // 전투가 끝났는지 체크
        bool battleEnded = Core.Instance?.TurnManager != null && Core.Instance.TurnManager.BattleEnded;

        // 전투가 끝나지 않았을 때만 쿨타임 체크
        if (!battleEnded && SkillCooldowns[skillIndex] > 0)
        {
            Debug.LogWarning($"Skill on cooldown: {SkillCooldowns[skillIndex]} turns left");
            return;
        }

        int apCost = skill.GetAPCost();
        
        // 전투가 끝나지 않았을 때만 AP 체크
        if (!battleEnded && ap < apCost)
        {
            Debug.LogWarning("Not enough AP to use this skill.");
            return;
        }

        // 전투가 끝나지 않았을 때만 CanExecute 체크
        if (!battleEnded && !skill.CanExecute(this, targetPos))
        {
            Debug.LogWarning(
                $"Cannot execute skill '{skill.data.skillMeta.nameKey}' at {targetPos}. Invalid target or out of range."
            );
            return;
        }

        DebugPrinter.LogColor(
            LogType.Unit,
            $"Using skill '{skill.data.skillMeta.nameKey}' on {targetPos}. AP before: {ap}, Cost: {apCost}"
        );

        // 전투가 끝나지 않았을 때만 AP 소모
        if (!battleEnded)
        {
            ap -= apCost;
        }

        // 전투가 끝나지 않았을 때만 쿨타임 설정
        if (!battleEnded && skill.data.cooldown > 0)
        {
            SkillCooldowns.Set(skillIndex, skill.data.cooldown);
            skill.currentCooldown = skill.data.cooldown;
        }

        DebugPrinter.LogColor(
            LogType.Unit,
            $"{name} used {skill.data.skillMeta.nameKey} on target at {targetPos}. AP left: {ap}"
        );

        await skill.ExecuteAsync(this, targetPos);
        PlaySkillVisualsRpc(skillIndex, targetPos);

        HasActedThisTurn = true;

        if (Core.Instance?.TurnManager != null)
        {
            Core.Instance.TurnManager.TriggerUnitSkillEnd();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void PlaySkillVisualsRpc(int skillIndex, Vector2Int targetPos)
    {
        Debug.Log($"Playing skill visuals for skill {skillIndex} on {targetPos}");
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
        if (HasStateAuthority)
        {
            TakeDamageRpc(amount);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void TakeDamageRpc(int amount)
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
        Core.Instance.EventManager.TriggerUnitDied(this);

        if (this is EnemyUnit enemyUnit)
        {
            Core.Instance.UnitManager.UnregisterEnemy(enemyUnit);
        }

        if (HasStateAuthority && Runner != null)
        {
            Runner.Despawn(Object);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public virtual void OnTurnStart()
    {
        DebugPrinter.LogColor(LogType.Unit, $"{name}'s turn starts, AP reset.");
        ap = unitData.maxAp;
        ReduceSkillCooldowns();
    }

    public virtual void ReduceSkillCooldowns()
    {
        if (HasStateAuthority)
        {
            for (int i = 0; i < _skills.Count && i < SkillCooldowns.Length; i++)
            {
                if (SkillCooldowns[i] > 0)
                {
                    SkillCooldowns.Set(i, SkillCooldowns[i] - 1);
                }
            }
        }

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
        if (skillIndex < 0 || skillIndex >= _skills.Count)
            return;

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
        if (skillIndex < 0 || skillIndex >= _skills.Count)
            return -1;
        return _skills[skillIndex].currentCooldown;
    }

    public virtual void OnEnable()
    {
        OnSelected += () =>
        {
            DebugPrinter.LogColor(LogType.Unit, $"{factionData.factionName} is Selected");
        };
        OnDeselected += () =>
        {
            DebugPrinter.LogColor(LogType.Unit, $"{factionData.factionName} is DeSelected");
        };
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
        if (gridManager == null)
            return;

        gridManager.ClearMovableHighlights();
        gridManager.ClearTargetHighlights();

        // 전투가 끝났을 때 (포탈 생성 후) 무한 이동 가능
        if (Core.Instance?.TurnManager != null &&
            Core.Instance.TurnManager.BattleEnded)
        {
            // 포탈이 있는 위치까지 포함하여 이동 가능한 모든 타일 표시
            var allWalkableTiles = gridManager.GetWalkableTilesInRange(position, 999);
            gridManager.HighlightMovableTiles(allWalkableTiles);
            return;
        }

        if (ap <= 0)
            return;

        var allUnits = gridManager.GetAllUnits();
        var potentialTargets = new List<UnitBase>();
        var movableTiles = new List<Vector2Int>();

        bool hasMoveAction = false;
        bool hasAttackAction = false;

        for (int i = 0; i < _skills.Count; i++)
        {
            var skill = _skills[i];

            if (skill.currentCooldown > 0 || ap < skill.GetAPCost())
                continue;

            if (skill.data.skillType == SkillType.Attack)
            {
                hasAttackAction = true;
                foreach (var potentialTarget in allUnits)
                {
                    if (potentialTarget.factionData == this.factionData)
                        continue;

                    if (
                        skill.data.initialBehaviors.Length > 0
                        && skill
                            .data.initialBehaviors[0]
                            .CanExecute(this, potentialTarget.position, skill)
                    )
                    {
                        if (!potentialTargets.Contains(potentialTarget))
                            potentialTargets.Add(potentialTarget);
                    }
                }
            }

            if (skill.data.skillType == SkillType.Move)
            {
                hasMoveAction = true;
                foreach (var offset in skill.data.movementPattern)
                {
                    Vector2Int destination = position + offset;
                    if (!gridManager.IsValidTile(destination))
                        continue;

                    UnitBase unitOnTile = gridManager.GetUnitAt(destination);

                    if (unitOnTile != null)
                    {
                        if (
                            unitOnTile.factionData != this.factionData
                            && !potentialTargets.Contains(unitOnTile)
                        )
                        {
                            potentialTargets.Add(unitOnTile);
                        }
                    }
                    else if (
                        skill.data.initialBehaviors.Length > 0
                        && skill.data.initialBehaviors[0].CanExecute(this, destination, skill)
                    )
                    {
                        if (!movableTiles.Contains(destination))
                            movableTiles.Add(destination);
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

    private System.Collections.IEnumerator MoveAlongPathCoroutine(
        List<Vector2Int> path,
        Action onComplete
    )
    {
        float moveSpeed = 5f;

        foreach (var targetCell in path)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = new Vector3(
                targetCell.x + 0.5f,
                transform.position.y,
                targetCell.y + 0.5f
            );
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

    public float PerformAttackMotion(Vector2Int targetPos, System.Action onHitFrame)
    {
        float speedMultiplier = unitData != null ? unitData.animationSpeedMultiplier : 1.0f;

        Vector3 originalPos = transform.position;
        Vector3 targetWorldPos = new Vector3(
            targetPos.x + 0.5f,
            transform.position.y,
            targetPos.y + 0.5f
        );
        Vector3 attackPos = Vector3.Lerp(
            originalPos,
            targetWorldPos,
            UnitAnimationConfig.ATTACK_APPROACH_DISTANCE_RATIO
        );

        float approachDuration = UnitAnimationConfig.GetAttackApproachDuration(speedMultiplier);
        float hitDuration = UnitAnimationConfig.GetAttackHitDuration(speedMultiplier);
        float returnDuration = UnitAnimationConfig.GetAttackReturnDuration(speedMultiplier);

        Sequence seq = DOTween.Sequence();

        seq.Append(transform.DOMove(attackPos, approachDuration).SetEase(Ease.OutExpo));

        if (_animator != null)
        {
            seq.AppendCallback(() => _animator.Play("Attack"));
            seq.AppendInterval(hitDuration);
        }
        else
        {
            seq.AppendInterval(hitDuration);
        }

        seq.AppendCallback(() => onHitFrame?.Invoke());

        seq.Append(transform.DOMove(originalPos, returnDuration).SetEase(Ease.InQuad));

        return UnitAnimationConfig.GetTotalAttackDuration(speedMultiplier);
    }

    public void PlayFlashEffect(Color flashColor, float duration)
    {
        if (_renderer == null || _propertyBlock == null)
            return;

        DOVirtual
            .Float(
                1f,
                0f,
                duration,
                (amount) =>
                {
                    _propertyBlock.SetColor("_FlashColor", flashColor);
                    _propertyBlock.SetFloat("_FlashAmount", amount);
                    _renderer.SetPropertyBlock(_propertyBlock);
                }
            )
            .SetEase(Ease.OutQuad);
    }

    #region 스킬 및 업그레이드

    /// <summary>
    /// 스킬에 업그레이드를 적용하고 빌드 데이터에 기록합니다.
    /// </summary>
    public void ApplySkillUpgrade(int skillIndex, UpgradeData upgradeData)
    {
        if (skillIndex < 0 || skillIndex >= _skills.Count)
        {
            Debug.LogError($"Invalid skill index: {skillIndex}");
            return;
        }

        var skill = _skills[skillIndex];

        // UpgradeBehavior 실행
        if (upgradeData.behavior != null)
        {
            upgradeData.behavior.Apply(this, skill);
        }

        // 빌드 데이터에 기록
        BuildData.AddSkillUpgrade(skillIndex, skill.data.skillMeta.nameKey, upgradeData);

        Debug.Log(
            $"[BuildData] Skill upgraded: {skill.data.skillMeta.nameKey} with {upgradeData.upgradeName}"
        );
    }

    /// <summary>
    /// 새로운 스킬을 배웁니다.
    /// </summary>
    public void LearnSkill(SkillData skillData)
    {
        // 이미 배운 스킬인지 확인
        if (BuildData.learnedSkills.Contains(skillData))
        {
            Debug.LogWarning($"Skill {skillData.skillMeta.nameKey} is already learned.");
            return;
        }

        // 스킬 인스턴스 생성 및 추가
        var newSkill = new Skill(skillData);
        _skills.Add(newSkill);

        // 빌드 데이터에 기록
        BuildData.AddSkill(skillData);

        Debug.Log($"[BuildData] Learned new skill: {skillData.skillMeta.nameKey}");
    }

    /// <summary>
    /// 일반 업그레이드를 적용합니다 (스킬별이 아닌 유닛 전체)
    /// </summary>
    public void ApplyGeneralUpgrade(UpgradeData upgradeData)
    {
        if (upgradeData.behavior != null)
        {
            upgradeData.behavior.Apply(this, null);
        }

        BuildData.AddUpgrade(upgradeData);

        Debug.Log($"[BuildData] Applied general upgrade: {upgradeData.upgradeName}");
    }

    /// <summary>
    /// 현재 유닛의 빌드 데이터를 가져옵니다 (디버깅/UI용)
    /// </summary>
    public string GetBuildSummary()
    {
        string summary = $"=== {name} Build Summary ===\n";
        summary += $"Learned Skills: {BuildData.learnedSkills.Count}\n";
        foreach (var skill in BuildData.learnedSkills)
        {
            summary += $"  - {skill.skillMeta.nameKey}\n";
        }
        summary += $"General Upgrades: {BuildData.generalUpgrades.Count}\n";
        foreach (var upgrade in BuildData.generalUpgrades)
        {
            summary += $"  - {upgrade.upgradeName}\n";
        }
        return summary;
    }

    #endregion
}
