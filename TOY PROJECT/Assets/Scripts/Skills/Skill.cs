using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 런타임에 존재하는 스킬 인스턴스
/// SkillData(템플릿)로부터 생성되며, 업그레이드를 통해 수정 가능한 수치를 관리합니다.
/// </summary>
public class Skill
{
    public SkillData data { get; private set; }
    public int currentCooldown { get; set; }

    // 업그레이드를 통해 수정되는 모든 수치 보정값
    // 예: "damage": +5, "range": +1, "apCost": -1

    // Behavior 간 데이터 전달을 위한 블랙보드 (예: 벽 충돌 여부, 타겟 유닛 등)
    public Dictionary<string, object> blackboard = new Dictionary<string, object>();
    public Dictionary<string, float> modifiers = new Dictionary<string, float>();

    public Skill(SkillData data)
    {
        this.data = data;
        this.currentCooldown = 0;
    }

    /// <summary>
    /// 수정된 수치를 조회합니다.
    /// </summary>
    public int GetModifiedValue(string key, int baseValue)
    {
        float modifier = modifiers.ContainsKey(key) ? modifiers[key] : 0f;
        return Mathf.RoundToInt(baseValue + modifier);
    }

    public float GetModifiedValue(string key, float baseValue)
    {
        float modifier = modifiers.ContainsKey(key) ? modifiers[key] : 0f;
        return baseValue + modifier;
    }

    // 자주 사용되는 수치 조회 헬퍼 메서드
    public int GetAPCost() => GetModifiedValue("apCost", data.apCost);

    public int GetRange() => GetModifiedValue("range", data.range);

    public int GetCooldown() => GetModifiedValue("cooldown", data.cooldown);

    /// <summary>
    /// 스킬 실행 로직
    /// </summary>
/// <summary>
    /// 스킬 실행 로직 (비동기)
    /// </summary>
    public async Cysharp.Threading.Tasks.UniTask ExecuteAsync(UnitBase caster, Vector2Int targetPos)
    {
        blackboard.Clear();

        if (data.initialBehaviors != null)
        {
            foreach (var behavior in data.initialBehaviors)
            {
                if (behavior != null)
                {
                    await behavior.ExecuteAsync(caster, targetPos, this);
                }
            }
        }
    }

    /// <summary>
    /// 스킬 사용 가능 여부 확인
    /// </summary>
    public bool CanExecute(UnitBase caster, Vector2Int targetPos)
    {
        // AP 체크
        if (caster.ap < GetAPCost())
            return false;

        // 쿨다운 체크
        if (currentCooldown > 0)
            return false;

        // 사거리 체크
        int distance = GridUtils.ChebyshevDistance(caster.position, targetPos);
        if (distance > GetRange())
            return false;

        // Behavior의 CanExecute 체크
        if (data.initialBehaviors != null && data.initialBehaviors.Length > 0)
        {
            return data.initialBehaviors[0].CanExecute(caster, targetPos, this);
        }

        return true;
    }
}
