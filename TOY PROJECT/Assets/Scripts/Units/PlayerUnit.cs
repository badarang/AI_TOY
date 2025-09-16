using UnityEngine;
using DG.Tweening;

public class PlayerUnit : UnitBase
{
    public SkillBase[] skills;

    public override void Move(Vector2Int targetPos)
    {
        if (Core.Instance?.GridManager == null) return;

        Vector2Int startPos = this.position;
        
        // Update grid data immediately so other systems know the new position
        Core.Instance.GridManager.MoveUnit(startPos, targetPos);

        // Animate the visual movement
        Vector3 targetWorldPos = new Vector3(targetPos.x + 0.5f, 0, targetPos.y + 0.5f);
        
        // Use DOTween for a nice jump animation
        transform.DOJump(targetWorldPos, 0.5f, 1, 0.4f);
    }

    public override void UseSkill(int skillIndex, Vector2Int targetPos)
    {
        // 스킬 사용 구현
    }

    public void DistributeSkills()
    {
        // 스킬 분배 로직
    }
}