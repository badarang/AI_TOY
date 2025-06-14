using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public int width { get; private set; }
    public int height { get; private set; }

    private List<GameObject> gridLines = new List<GameObject>();
    private Vector2Int? hoveredCell = null;
    private Vector2Int? selectedCell = null;
    private UnitBase selectedUnit = null;

    public LayerMask unitLayer;
    public Material highlightMat;
    private GameObject highlightQuad;
    private Material defaultHighlightMat;

    public void GenerateGrid(StageData stageData)
    {
        width = stageData.width;
        height = stageData.height;
        ClearGridLines();
        hoveredCell = null;
        selectedCell = null;
        selectedUnit = null;
        if (highlightQuad != null) Destroy(highlightQuad);

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

    void Update()
    {
        // 마우스 위치에서 격자 셀 계산
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0))
        {
            Vector3 point = hit.point;
            int x = Mathf.FloorToInt(point.x);
            int z = Mathf.FloorToInt(point.z);
            if (x >= 0 && x < width && z >= 0 && z < height)
            {
                hoveredCell = new Vector2Int(x, z);
                ShowHighlight(x, z);
                // 클릭 시 유닛 선택
                if (Input.GetMouseButtonDown(0))
                {
                    TrySelectUnitAtCell(x, z);
                }
            }
            else
            {
                hoveredCell = null;
                HideHighlight();
            }
        }
        else
        {
            hoveredCell = null;
            HideHighlight();
        }
    }

    void ShowHighlight(int x, int z)
    {
        if (highlightQuad == null)
        {
            highlightQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlightQuad.transform.localScale = new Vector3(1, 1, 1);
            highlightQuad.transform.rotation = Quaternion.Euler(90, 0, 0);
            highlightQuad.GetComponent<Collider>().enabled = false;
            defaultHighlightMat = highlightQuad.GetComponent<Renderer>().material;
        }
        highlightQuad.transform.position = new Vector3(x + 0.5f, 0.02f, z + 0.5f);
        highlightQuad.SetActive(true);
        var rend = highlightQuad.GetComponent<Renderer>();
        if (highlightMat != null)
            rend.material = highlightMat;
        else
            rend.material.color = new Color(0.3f, 0.6f, 1f, 0.3f); // 푸른빛
    }

    void HideHighlight()
    {
        if (highlightQuad != null)
            highlightQuad.SetActive(false);
    }

    void TrySelectUnitAtCell(int x, int z)
    {
        Vector3 cellCenter = new Vector3(x + 0.5f, 0.5f, z + 0.5f);
        Collider[] hits = Physics.OverlapSphere(cellCenter, 0.3f, unitLayer);
        if (hits.Length > 0)
        {
            Debug.Log($"Cell ({x}, {z}) has {hits.Length} units.");
            UnitBase unit = hits[0].GetComponent<UnitBase>();
            if (unit != null && !(unit is EnemyUnit))
            {
                if (selectedUnit != null)
                    selectedUnit.Deselect();
                selectedUnit = unit;
                selectedUnit.Select();
                selectedCell = new Vector2Int(x, z);
            }
            else
            {
                // EnemyUnit이거나 Unit이 아니면 선택 해제
                if (selectedUnit != null)
                    selectedUnit.Deselect();
                selectedUnit = null;
                selectedCell = null;
            }
        }
        else
        {
            if (selectedUnit != null)
                selectedUnit.Deselect();
            selectedUnit = null;
            selectedCell = null;
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