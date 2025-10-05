using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    [Header("Grid Settings")]
    private int width { get; set; }
    private int height { get; set; }
    public LayerMask unitLayer;
    public Material gridLineMaterial;

    [Header("Datas")]
    private Dictionary<Vector2Int, UnitBase> unitPositions = new Dictionary<Vector2Int, UnitBase>();

    private List<GameObject> gridLines = new List<GameObject>();
    private Vector2Int? hoveredCell = null;

    public Material highlightMat;
    private GameObject highlightQuad;

    void Update()
    {
        UpdateCellHover();
    }

    public void GenerateGrid(StageData stageData)
    {
        width = stageData.width;
        height = stageData.height;
        
        ClearGrid(); // This now handles all cleanup

        for (int x = 0; x <= width; x++)
        {
            CreateGridLine(new Vector3(x, 0.01f, 0), new Vector3(x, 0.01f, height));
        }
        for (int z = 0; z <= height; z++)
        {
            CreateGridLine(new Vector3(0, 0.01f, z), new Vector3(width, 0.01f, z));
        }
    }

    /// <summary>
    /// Clears all grid data and visual elements, preparing for a new stage.
    /// </summary>
    public void ClearGrid()
    {
        // Clear data
        unitPositions.Clear();
        hoveredCell = null;
        // NOTE: TurnManager responsibility was removed from here.
        
        // Clear visual elements
        ClearGridLines();
        ClearAllHighlights(); 
        if (highlightQuad != null)
        {
            Destroy(highlightQuad);
            highlightQuad = null;
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
                    if (hoveredCell != cellPos)
                    {
                        hoveredCell = cellPos;
                        ShowHighlight(x, z);
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
            highlightQuad.transform.rotation = Quaternion.Euler(90, 0, 0);
            highlightQuad.GetComponent<Collider>().enabled = false;
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

    public void TrySelectUnitAtCell(Vector2Int cellPos)
    {
        if (unitPositions.ContainsKey(cellPos) && unitPositions[cellPos] != null)
        {
            UnitBase unit = unitPositions[cellPos];

            if (unit is PlayerUnit)
            {
                Core.Instance.TurnManager.SelectPlayerUnit(unit as PlayerUnit);
            }
            else
            {
                 Core.Instance.TurnManager.ClearSelection();
            }
        }
        else
        {
             Core.Instance.TurnManager.ClearSelection();
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
        lr.material = gridLineMaterial;
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

    public List<UnitBase> GetAllUnits()
    {
        return unitPositions.Values.ToList();
    }

    public bool HasUnitAt(Vector2Int gridPos)
    {
        return unitPositions.ContainsKey(gridPos) && unitPositions[gridPos] != null;
    }

    public void ClearAllHighlights()
    {
        HideHighlight();
        ClearMovableHighlights();
        ClearTargetHighlights();
    }

    [Header("Movable Tile Highlight")]
    public Material movableTileMaterial;
    private List<GameObject> movableTileHighlights = new List<GameObject>();

    [Header("Target Highlight")]
    public Material executableTargetMaterial;
    private List<GameObject> targetHighlights = new List<GameObject>();

    public void HighlightMovableTiles(List<Vector2Int> tilesToHighlight)
    {
        ClearMovableHighlights();
        foreach (var tile in tilesToHighlight)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlight.name = $"MovableHighlight_{tile.x}_{tile.y}";
            highlight.transform.position = new Vector3(tile.x + 0.5f, 0.03f, tile.y + 0.5f);
            highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
            highlight.GetComponent<Collider>().enabled = false;
            var rend = highlight.GetComponent<Renderer>();
            rend.material = movableTileMaterial;
            movableTileHighlights.Add(highlight);
        }
    }

    public void ClearMovableHighlights()
    {
        foreach (var highlight in movableTileHighlights) { if(highlight != null) Destroy(highlight); }
        movableTileHighlights.Clear();
    }

    public void HighlightTargets(List<UnitBase> targets)
    {
        ClearTargetHighlights();
        foreach (var unit in targets)
        {
            Vector2Int tile = unit.position;
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlight.name = $"TargetHighlight_{tile.x}_{tile.y}";
            highlight.transform.position = new Vector3(tile.x + 0.5f, 0.04f, tile.y + 0.5f);
            highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
            highlight.GetComponent<Collider>().enabled = false;
            var rend = highlight.GetComponent<Renderer>();
            rend.material = executableTargetMaterial;
            targetHighlights.Add(highlight);
        }
    }

    public void ClearTargetHighlights()
    {
        foreach (var highlight in targetHighlights) { if(highlight != null) Destroy(highlight); }
        targetHighlights.Clear();
    }

    public bool IsValidTile(Vector2Int tile)
    {
        return tile.x >= 0 && tile.x < width && tile.y >= 0 && tile.y < height;
    }
    
    // This is a dummy method to ensure compilation. The actual implementation is in TurnManager.
    public void ClearSelection() { }
}
