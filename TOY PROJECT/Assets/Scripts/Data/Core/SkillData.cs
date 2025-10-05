using System.Collections.Generic;
using UnityEngine;

// 스킬의 등급을 정의합니다. 팬심(후원사 등급)에 따라 등장 확률이 달라집니다.
public enum SkillTier
{
    Normal,     // 일반
    Rare,       // 희귀
    Heroic,     // 영웅
    Legendary,  // 전설
    Cursed      // 저주: 매우 강력하지만 큰 디메리트를 가짐
}

// 스킬의 속성이나 특성을 구분하기 위한 태그입니다. (예: 화염, 냉기, 이동, 방어 등)
// 쇼러너 AI가 플레이어의 빌드를 분석하거나, 업그레이드 대상을 필터링하는 데 사용됩니다.
public enum SkillTag
{
    Movement,
    Attack,
    Defense,
    Utility
}

// SkillData.skillType에서 사용하는 스킬의 기본 타입을 정의합니다.
public enum SkillType
{
    Attack,
    Move,
    Buff,
    Debuff,
    Etc
}

[CreateAssetMenu(menuName = "Data/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public SkillMeta skillMeta; // 스킬 이름, 설명, 아이콘 등
    public SkillTier tier;      // 스킬 등급
    public List<SkillTag> tags; // 스킬 태그 (다중 선택 가능)

    [Header("게임 플레이 수치")]
    public int apCost;
    public int cooldown;
    public int range;
    public SkillType skillType; // 이전 리팩토링에서 추가한 스킬 타입

    [Header("빌드업 정보")]
    // 이 스킬을 배우기 위해 먼저 배워야 하는 스킬 또는 업그레이드 목록입니다.
    public List<ScriptableObject> prerequisites;

    [Header("동작 정의")]
    public List<Vector2Int> movementPattern;
    public SkillBehavior[] initialBehaviors;
    public SkillBehavior[] subTargetBehaviors;
}