using UnityEngine;

/// <summary>
/// 특정 스킬의 데미지를 증가시키는 업그레이드
/// 예: "Fireball의 데미지 +5"
/// </summary>
[CreateAssetMenu(menuName = "Upgrades/IncreaseSkillDamage")]
public class IncreaseSkillDamageUpgrade : UpgradeBehavior
{
    [Header("타겟 스킬")]
    public string targetSkillName = "Fireball";
    
    [Header("증가량")]
    public int damageIncrease = 5;
    
    public override void Apply(UnitBase playerUnit)
    {
        // 플레이어의 스킬 목록에서 타겟 스킬 찾기
        var skills = playerUnit.GetSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].data.skillMeta.nameKey == targetSkillName)
            {
                playerUnit.ApplySkillUpgrade(i, "damage", damageIncrease);
                Debug.Log($"업그레이드 적용: {targetSkillName}의 데미지 +{damageIncrease}");
                return;
            }
        }
        
        Debug.LogWarning($"스킬을 찾을 수 없습니다: {targetSkillName}");
    }
}
