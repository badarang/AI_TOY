using UnityEngine;

// 스킬을 구성하는 개별 동작(이동, 데미지, 버프 등)의 기반이 될 추상 클래스
// ScriptableObject로 만들어 에디터에서 생성 및 수정이 용이하도록 함
public abstract class SkillBehavior : ScriptableObject
{
    // 모든 구체적인 행동들은 이 Execute 메소드를 구현해야 함
    // context 객체를 통해 스킬 시전자, 타겟, 게임 매니저 등 필요한 모든 정보에 접근
    // 행동에 애니메이션 등 시간이 소요되는 경우, 해당 시간을 float으로 반환해야 합니다.
    public abstract float Execute(SkillContext context);
}
