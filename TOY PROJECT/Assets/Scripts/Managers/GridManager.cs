using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    private int width { get; set; }
    private int height { get; set; }
    public LayerMask unitLayer;

    [Header("Datas")]
    private Dictionary<Vector2Int, UnitBase> unitPositions = new Dictionary<Vector2Int, UnitBase>();

    private List<GameObject> gridLines = new List<GameObject>();
    private Vector2Int? hoveredCell = null;
    private Vector2Int? selectedCell = null;
    private UnitBase selectedUnit = null;

    public Material highlightMat;
    private GameObject highlightQuad;
    private Material defaultHighlightMat;

    void Start()
    {
        if (Core.Instance?.InputManager != null)
        {
            Core.Instance.InputManager.OnClick += HandleClick;
        }
    }

    void Update()
    {
        UpdateCellHover();
    }

    void OnDestroy()
    {
        if (Core.Instance?.InputManager != null)
        {
            Core.Instance.InputManager.OnClick -= HandleClick;
        }
    }

    private void HandleClick(Vector2 clickPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(clickPosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            int x = Mathf.FloorToInt(hitPoint.x);
            int z = Mathf.FloorToInt(hitPoint.z);

            if (x >= 0 && x < width && z >= 0 && z < height)
            {
                TrySelectUnitAtCell(x, z);
            }
            else
            {
                // 그리드 밖을 클릭하면 deselect
                ClearSelection();
            }
        }
        else
        {
            // Ray가 ground plane과 교차하지 않으면 deselect
            ClearSelection();
        }
    }

    public void GenerateGrid(StageData stageData)
    {
        width = stageData.width;
        height = stageData.height;
        ClearGridLines();
        hoveredCell = null;
        selectedCell = null;
        selectedUnit = null;
        if (highlightQuad != null) Destroy(highlightQuad);

        for (int x = 0; x <= width; x++)
        {
            CreateGridLine(new Vector3(x, 0.01f, 0), new Vector3(x, 0.01f, height));
        }
        for (int z = 0; z <= height; z++)
        {
            CreateGridLine(new Vector3(0, 0.01f, z), new Vector3(width, 0.01f, z));
        }
    }

    private void UpdateCellHover()
    {
        Vector2 inputPosition = GetInputPosition();
        if (inputPosition == Vector2.zero) return;

        Ray ray = Camera.main.ScreenPointToRay(inputPosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            int x = Mathf.FloorToInt(hitPoint.x);
            int z = Mathf.FloorToInt(hitPoint.z);

            if (x >= 0 && x < width && z >= 0 && z < height)
            {
                Vector2Int cellPos = new Vector2Int(x, z);

                if (unitPositions.ContainsKey(cellPos) && unitPositions[cellPos] != null)
                {
                    Vector2Int newHoveredCell = cellPos;

                    if (hoveredCell != newHoveredCell)
                    {
                        hoveredCell = newHoveredCell;
                    }

                    ShowHighlight(x, z);
                }
                else
                {
                    if (hoveredCell.HasValue)
                    {
                        hoveredCell = null;
                        HideHighlight();
                    }
                }
            }
            else
            {
                if (hoveredCell.HasValue)
                {
                    hoveredCell = null;
                    HideHighlight();
                }
            }
        }
    }

    private Vector2 GetInputPosition()
    {
        if (Core.Instance?.InputManager != null)
        {
            return Core.Instance.InputManager.CurrentInputPosition;
        }
        return Vector2.zero;
    }

    void ShowHighlight(int x, int z)
    {
        if (highlightQuad == null)
        {
            highlightQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlightQuad.name = "GridHighlight";
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
            rend.material.color = new Color(0.3f, 0.6f, 1f, 0.3f);
    }

    void HideHighlight()
    {
        if (highlightQuad != null)
        {
            highlightQuad.SetActive(false);
        }
    }

    void TrySelectUnitAtCell(int x, int z)
    {
        Vector2Int cellPos = new Vector2Int(x, z);

        if (unitPositions.ContainsKey(cellPos) && unitPositions[cellPos] != null)
        {
            UnitBase unit = unitPositions[cellPos];

            if (!(unit is EnemyUnit))
            {
                // 이미 선택된 유닛을 다시 클릭하면 deselect
                if (selectedUnit == unit)
                {
                    ClearSelection();
                }
                else
                {
                    // 다른 유닛을 선택
                    if (selectedUnit != null)
                        selectedUnit.Deselect();
                    selectedUnit = unit;
                    selectedUnit.Select();
                    selectedCell = cellPos;
                }
            }
            else
            {
                // 적 유닛을 클릭하면 deselect
                ClearSelection();
            }
        }
        else
        {
            // 빈 칸을 클릭하면 deselect
            ClearSelection();
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

    public void RegisterUnit(UnitBase unit, Vector2Int gridPos)
    {
        unitPositions[gridPos] = unit;
        unit.position = gridPos;
    }

    public void UnregisterUnit(Vector2Int gridPos)
    {
        if (unitPositions.ContainsKey(gridPos))
        {
            unitPositions.Remove(gridPos);
        }
    }

    public void MoveUnit(Vector2Int from, Vector2Int to)
    {
        if (unitPositions.ContainsKey(from))
        {
            UnitBase unit = unitPositions[from];
            unitPositions.Remove(from);
            unitPositions[to] = unit;
            unit.position = to;
        }
    }

    public UnitBase GetUnitAt(Vector2Int gridPos)
    {
        return unitPositions.ContainsKey(gridPos) ? unitPositions[gridPos] : null;
    }

    public bool HasUnitAt(Vector2Int gridPos)
    {
        return unitPositions.ContainsKey(gridPos) && unitPositions[gridPos] != null;
    }

    public UnitBase GetSelectedUnit()
    {
        return selectedUnit;
    }

    public Vector2Int? GetSelectedCell()
    {
        return selectedCell;
    }

    public Vector2Int? GetHoveredCell()
    {
        return hoveredCell;
    }

    public void ClearSelection()
    {
        if (selectedUnit != null)
            selectedUnit.Deselect();
        selectedUnit = null;
        selectedCell = null;
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