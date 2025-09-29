using Sirenix.OdinInspector;
using UnityEngine;

public class SkillBase : MonoBehaviour
{
    public SkillData skillData;

    [Button("Activate Skill (Test)")]
    private void ActivateForTest()
    {
        var player = FindObjectOfType<PlayerUnit>();
        if (player != null)
        { Activate(player, player.position + new Vector2Int(0, 1)); }
        else
        { Debug.LogError("Test failed. No PlayerUnit found in scene."); }
    }

    // 1단계: 스킬 최초 발동
    public virtual void Activate(UnitBase caster, Vector2Int targetPos)
    {
        if (skillData == null) { Debug.LogError("SkillData is not assigned!", this); return; }

        if (caster.ap < skillData.apCost) { Debug.Log($"{caster.name} has not enough AP."); return; }
        caster.ap -= skillData.apCost;
        Debug.Log($"{caster.name} used {skillData.name}. AP Cost: {skillData.apCost}, Remaining: {caster.ap}");

        var context = new SkillContext(caster, targetPos);

        // TurnManager에 현재 스킬 정보 저장 (일시정지 대비)
        Core.Instance.TurnManager.PausedSkill = this;
        Core.Instance.TurnManager.PausedSkillContext = context;

        if (skillData.initialBehaviors != null)
        {
            foreach (var behavior in skillData.initialBehaviors)
            {
                if (behavior != null) behavior.Execute(context);
            }
        }
        
        // 만약 subTargetBehaviors가 없다면, 즉시 턴 종료 로직으로 연결
        if (skillData.subTargetBehaviors == null || skillData.subTargetBehaviors.Length == 0)
        {
            Core.Instance.TurnManager.PausedSkill = null;
            Core.Instance.TurnManager.PausedSkillContext = null;
            Core.Instance.TurnManager.EndTurn();
        }
    }

    // 2단계: 추가 대상 선택 후 발동
    public virtual void ActivateSubTarget(UnitBase targetUnit)
    {
        var context = Core.Instance.TurnManager.PausedSkillContext;
        if (context == null) { Debug.LogError("No paused skill context found!"); return; }

        // 컨텍스트에 2단계 타겟 정보 추가 (이 필드는 SkillContext에 추가 필요)
        context.SubTargetUnit = targetUnit;

        if (skillData.subTargetBehaviors != null)
        {
            foreach (var behavior in skillData.subTargetBehaviors)
            {
                if (behavior != null) behavior.Execute(context);
            }
        }

        // 모든 스킬 단계가 끝났으므로 정보 초기화 및 턴 종료
        Core.Instance.TurnManager.PausedSkill = null;
        Core.Instance.TurnManager.PausedSkillContext = null;
        Core.Instance.TurnManager.EndTurn();
    }
} 