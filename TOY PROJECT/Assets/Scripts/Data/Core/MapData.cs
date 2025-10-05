using System.Collections.Generic;
using UnityEngine;

// "아레나의 갈림길" 맵 전체의 구조를 정의하는 데이터입니다.
// 하데스의 '탈출 시도' 하나에 해당하는 전체 맵과 같습니다.
[CreateAssetMenu(menuName = "Data/Map Data")]
public class MapData : ScriptableObject
{
    [Header("맵 시작 정보")]
    // 이 맵의 시작 지점이 되는 노드입니다.
    public MapNodeData startingNode;

    // 맵의 모든 노드들을 담아두는 리스트 (에디터에서 관리를 용이하게 하기 위함)
    // 실제 게임 로직은 startingNode부터 시작하여 연결된 노드들을 따라갑니다.
    public List<MapNodeData> allNodesInMap;
}
