using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

public class PlayerUnit : UnitBase
{
    // 인스펙터에서 관리하는 대신, 런타임에 생성되는 스킬 목록
    private List<SkillBase> runtimeSkills;

    // 외부에서 스킬 유무를 확인할 수 있는 프로퍼티
    public bool HasSkills => runtimeSkills != null && runtimeSkills.Count > 0;

    private void Awake()
    {
        SetupSkills();
    }

    // UnitData를 기반으로 SkillBase 컴포넌트를 동적으로 생성하고 설정
    private void SetupSkills()
    {
        // 기존에 붙어있을지 모르는 SkillBase 컴포넌트들을 모두 제거
        var existingSkills = GetComponents<SkillBase>();
        foreach (var skill in existingSkills)
        {
            Destroy(skill);
        }

        runtimeSkills = new List<SkillBase>();

        if (unitData != null && unitData.skills != null)
        {
            foreach (var skillData in unitData.skills)
            {
                if (skillData != null)
                {
                    // SkillBase 컴포넌트를 게임오브젝트에 추가
                    var newSkill = gameObject.AddComponent<SkillBase>();
                    // SkillData를 할당
                    newSkill.skillData = skillData;
                    runtimeSkills.Add(newSkill);
                }
            }
        }
    }

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
        if (skillIndex < 0 || skillIndex >= runtimeSkills.Count || runtimeSkills[skillIndex] == null)
        { 
            Debug.LogError($"Invalid skill index: {skillIndex} or skill not assigned.");
            return;
        }

        runtimeSkills[skillIndex].Activate(this, targetPos);
    }

    public void DistributeSkills()
    {
        // 이 함수는 이제 SetupSkills로 대체되었으므로 비워두거나 삭제 가능
    }
}
