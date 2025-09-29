using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/DamageAndPierceBehavior")]
public class DamageAndPierceBehavior : SkillBehavior
{
    public int damage = 1;

    public override void Execute(SkillContext context)
    {
        UnitBase targetUnit = context.SubTargetUnit;
        if (targetUnit == null)
        {
            Debug.LogError("SubTargetUnit is not set in the context!");
            return;
        }

        Debug.Log($"Executing {targetUnit.name} at {targetUnit.position}.");

        // 1. 데미지 적용
        targetUnit.hp -= damage;

        // 2. 관통 이동 계산 및 실행
        if (targetUnit.hp <= 0)
        {
            Vector2Int killedUnitPos = targetUnit.position;
            // 방향 계산의 기준을 CasterOriginalPosition에서 Caster의 현재 위치로 변경
            Vector2Int direction = killedUnitPos - context.Caster.position;
            Vector2Int pierceDestination = killedUnitPos + direction;

            // Grid에서 이전 유닛 정보 제거
            Core.Instance.GridManager.UnregisterUnit(killedUnitPos);
            Destroy(targetUnit.gameObject);

            // 관통 이동
            if (!Core.Instance.GridManager.HasUnitAt(pierceDestination))
            { 
                Debug.Log($"Enemy killed. Piercing to {pierceDestination}.");
                context.Caster.Move(pierceDestination);
            }
            else
            {
                Debug.Log($"Pierce to {pierceDestination} is blocked.");
            }
        }
    }
}
