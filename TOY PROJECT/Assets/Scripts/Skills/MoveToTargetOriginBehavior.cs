// /Assets/Scripts/Skills/MoveToTargetOriginBehavior.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SkillBehaviors/MoveToTargetOriginBehavior")]
public class MoveToTargetOriginBehavior : SkillBehavior
{
    public override float Execute(SkillContext context)
    {
        // 요청에 따라 시전자를 이동시키는 로직이 비활성화되었습니다.
        // 원래 로직은 새로운 SkillContext 프로퍼티를 사용하도록 업데이트되어 참조용으로 보관됩니다.

        // bool hitWall = context.PushedUnitHitWall ?? false;

        // if (!hitWall)
        // {
        //     DebugPrinter.DebugColor(DebugType.Unit, $"MoveToTargetOriginBehavior: 벽에 부딪히지 않음. 시전자({context.Caster.name})를 타겟의 원래 위치({context.TargetPosition})로 이동시킵니다.");
        //     DebugPrinter.DebugColor(DebugType.Unit, $"MoveToTargetOriginBehavior: 시전자 현재 위치: {context.Caster.position}");
            
        //     // PushBehavior가 먼저 실행되었으므로, context.TargetPosition은 타겟의 '원래' 위치입니다.
        //     Core.Instance.GridManager.MoveUnit(context.Caster.position, context.TargetPosition);

        //     // 논리적 위치와 함께 실제 비주얼 위치도 즉시 동기화합니다.
        //     Vector3 targetWorldPos = new Vector3(context.TargetPosition.x + 0.5f, context.Caster.transform.position.y, context.TargetPosition.y + 0.5f);
        //     context.Caster.transform.position = targetWorldPos;
            
        //     DebugPrinter.DebugColor(DebugType.Unit, $"{context.Caster.name}이(가) 적의 원래 위치로 이동합니다. 이동 후 위치: {context.Caster.position}");
        // }
        // else
        // {
        //     DebugPrinter.DebugColor(DebugType.Unit, "MoveToTargetOriginBehavior: 벽에 부딪힘. 시전자는 이동하지 않습니다.");
        // }
        DebugPrinter.DebugColor(DebugType.Unit, "MoveToTargetOriginBehavior: 시전자는 이동하지 않도록 수정됨.");
        return 0f;
    }
}
