using System;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GridManager : MonoBehaviour, IManager
{
    public void BeforeInit() { }

    public void AfterInit() { }

    private const float GRID_CELL_OFFSET = 0.5f;
    private const float GRID_LINE_HEIGHT = 0.01f;
    private const float HOVER_HIGHLIGHT_HEIGHT = 0.02f;
    private const float MOVABLE_HIGHLIGHT_HEIGHT = 0.03f;
    private const float ATTACK_HIGHLIGHT_HEIGHT = 0.04f;
    private const float GRID_LINE_WIDTH = 0.03f;

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
    private int displayWidth { get; set; }
    private int displayHeight { get; set; }
    private int actualWidth { get; set; }
    private int actualHeight { get; set; }
    public LayerMask unitLayer;
    public AssetReference gridPlaneAsset;

    [Header("Datas")]
    private Dictionary<Vector2Int, UnitBase> unitPositions = new Dictionary<Vector2Int, UnitBase>();

    private GameObject gridPlane;
    private Vector2Int? hoveredCell = null;

    public Material highlightMat;
    private GameObject highlightQuad;

    void Update()
    {
        UpdateCellHover();
    }

public async void GenerateGrid(Room room)
    {
        displayWidth = room.width;
        displayHeight = room.height;
        // 격자를 2씩 더 크게 생성 (포탈 공간용)
        actualWidth = displayWidth + 2;
        actualHeight = displayHeight + 2;

        ClearGrid();

        await CreateGridPlane();
    }

    private async UniTask CreateGridPlane()
    {
        if (gridPlaneAsset == null)
        {
            Debug.LogError("GridPlane AssetReference가 할당되지 않았습니다.");
            return;
        }

        AsyncOperationHandle<GameObject> handle = gridPlaneAsset.LoadAssetAsync<GameObject>();
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            gridPlane = Instantiate(handle.Result);
            gridPlane.name = "GridPlane";

            gridPlane.transform.position = new Vector3(displayWidth * 0.5f, 0f, displayHeight * 0.5f);
            gridPlane.transform.localScale = new Vector3(displayWidth * 0.1f, 1f, displayHeight * 0.1f);

            var collider = gridPlane.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
        else
        {
            Debug.LogError("GridPlane Addressable 로드 실패");
        }
    }

    public void ClearGrid()
    {
        unitPositions.Clear();
        hoveredCell = null;
        ClearGridPlane();
        ClearAllHighlights();
        if (highlightQuad != null)
        {
            Destroy(highlightQuad);
            highlightQuad = null;
        }
    }

    private void ClearGridPlane()
    {
        if (gridPlane != null)
        {
            Destroy(gridPlane);
            gridPlane = null;
        }
    }

    private void UpdateCellHover()
    {
        Vector2 inputPosition = GetInputPosition();
        if (inputPosition == Vector2.zero)
            return;

        Ray ray = Camera.main.ScreenPointToRay(inputPosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            int x = Mathf.FloorToInt(hitPoint.x);
            int z = Mathf.FloorToInt(hitPoint.z);

            if (x >= 0 && x < actualWidth && z >= 0 && z < actualHeight)
            {
                Vector2Int cellPos = new Vector2Int(x, z);

                if (
                    (unitPositions.ContainsKey(cellPos) && unitPositions[cellPos] != null)
                    || IsMovableHighlightedTile(cellPos)
                )
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

        highlightQuad.transform.position = new Vector3(
            x + GRID_CELL_OFFSET,
            HOVER_HIGHLIGHT_HEIGHT,
            z + GRID_CELL_OFFSET
        );
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
            highlight.transform.position = new Vector3(
                tile.x + GRID_CELL_OFFSET,
                MOVABLE_HIGHLIGHT_HEIGHT,
                tile.y + GRID_CELL_OFFSET
            );
            highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
            highlight.GetComponent<Collider>().enabled = false;
            var rend = highlight.GetComponent<Renderer>();
            rend.material = movableTileMaterial;
            movableTileHighlights.Add(highlight);
        }
    }

    public void ClearMovableHighlights()
    {
        foreach (var highlight in movableTileHighlights)
        {
            if (highlight != null)
                Destroy(highlight);
        }
        movableTileHighlights.Clear();
    }

    public void HighlightAttackableTiles(List<Vector2Int> tiles)
    {
        ClearTargetHighlights();
        foreach (var tile in tiles)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlight.name = $"AttackHighlight_{tile.x}_{tile.y}";
            highlight.transform.position = new Vector3(
                tile.x + GRID_CELL_OFFSET,
                ATTACK_HIGHLIGHT_HEIGHT,
                tile.y + GRID_CELL_OFFSET
            );
            highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
            highlight.GetComponent<Collider>().enabled = false;
            var rend = highlight.GetComponent<Renderer>();
            rend.material = executableTargetMaterial;
            targetHighlights.Add(highlight);
        }
    }

    public void HighlightDangerTiles(List<Vector2Int> tiles)
    {
        foreach (var tile in tiles)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlight.name = $"DangerHighlight_{tile.x}_{tile.y}";
            highlight.transform.position = new Vector3(
                tile.x + GRID_CELL_OFFSET,
                ATTACK_HIGHLIGHT_HEIGHT + 0.01f,
                tile.y + GRID_CELL_OFFSET
            );
            highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
            highlight.GetComponent<Collider>().enabled = false;
            var rend = highlight.GetComponent<Renderer>();

            Material dangerMat = new Material(Shader.Find("Unlit/Color"));
            dangerMat.color = GameColors.Grid.Danger;
            rend.material = dangerMat;

            targetHighlights.Add(highlight);
        }
    }

    public void HighlightTargets(List<UnitBase> targets)
    {
        var tiles = targets.Select(u => u.position).ToList();
        HighlightAttackableTiles(tiles);
    }

    public void ClearTargetHighlights()
    {
        foreach (var highlight in targetHighlights)
        {
            if (highlight != null)
                Destroy(highlight);
        }
        targetHighlights.Clear();
    }

    public bool IsValidTile(Vector2Int tile)
    {
        return tile.x >= 0 && tile.x < displayWidth &&
               tile.y >= 0 && tile.y < displayHeight;
    }

