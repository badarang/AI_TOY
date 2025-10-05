using System.Collections.Generic;
using UnityEngine;

// "아레나의 갈림길" 맵 시스템을 총괄하는 매니저입니다.
public class MapManager : MonoBehaviour
{
    [Header("맵 데이터")]
    [SerializeField] private MapData currentMapData;

    private MapNodeData currentNode;
    public MapNodeData CurrentNode => currentNode;

    /// <summary>
    /// 게임 시작 시, 지정된 맵 데이터로 모험을 시작합니다.
    /// </summary>
    public void LoadMap(MapData mapData)
    {
        currentMapData = mapData;
        if (currentMapData != null && currentMapData.startingNode != null)
        {
            currentNode = currentMapData.startingNode;
            Debug.Log($"맵 로드 완료. 시작 노드: {currentNode.name}");
        }
        else
        {
            Debug.LogError("맵 데이터를 로드할 수 없거나, 시작 노드가 지정되지 않았습니다.");
        }
    }

    /// <summary>
    /// 현재 노드에 연결된 다음 갈림길 노드들의 목록을 반환합니다.
    /// </summary>
    public List<MapNodeData> GetNextNodes()
    {
        if (currentNode != null)
        {
            return currentNode.nextNodes;
        }
        return new List<MapNodeData>();
    }

    /// <summary>
    /// 플레이어가 선택한 다음 노드로 현재 위치를 이동시킵니다.
    /// </summary>
    public void MoveToNode(MapNodeData nextNode)
    {
        if (currentNode.nextNodes.Contains(nextNode))
        {
            currentNode = nextNode;
            Debug.Log($"다음 노드로 이동: {currentNode.name} (타입: {currentNode.nodeType})");
        }
        else
        {
            Debug.LogError($"{nextNode.name}은(는) 현재 노드({currentNode.name})에 연결되어 있지 않습니다.");
        }
    }
}
