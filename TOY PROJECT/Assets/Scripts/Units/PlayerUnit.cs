using UnityEngine;

public class PlayerUnit : UnitBase
{
    public SkillBase[] skills;

    public override void Move(Vector2Int targetPos)
    {
        // 이동 구현
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