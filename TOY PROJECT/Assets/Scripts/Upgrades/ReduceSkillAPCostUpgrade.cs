using UnityEngine;

/// <summary>
/// 특정 스킬의 AP 코스트를 감소시키는 업그레이드
/// 예: "모든 스킬의 AP 코스트 -1"
/// </summary>
[CreateAssetMenu(menuName = "Upgrades/ReduceSkillAPCost")]
public class ReduceSkillAPCostUpgrade : UpgradeBehavior
{
    [Header("타겟 스킬 (비워두면 모든 스킬)")]
    public string targetSkillName = "";
    
    [Header("감소량")]
    public int apCostReduction = 1;
    
    public override void Apply(UnitBase playerUnit)
    {
        var skills = playerUnit.GetSkills();
        
        if (string.IsNullOrEmpty(targetSkillName))
        {
            // 모든 스킬에 적용
            for (int i = 0; i < skills.Count; i++)
            {
                playerUnit.ApplySkillUpgrade(i, "apCost", -apCostReduction);
            }
            Debug.Log($"업그레이드 적용: 모든 스킬의 AP 코스트 -{apCostReduction}");
        }
        else
        {
            // 특정 스킬에만 적용
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].data.skillMeta.nameKey == targetSkillName)
                {
                    playerUnit.ApplySkillUpgrade(i, "apCost", -apCostReduction);
                    Debug.Log($"업그레이드 적용: {targetSkillName}의 AP 코스트 -{apCostReduction}");
                    return;
                }
            }
            
            Debug.LogWarning($"스킬을 찾을 수 없습니다: {targetSkillName}");
        }
    }
}
