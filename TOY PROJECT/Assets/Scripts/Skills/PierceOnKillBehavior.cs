using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/PierceOnKillBehavior")]
public class PierceOnKillBehavior : SkillBehavior
{
    public override void Execute(SkillContext context)
    {
        // 이전 Behavior에서 적이 죽었는지 확인
        if (context.KilledUnits == null || context.KilledUnits.Count == 0)
        {
            return;
        }

        // 첫 번째로 죽은 유닛을 기준으로 관통 이동 계산
        UnitBase killedUnit = context.KilledUnits[0];
        Vector2Int killedUnitPos = killedUnit.position;

        // 시전자의 원래 위치에서 -> 죽은 유닛의 위치로 향하는 벡터 계산
        Vector2Int direction = killedUnitPos - context.CasterOriginalPosition;

        // 최종 목적지 = 죽은 유닛 위치 + 방향 벡터
        Vector2Int pierceDestination = killedUnitPos + direction;

        // 목적지가 이동 가능한지 체크 (맵 범위, 다른 유닛 등)
        if (Core.Instance.GridManager.HasUnitAt(pierceDestination) || pierceDestination.x < 0 || pierceDestination.y < 0)
        {
            Debug.Log($"Pierce to {pierceDestination} is blocked.");
            return;
        }

        Debug.Log($"Enemy killed. Piercing to {pierceDestination}.");
        var caster = context.Caster;
        int moveSkillIndex = caster.GetMoveSkillIndex();
        if (moveSkillIndex != -1)
        {
            caster.UseSkill(moveSkillIndex, pierceDestination);
        }
    }
}
