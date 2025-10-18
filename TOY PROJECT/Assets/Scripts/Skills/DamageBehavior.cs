// /Assets/Scripts/Skills/DamageBehavior.cs

using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/DamageBehavior")]
public class DamageBehavior : SkillBehavior
{
    public int damage = 10;

    public override bool CanExecute(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        if (!base.CanExecute(caster, targetPos, skill))
            return false;

        var target = Core.Instance.GridManager.GetUnitAt(targetPos);
        if (target == null)
            return false;

        return target.factionData != caster.factionData;
    }

    public override UniTask ExecuteAsync(UnitBase caster, Vector2Int targetPos, Skill skill)
    {
        int finalDamage = skill.GetModifiedValue("damage", damage);
        UnitBase target = Core.Instance.GridManager.GetUnitAt(targetPos);

        if (target != null)
        {
            caster.PerformAttackMotion(
                targetPos,
                () =>
                {
                    DebugPrinter.LogColor(
                        LogType.Action,
                        $"{target.name}에게 {finalDamage}의 피해를 입혔습니다!"
                    );
                    target.TakeDamage(finalDamage);

                    float speedMultiplier =
                        target.unitData != null ? target.unitData.animationSpeedMultiplier : 1.0f;
                    float flashDuration = UnitAnimationConfig.GetFlashDuration(speedMultiplier);
                    target.PlayFlashEffect(UnitAnimationConfig.FLASH_COLOR, flashDuration);
                }
            );
        }
        return UniTask.CompletedTask;
    }
}