// 포탈 위치 포함 (확장된 9x9)
    public bool IsValidTileWithPortal(Vector2Int tile)
    {
        return tile.x >= 0 && tile.x < actualWidth &&
               tile.y >= 0 && tile.y < actualHeight;
    }

    public void ClearSelection()
    {
        ClearAllHighlights();
    }

    // --- Pathfinding (A*) ---

    private class PathNode
    {
        public Vector2Int position;
        public int gCost;
        public int hCost;
        public int fCost;
        public PathNode parent;

        public PathNode(Vector2Int position)
        {
            this.position = position;
            this.gCost = int.MaxValue;
        }

        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }
    }

    public List<Vector2Int> FindPath(Vector2Int startPosition, Vector2Int targetPosition)
    {
        PathNode startNode = new PathNode(startPosition);
        PathNode targetNode = new PathNode(targetPosition);

        List<PathNode> openList = new List<PathNode> { startNode };
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, PathNode> allNodes = new Dictionary<Vector2Int, PathNode>();
        allNodes[startPosition] = startNode;

        startNode.gCost = 0;
        startNode.hCost = ChebyshevDistance(startPosition, targetPosition);
        startNode.CalculateFCost();

        while (openList.Count > 0)
        {
            PathNode currentNode = GetLowestFCostNode(openList);

            if (currentNode.position == targetNode.position)
            {
                return RetracePath(startNode, currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode.position);

            foreach (Vector2Int neighbourPosition in GetNeighbourPositions(currentNode.position))
            {
                if (closedList.Contains(neighbourPosition) || !IsValidTile(neighbourPosition))
                {
                    continue;
                }

                if (!IsWalkable(neighbourPosition) && neighbourPosition != targetPosition)
                {
                    continue;
                }

                int tentativeGCost = currentNode.gCost + 1; // Chebyshev distance for adjacent nodes is 1

                PathNode neighbourNode;
                if (!allNodes.TryGetValue(neighbourPosition, out neighbourNode))
                {
                    neighbourNode = new PathNode(neighbourPosition);
                    allNodes[neighbourPosition] = neighbourNode;
                }

                if (tentativeGCost < neighbourNode.gCost)
                {
                    neighbourNode.parent = currentNode;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.hCost = ChebyshevDistance(neighbourPosition, targetPosition);
                    neighbourNode.CalculateFCost();

                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }
        }

        Debug.LogWarning($"Path not found from {startPosition} to {targetPosition}");
        return null;
    }

    private bool IsWalkable(Vector2Int position)
    {
        return !HasUnitAt(position);
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

    private PathNode GetLowestFCostNode(List<PathNode> pathNodeList)
    {
        PathNode lowestFCostNode = pathNodeList[0];
        for (int i = 1; i < pathNodeList.Count; i++)
        {
            if (
                pathNodeList[i].fCost < lowestFCostNode.fCost
                || (
                    pathNodeList[i].fCost == lowestFCostNode.fCost
                    && pathNodeList[i].hCost < lowestFCostNode.hCost
                )
            )
            {
                lowestFCostNode = pathNodeList[i];
            }
        }
        return lowestFCostNode;
    }

    private List<Vector2Int> GetNeighbourPositions(Vector2Int currentPos)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                neighbours.Add(new Vector2Int(currentPos.x + x, currentPos.y + y));
            }
        }
        return neighbours;
    }

    private int ChebyshevDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }

    public void HighlightWalkableNodes(Vector2Int start, int range)
    {
        List<Vector2Int> walkableTiles = GetWalkableTilesInRange(start, range);
        HighlightMovableTiles(walkableTiles);
    }

    public List<Vector2Int> GetWalkableTilesInRange(Vector2Int start, int range)
    {
        List<Vector2Int> reachableTiles = new List<Vector2Int>();
        Queue<Tuple<Vector2Int, int>> queue = new Queue<Tuple<Vector2Int, int>>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(new Tuple<Vector2Int, int>(start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (currentPos, currentCost) = queue.Dequeue();

            if (currentCost > 0)
            {
                reachableTiles.Add(currentPos);
            }

            if (currentCost < range)
            {
                foreach (var neighbour in GetNeighbourPositions(currentPos))
                {
                    if (
                        IsValidTile(neighbour)
                        && !HasUnitAt(neighbour)
                        && !visited.Contains(neighbour)
                    )
                    {
                        visited.Add(neighbour);
                        queue.Enqueue(new Tuple<Vector2Int, int>(neighbour, currentCost + 1));
                    }
                }
            }
        }

        return reachableTiles;
    }

    private bool IsMovableHighlightedTile(Vector2Int pos)
    {
        return movableTileHighlights.Any(h => h != null && h.name == $"MovableHighlight_{pos.x}_{pos.y}");
    }

    public void Dispose()
    {
        DebugPrinter.LogColor(LogType.System, "[GridManager] Disposing...");
        
        unitPositions.Clear();
        
        ClearGrid();
        
        if (Instance == this)
        {
            Instance = null;
        }
;
    }
}
