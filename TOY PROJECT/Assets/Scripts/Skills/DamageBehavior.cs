// /Assets/Scripts/Skills/DamageBehavior.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/DamageBehavior")]
public class DamageBehavior : SkillBehavior
{
    public int damage = 10;

    public override void Execute(SkillContext context)
    {
        UnitBase target = Core.Instance.GridManager.GetUnitAt(context.TargetPosition);
        if (target != null)
        {
            Debug.Log($"{target.name}에게 {damage}의 피해를 입혔습니다!");
            target.TakeDamage(damage);
        }
    }
}