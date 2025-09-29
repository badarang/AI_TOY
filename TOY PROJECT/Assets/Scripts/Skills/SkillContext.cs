using UnityEngine;
using System.Collections.Generic;

// SkillBehavior 실행에 필요한 모든 정보를 담는 컨텍스트 클래스
public class SkillContext
{
    public UnitBase Caster { get; private set; }
    public Vector2Int CasterOriginalPosition { get; private set; }
    public Vector2Int TargetPosition { get; private set; }

    // 2단계 타겟 정보
    public UnitBase SubTargetUnit { get; set; }

    // Behavior 사이의 데이터 전달을 위해 사용
    public List<UnitBase> DamagedUnits { get; set; }
    public List<UnitBase> KilledUnits { get; set; }
    public List<UnitBase> HighlightedTargets { get; set; }

    // 어떤 데이터든 저장할 수 있는 범용 블랙보드입니다.
    public Dictionary<BlackboardKeys, object> blackboard { get; private set; }

    public SkillContext(UnitBase caster, Vector2Int targetPosition)
    {
        Caster = caster;
        CasterOriginalPosition = caster.position;
        TargetPosition = targetPosition;
        DamagedUnits = new List<UnitBase>();
        KilledUnits = new List<UnitBase>();
        HighlightedTargets = new List<UnitBase>();
        blackboard = new Dictionary<BlackboardKeys, object>(); // 블랙보드 초기화
    }
}