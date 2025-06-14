using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width { get; private set; }
    public int height { get; private set; }

    public void GenerateGrid(StageData stageData)
    {
        width = stageData.width;
        height = stageData.height;
        // 격자 생성
    }

    public bool IsMovable(Vector2Int pos)
    {
        // 이동 가능 여부
        return true;
    }
} 