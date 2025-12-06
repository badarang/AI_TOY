using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/DamageBehavior")]
public class DamageBehavior : SkillBehavior
{
    public int damage = 10;

    public override bool CanExecute(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        if (!base.CanExecute(caster, targetPos, skill))
        {
            return false;
        }

        var attackable = Core.Instance.GridManager.GetAttackableAt(targetPos);
        if (attackable == null)
        {
            return false;
        }

        // 유닛인 경우, 적대적인지 확인
        if (attackable is UnitBase targetUnit)
        {
            return targetUnit.factionData != caster.factionData;
        }

        // 포탈인 경우, 전투가 끝났는지 확인
        if (attackable is Portal)
        {
            return Core.Instance.TurnManager.BattleEnded;
        }
        
        return false;
    }

    public override UniTask ExecuteAsync(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        var attackable = Core.Instance.GridManager.GetAttackableAt(targetPos);
        if (attackable == null)
        {
            return UniTask.CompletedTask;
        }

        caster.PerformAttackMotion(
            targetPos,
            () =>
            {
                if (attackable is UnitBase targetUnit)
                {
                    // 유닛 공격
                    int finalDamage = skill.GetModifiedValue("damage", damage);
                    targetUnit.TakeDamage(finalDamage);
                    
                    float speedMultiplier = targetUnit.unitData != null ? targetUnit.unitData.animationSpeedMultiplier : 1.0f;
                    float flashDuration = UnitAnimationConfig.GetFlashDuration(speedMultiplier);
                    targetUnit.PlayFlashEffect(UnitAnimationConfig.FLASH_COLOR, flashDuration);
                }
                else if (attackable is Portal targetPortal)
                {
                    // 포탈 공격
                    targetPortal.TakeDamage(1);
                }
            }
        );

        return UniTask.CompletedTask;
    }
}
