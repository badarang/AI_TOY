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
        {
            Debug.Log($"[DamageBehavior] Base check failed for {caster.name} attacking {targetPos}");
            return false;
        }

        var target = Core.Instance.GridManager.GetUnitAt(targetPos);
        if (target == null)
        {
            Debug.Log($"[DamageBehavior] No target at {targetPos}");
            return false;
        }

        bool isFaction = target.factionData != caster.factionData;
        if (!isFaction)
        {
            Debug.Log($"[DamageBehavior] Target at {targetPos} is same faction as caster");
            return false;
        }

        Debug.Log($"[DamageBehavior] Can attack {targetPos}, target: {target.name}");
        return true;
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
