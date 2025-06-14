using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public int width { get; private set; }
    public int height { get; private set; }

    private List<GameObject> gridLines = new List<GameObject>();

    public void GenerateGrid(StageData stageData)
    {
        width = stageData.width;
        height = stageData.height;
        ClearGridLines();

        // 세로선
        for (int x = 0; x <= width; x++)
        {
            CreateGridLine(new Vector3(x, 0.01f, 0), new Vector3(x, 0.01f, height));
        }
        // 가로선
        for (int z = 0; z <= height; z++)
        {
            CreateGridLine(new Vector3(0, 0.01f, z), new Vector3(width, 0.01f, z));
        }
    }

    void CreateGridLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.parent = this.transform;
        var lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = Color.gray;
        lr.useWorldSpace = true;
        gridLines.Add(lineObj);
    }

    void ClearGridLines()
    {
        foreach (var go in gridLines)
        {
            if (go != null) Destroy(go);
        }
        gridLines.Clear();
    }

    public bool IsMovable(Vector2Int pos)
    {
        // 이동 가능 여부
        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(new Vector3(x, 0, 0), new Vector3(x, 0, height));
        }
        for (int z = 0; z <= height; z++)
        {
            Gizmos.DrawLine(new Vector3(0, 0, z), new Vector3(width, 0, z));
        }
    }
} 