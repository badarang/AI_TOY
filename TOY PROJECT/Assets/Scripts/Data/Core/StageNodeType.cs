using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// "아레나의 갈림길" 시스템에서 사용될 각 방(노드)의 종류를 정의합니다.
public enum StageNodeType
{
    Battle,      // 일반 전투
    EliteBattle, // 정예 전투
    Event,       // ? 이벤트
    Shop,        // 상점
    Rest,        // 휴식 (체력 회복)
    Boss         // 보스 전투
}
