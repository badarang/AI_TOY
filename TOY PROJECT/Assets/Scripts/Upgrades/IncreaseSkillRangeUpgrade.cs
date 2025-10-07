using UnityEngine;

/// <summary>
/// 특정 스킬의 사거리를 증가시키는 업그레이드
/// 예: "Dash의 사거리 +1"
/// </summary>
[CreateAssetMenu(menuName = "Upgrades/IncreaseSkillRange")]
public class IncreaseSkillRangeUpgrade : UpgradeBehavior
{
    [Header("타겟 스킬")]
    public string targetSkillName = "Dash";
    
    [Header("증가량")]
    public int rangeIncrease = 1;
    
    public override void Apply(UnitBase playerUnit)
    {
        var skills = playerUnit.GetSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].data.skillMeta.nameKey == targetSkillName)
            {
                playerUnit.ApplySkillUpgrade(i, "range", rangeIncrease);
                Debug.Log($"업그레이드 적용: {targetSkillName}의 사거리 +{rangeIncrease}");
                return;
            }
        }
        
        Debug.LogWarning($"스킬을 찾을 수 없습니다: {targetSkillName}");
    }
}
