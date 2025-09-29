using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/MoveBehavior")]
public class MoveBehavior : SkillBehavior
{
    public override void Execute(SkillContext context)
    {
        // PlayerUnit의 Move 메소드를 호출하여 이동 실행
        // PlayerUnit.Move는 시각적 이동(애니메이션)과 GridManager 데이터 업데이트를 모두 처리
        context.Caster.Move(context.TargetPosition);
        Debug.Log($"{context.Caster.name} moved to {context.TargetPosition}");
    }
}
