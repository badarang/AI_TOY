using System.Collections.Generic;
using UnityEngine;

// "아레나의 갈림길" 맵을 구성하는 개별 노드(방)의 데이터입니다.
[CreateAssetMenu(menuName = "Data/Map Node Data")]
public class MapNodeData : ScriptableObject
{
    [Header("노드 정보")]
    public StageNodeType nodeType;

    public StageData stageData;

    // 이 노드에서 발생할 전투의 종류입니다.
    // nodeType이 'Battle' 또는 'Boss'일 경우에만 의미가 있습니다.
    public EncounterType encounterType;

    [Header("연결 정보")]
    // 이 노드를 클리어한 후, 다음으로 진행할 수 있는 노드들의 목록입니다.
    public List<MapNodeData> nextNodes;

    // TODO: 팬심 예측 시스템을 위한 필드 추가
    // public int estimatedFanGain;
}
