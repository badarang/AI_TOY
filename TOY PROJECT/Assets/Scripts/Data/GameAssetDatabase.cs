// Assets/Scripts/Data/GameAssetDatabase.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임에 존재하는 모든 스킬, 업그레이드, 유닛 애셋을 담아두는 중앙 데이터베이스입니다.
/// "쇼러너 AI"는 이 데이터베이스를 참조하여 보상 목록을 생성합니다.
/// Network 환경에서는 서버와 클라이언트 모두 이 DB를 참조할 수 있어야 합니다.
/// </summary>
[CreateAssetMenu(menuName = "Data/Game Asset Database")]
public class GameAssetDatabase : ScriptableObject
{
    [Header("스킬 라이브러리")]
    public List<SkillData> allSkills;

    [Header("업그레이드 라이브러리")]
    public List<UpgradeData> allUpgrades;

    [Header("유닛 라이브러리")]
    public List<UnitData> allUnits;

    [Header("스테이지 라이브러리")]
    public List<StageData> allStages;

    #region 조회 메서드들
    
    /// <summary>
    /// UnitType으로 UnitData를 찾습니다.
    /// </summary>
    public UnitData GetUnitData(UnitType unitType)
    {
        return allUnits.Find(u => u.unitType == unitType);
    }

    /// <summary>
    /// 스킬 이름으로 SkillData를 찾습니다.
    /// </summary>
    public SkillData GetSkillByName(string skillName)
    {
        return allSkills.Find(s => s.skillMeta.nameKey == skillName);
    }

    /// <summary>
    /// 업그레이드 이름으로 UpgradeData를 찾습니다.
    /// </summary>
    public UpgradeData GetUpgradeByName(string upgradeName)
    {
        return allUpgrades.Find(u => u.upgradeName == upgradeName);
    }

    /// <summary>
    /// 스테이지 이름으로 StageData를 찾습니다.
    /// </summary>
    public StageData GetStageByName(string stageName)
    {
        return allStages.Find(s => s.name == stageName);
    }

    #endregion
}