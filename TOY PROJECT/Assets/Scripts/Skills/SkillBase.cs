using UnityEngine;

// SkillData와 SkillBehavior의 도입으로, 이 클래스는 더 이상 적극적으로 사용되지 않는 레거시 클래스가 될 예정입니다.
// 점진적인 리팩토링을 위해 임시로 유지됩니다.
public abstract class SkillBase : MonoBehaviour
{
    public SkillData skillData; // 모든 스킬은 이제 SkillData를 참조해야 합니다.

    protected virtual void Awake()
    {
        if (skillData == null) { Debug.LogError("SkillData is not assigned!", this); return; }
    }

    // 스킬 사용의 주 로직은 이제 UnitBase.UseSkill()로 이전되었습니다.
    // 이 Execute 메소드는 특정 시나리오(예: 서브 타겟팅)에서만 제한적으로 사용될 수 있습니다.
    public virtual float Execute(SkillContext context)
    {
        float totalDuration = 0f;

        // Initial Behaviors 실행
        if (skillData.initialBehaviors != null)
        {
            foreach (var behavior in skillData.initialBehaviors)
            {
                if (behavior != null) totalDuration += behavior.Execute(context);
            }
        }

        // Sub-Targeting 로직 (TurnManager가 직접 처리)
        if (skillData.subTargetBehaviors != null && skillData.subTargetBehaviors.Length > 0)
        {
            // TurnManager가 이 상태를 관리하므로, SkillBase에서 직접 상태를 설정하는 코드를 제거합니다.
            // Core.Instance.TurnManager.PausedSkillData = skillData;
            // Core.Instance.TurnManager.PausedSkillContext = context;
            // Core.Instance.TurnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingSkillSubTarget);
            Debug.Log("Sub-targeting skill used. TurnManager will handle the state.");
        }

        return totalDuration;
    }
}
