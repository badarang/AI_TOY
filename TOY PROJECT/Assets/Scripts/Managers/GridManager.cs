using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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

    [Header("Datas")]
    private Dictionary<Vector2Int, UnitBase> unitPositions = new Dictionary<Vector2Int, UnitBase>();

    private List<GameObject> gridLines = new List<GameObject>();
    private Vector2Int? hoveredCell = null;
    private Vector2Int? selectedCell = null;
    private UnitBase selectedUnit = null;

    public Material highlightMat;
    private GameObject highlightQuad;
    private Material defaultHighlightMat;

    void Update()
    {
        UpdateCellHover();
    }

    public void GenerateGrid(StageData stageData)
    {
        width = stageData.width;
        height = stageData.height;
        ClearAllHighlights();
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

    public void TrySelectUnitAtCell(Vector2Int cellPos)
    {
        if (unitPositions.ContainsKey(cellPos) && unitPositions[cellPos] != null)
        {
            UnitBase unit = unitPositions[cellPos];

            if (unit is PlayerUnit)
            {
                if (selectedUnit == unit)
                {
                    ClearSelection();
                }
                else
                {
                    if (selectedUnit != null)
                        selectedUnit.Deselect();
                    selectedUnit = unit;
                    selectedUnit.Select();
                    selectedCell = cellPos;
                }
            }
            else
            {
                ClearSelection();
            }
        }
        else
        {
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

    public UnitBase GetSelectedUnit() { return selectedUnit; }
    public Vector2Int? GetSelectedCell() { return selectedCell; }
    public Vector2Int? GetHoveredCell() { return hoveredCell; }

    public void ClearSelection()
    {
        if (selectedUnit != null)
            selectedUnit.Deselect();
        selectedUnit = null;
        selectedCell = null;
        ClearMovableHighlights();
        ClearTargetHighlights();
    }

    public void ClearAllHighlights()
    {
        ClearSelection();
    }

    [Header("Movable Tile Highlight")]
    public Material movableTileMaterial;
    private List<GameObject> movableTileHighlights = new List<GameObject>();
    private List<Vector2Int> _currentMovableTiles = new List<Vector2Int>();

    [Header("Target Highlight")]
    public Material executableTargetMaterial;
    private List<GameObject> targetHighlights = new List<GameObject>();

    public List<Vector2Int> FindMovableTiles(Vector2Int startPos, List<Vector2Int> movementPattern)
    {
        var movableTiles = new List<Vector2Int>();
        if (movementPattern == null) return movableTiles;

        foreach (var offset in movementPattern)
        {
            Vector2Int destination = startPos + offset;
            if (destination.x < 0 || destination.x >= width || destination.y < 0 || destination.y >= height) continue;
            if (HasUnitAt(destination)) continue;
            movableTiles.Add(destination);
        }
        return movableTiles;
    }

    public void HighlightMovableTiles(List<Vector2Int> tilesToHighlight)
    {
        ClearMovableHighlights();
        _currentMovableTiles = tilesToHighlight;

        foreach (var tile in tilesToHighlight)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlight.name = $"MovableHighlight_{tile.x}_{tile.y}";
            highlight.transform.position = new Vector3(tile.x + 0.5f, 0.03f, tile.y + 0.5f);
            highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
            highlight.GetComponent<Collider>().enabled = false;
            
            var rend = highlight.GetComponent<Renderer>();
            if (movableTileMaterial != null) { rend.material = movableTileMaterial; }
            else { rend.material.color = new Color(0.1f, 0.5f, 1f, 0.4f); }
            
            movableTileHighlights.Add(highlight);
        }
    }

    public void ClearMovableHighlights()
    {
        foreach (var highlight in movableTileHighlights) { Destroy(highlight); }
        movableTileHighlights.Clear();
        _currentMovableTiles.Clear();
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
            if (executableTargetMaterial != null) { rend.material = executableTargetMaterial; }
            else { rend.material.color = new Color(1f, 0.2f, 0.2f, 0.5f); }
            
            targetHighlights.Add(highlight);
        }
    }

    public void ClearTargetHighlights()
    {
        foreach (var highlight in targetHighlights) { Destroy(highlight); }
        targetHighlights.Clear();
    }

    public bool IsMovableTile(Vector2Int tile) { return _currentMovableTiles.Contains(tile); }

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

    #region A* Pathfinding

    private class PathNode
    {
        public Vector2Int position;
        public int gCost; // Cost from start node
        public int hCost; // Heuristic cost to end node
        public int fCost => gCost + hCost; // Total cost
        public PathNode parent;

        public PathNode(Vector2Int position)
        {
            this.position = position;
        }
    }

    public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int endPos)
    {
        PathNode startNode = new PathNode(startPos);
        PathNode endNode = new PathNode(endPos);

        List<PathNode> openList = new List<PathNode> { startNode };
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();

        while (openList.Count > 0)
        {
            PathNode currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost || (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode.position);

            if (currentNode.position == endPos)
            {
                return RetracePath(startNode, currentNode);
            }

            foreach (Vector2Int neighbourPos in GetNeighbours(currentNode.position))
            {
                if (closedList.Contains(neighbourPos)) continue;

                // If the neighbour is an obstacle (or has a unit, and it's not the target), skip it
                if (HasUnitAt(neighbourPos) && neighbourPos != endPos) continue;

                int newGCost = currentNode.gCost + GetDistance(currentNode.position, neighbourPos);
                PathNode neighbourNode = new PathNode(neighbourPos) { gCost = newGCost, hCost = GetDistance(neighbourPos, endPos), parent = currentNode };

                if (!openList.Exists(n => n.position == neighbourPos) || newGCost < openList.Find(n => n.position == neighbourPos).gCost)
                {
                    if (!openList.Exists(n => n.position == neighbourPos))
                        openList.Add(neighbourNode);
                }
            }
        }

        return null; // No path found
    }

    private List<Vector2Int> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        PathNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    private List<Vector2Int> GetNeighbours(Vector2Int pos)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                // if (Mathf.Abs(x) == 1 && Mathf.Abs(y) == 1) continue; // Uncomment for no diagonal movement

                Vector2Int neighbourPos = new Vector2Int(pos.x + x, pos.y + y);
                if (neighbourPos.x >= 0 && neighbourPos.x < width && neighbourPos.y >= 0 && neighbourPos.y < height)
                {
                    neighbours.Add(neighbourPos);
                }
            }
        }
        return neighbours;
    }

    private int GetDistance(Vector2Int posA, Vector2Int posB)
    {
        int dstX = Mathf.Abs(posA.x - posB.x);
        int dstY = Mathf.Abs(posA.y - posB.y);
        return 14 * Mathf.Min(dstX, dstY) + 10 * (Mathf.Abs(dstX - dstY)); // Diagonal distance heuristic
    }

    #endregion
}
