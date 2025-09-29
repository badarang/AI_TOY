// /Assets/Scripts/Utils/BlackboardKeys.cs

// SkillContext의 블랙보드에서 사용할 키들을 정의하는 열거형입니다.
// 문자열을 직접 사용하는 것보다 오타를 방지하고 코드 자동완성을 활용할 수 있어 안전합니다.
public enum BlackboardKeys
{
    // 유닛 관련
    TargetUnit,         // 스킬의 주된 대상이 되는 유닛 (UnitBase)

    // 상태 플래그
    PushedUnitHitWall,  // PushBehavior에 의해 밀려난 유닛이 벽에 부딪혔는지 여부 (bool)
}
