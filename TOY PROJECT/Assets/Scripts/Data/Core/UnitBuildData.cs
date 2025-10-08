// Assets/Scripts/Data/Runtime/UnitBuildData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 런타임에 유닛이 배운 스킬과 업그레이드를 추적합니다.
/// 로그라이크 빌드업 정보를 저장하는 클래스입니다.
/// </summary>
[Serializable]
public class UnitBuildData
{
    /// <summary>
    /// 특정 스킬에 적용된 업그레이드를 추적하는 레코드
    /// </summary>
    [Serializable]
    public class SkillUpgradeRecord
    {
        public int skillIndex;
        public string skillName;
        public List<UpgradeData> appliedUpgrades = new List<UpgradeData>();

        // 직렬화 가능한 업그레이드 이름 목록 (네트워크 전송용)
        public List<string> upgradeNames = new List<string>();
    }

    // 유닛이 현재 보유한 스킬 목록 (SkillData 참조)
    public List<SkillData> learnedSkills = new List<SkillData>();

    // 네트워크 전송을 위한 스킬 이름 목록
    public List<string> learnedSkillNames = new List<string>();

    // 각 스킬에 적용된 업그레이드 기록
    public List<SkillUpgradeRecord> skillUpgrades = new List<SkillUpgradeRecord>();

    // 유닛이 배운 패시브/액티브 업그레이드
    public List<UpgradeData> generalUpgrades = new List<UpgradeData>();

    // 네트워크 전송을 위한 업그레이드 이름 목록
    public List<string> generalUpgradeNames = new List<string>();

    /// <summary>
    /// 특정 스킬에 업그레이드를 추가합니다.
    /// </summary>
    public void AddSkillUpgrade(int skillIndex, string skillName, UpgradeData upgrade)
    {
        var record = skillUpgrades.Find(r => r.skillIndex == skillIndex);
        if (record == null)
        {
            record = new SkillUpgradeRecord
            {
                skillIndex = skillIndex,
                skillName = skillName
            };
            skillUpgrades.Add(record);
        }

        if (!record.appliedUpgrades.Contains(upgrade))
        {
            record.appliedUpgrades.Add(upgrade);
            record.upgradeNames.Add(upgrade.upgradeName);
        }
    }

    /// <summary>
    /// 특정 스킬에 적용된 모든 업그레이드를 가져옵니다.
    /// </summary>
    public List<UpgradeData> GetSkillUpgrades(int skillIndex)
    {
        var record = skillUpgrades.Find(r => r.skillIndex == skillIndex);
        return record?.appliedUpgrades ?? new List<UpgradeData>();
    }

    /// <summary>
    /// 새로운 스킬을 배웁니다.
    /// </summary>
    public void AddSkill(SkillData skillData)
    {
        if (!learnedSkills.Contains(skillData))
        {
            learnedSkills.Add(skillData);
            learnedSkillNames.Add(skillData.skillMeta.nameKey);
        }
    }

    /// <summary>
    /// 일반 업그레이드를 배웁니다.
    /// </summary>
    public void AddUpgrade(UpgradeData upgradeData)
    {
        if (!generalUpgrades.Contains(upgradeData))
        {
            generalUpgrades.Add(upgradeData);
            generalUpgradeNames.Add(upgradeData.upgradeName);
        }
    }

    /// <summary>
    /// 업그레이드 또는 스킬이 이미 배웠는지 확인합니다.
    /// </summary>
    public bool IsEquipped(ScriptableObject item)
    {
        if (item is SkillData skillData)
        {
            return learnedSkills.Contains(skillData);
        }
        else if (item is UpgradeData upgradeData)
        {
            return generalUpgrades.Contains(upgradeData);
        }
        return false;
    }

    /// <summary>
    /// 전제 조건(prerequisites)을 만족하는지 확인합니다.
    /// </summary>
    public bool MeetsPrerequisites(List<ScriptableObject> prerequisites)
    {
        if (prerequisites == null || prerequisites.Count == 0)
            return true;

        foreach (var prereq in prerequisites)
        {
            if (!IsEquipped(prereq))
                return false;
        }
        return true;
    }

    /// <summary>
    /// GameAssetDatabase를 사용하여 이름 목록으로부터 실제 데이터를 복원합니다.
    /// 네트워크 동기화 후 호출해야 합니다.
    /// </summary>
    public void RestoreFromNames(GameAssetDatabase database)
    {
        // 스킬 복원
        learnedSkills.Clear();
        foreach (var skillName in learnedSkillNames)
        {
            var skillData = database.GetSkillByName(skillName);
            if (skillData != null)
            {
                learnedSkills.Add(skillData);
            }
        }

        // 일반 업그레이드 복원
        generalUpgrades.Clear();
        foreach (var upgradeName in generalUpgradeNames)
        {
            var upgradeData = database.GetUpgradeByName(upgradeName);
            if (upgradeData != null)
            {
                generalUpgrades.Add(upgradeData);
            }
        }

        // 스킬별 업그레이드 복원
        foreach (var record in skillUpgrades)
        {
            record.appliedUpgrades.Clear();
            foreach (var upgradeName in record.upgradeNames)
            {
                var upgradeData = database.GetUpgradeByName(upgradeName);
                if (upgradeData != null)
                {
                    record.appliedUpgrades.Add(upgradeData);
                }
            }
        }
    }
}