using UnityEngine;

// 업그레이드의 실제 동작을 정의하는 기반 클래스입니다.
// ScriptableObject로 만들어 에디터에서 생성 및 수정이 용이하도록 합니다.
public abstract class UpgradeBehavior : ScriptableObject
{
    // 모든 구체적인 업그레이드 로직은 이 Apply 메소드를 구현해야 합니다.
    // 이 메소드는 업그레이드가 적용되는 시점에 단 한 번 호출됩니다.
    // playerUnit을 인자로 받아, 해당 유닛의 영구적인 스탯이나 상태를 변경합니다.
    public abstract void Apply(UnitBase playerUnit);
}
