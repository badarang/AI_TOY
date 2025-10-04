using System.Collections.Generic;
using UnityEngine;

public enum SkillType
{
    None,
    Attack,
    Move,
    Buff,
    Debuff,
    Etc
}

[CreateAssetMenu(menuName = "Data/SkillData")]
public class SkillData : ScriptableObject
{
    public SkillType skillType;
    public SkillMeta skillMeta; // 스킬 이름, 설명, 아이콘 등
    public int apCost;
    public int cooldown;
    public int range;

    [Header("Movement")]
    public List<Vector2Int> movementPattern;

    [Header("Initial Behaviors")]
    // 스킬 사용 시 즉시 발동되는 행동들
    public SkillBehavior[] initialBehaviors;

    [Header("Sub-Target Behaviors")]
    // 추가 대상 선택 후 발동되는 행동들
    public SkillBehavior[] subTargetBehaviors;
} 