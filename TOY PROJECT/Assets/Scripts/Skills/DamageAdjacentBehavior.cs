using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "SkillBehaviors/DamageAdjacentBehavior")]
public class DamageAdjacentBehavior : SkillBehavior
{
    public int damage = 1;

    public override void Execute(SkillContext context)
    {
        Vector2Int casterPos = context.Caster.position;
        List<UnitBase> damagedUnits = new List<UnitBase>();
        List<UnitBase> killedUnits = new List<UnitBase>();

        // 8방향 탐색
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Skip self

                Vector2Int checkPos = casterPos + new Vector2Int(x, y);
                UnitBase targetUnit = Core.Instance.GridManager.GetUnitAt(checkPos);

                // 적 유닛이 있다면 데미지 처리
                if (targetUnit != null && targetUnit is EnemyUnit)
                {
                    Debug.Log($"Found enemy {targetUnit.name} at {checkPos}. Applying {damage} damage.");
                    targetUnit.hp -= damage; // 실제 데미지 적용
                    damagedUnits.Add(targetUnit);

                    if (targetUnit.hp <= 0)
                    {
                        Debug.Log($"{targetUnit.name} was killed.");
                        killedUnits.Add(targetUnit);
                        // TODO: 유닛 사망 처리 로직 호출 (예: Grid에서 제거, 오브젝트 파괴 등)
                        Core.Instance.GridManager.UnregisterUnit(checkPos);
                        Destroy(targetUnit.gameObject);
                    }
                }
            }
        }

        // 다음 Behavior에서 사용될 수 있도록 컨텍스트에 정보 저장
        context.DamagedUnits = damagedUnits;
        context.KilledUnits = killedUnits;
    }
}
