using System.Collections.Generic;
using UnityEngine;

// 업그레이드의 카테고리를 구분하기 위한 태그입니다.
// 쇼러너 AI가 플레이어의 현재 상태에 맞는 보상을 추천하는 데 사용됩니다.
public enum UpgradeTag
{
    Offense,    // 공격 관련
    Defense,    // 방어 관련
    Utility,    // 유틸리티 관련
    Mobility    // 기동성 관련
}

[CreateAssetMenu(menuName = "Data/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    [Header("기본 정보")]
    public string upgradeName;
    [TextArea] public string description;
    public Sprite icon;
    public SkillTier tier;          // 등급 (SkillTier 재사용)
    public List<UpgradeTag> tags;   // 태그

    [Header("빌드업 정보")]
    // 이 업그레이드를 얻기 위해 먼저 만족해야 하는 조건 목록입니다.
    public List<ScriptableObject> prerequisites;

    [Header("업그레이드 효과")]
    // 이 업그레이드가 적용될 때 실행될 실제 로직입니다.
    // (예: '최대 체력 +1' 로직, '선제 공격' 로직 등)
    public UpgradeBehavior behavior;
}