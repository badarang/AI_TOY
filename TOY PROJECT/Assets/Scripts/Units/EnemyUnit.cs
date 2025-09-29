using UnityEngine;
using DG.Tweening;

public class EnemyUnit : UnitBase
{
    public override void Move(Vector2Int targetPos)
    {
        if (Core.Instance?.GridManager == null) return;

        Vector2Int startPos = this.position;
        
        Core.Instance.GridManager.MoveUnit(startPos, targetPos);

        Vector3 targetWorldPos = new Vector3(targetPos.x + 0.5f, 0, targetPos.y + 0.5f);
        transform.DOJump(targetWorldPos, 0.5f, 1, 0.4f);
    }

    public override void UseSkill(int skillIndex, Vector2Int targetPos)
    {
        // 현재 Goose는 별도 스킬이 없으므로 비워둠
    }
} 