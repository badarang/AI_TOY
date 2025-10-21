using System.Collections.Generic;
using UnityEngine;

public enum PreviewActionType
{
    Attack,
    Move,
    Skill,
    Wait,
}

public class UnitActionPreview
{
    public UnitBase unit;
    public PreviewActionType actionType;
    public Vector2Int targetPosition;
    public List<Vector2Int> affectedTiles = new List<Vector2Int>();
    public int skillIndex = -1;
    public GameObject visualObject;
}

public class PreviewManager : MonoBehaviour, IManager
{
    private HashSet<Vector2Int> _previewOccupiedTiles = new HashSet<Vector2Int>();

    public void BeforeInit()
    {
        if (previewVisuals == null)
            previewVisuals = new List<GameObject>();
    }

    public void AfterInit()
    {
        gridManager = Core.Instance.GridManager;
        turnManager = Core.Instance.TurnManager;

        if (turnManager != null)
        {
            turnManager.OnPlayerTurnStart += UpdateAllPreviews;
            turnManager.OnPlayerSkillEnd += UpdateAllPreviews;
            turnManager.OnEnemyTurnStart += UpdateAllPreviews;
        }

        // EventManager의 유닛 죽음 이벤트 구독
        if (Core.Instance?.EventManager != null)
        {
            Core.Instance.EventManager.OnUnitDied += HandleUnitDied;
        }
    }

    private Dictionary<UnitBase, UnitActionPreview> currentPreviews = new();

    private GridManager gridManager;
    private TurnManager turnManager;

    public string previewArrowTag;
    public List<GameObject> previewVisuals;

    private void HandleUnitDied(UnitBase unit)
    {
        if (unit is EnemyUnit enemyUnit)
        {
            DebugPrinter.LogColor(LogType.Unit, $"Enemy died, clearing preview: {unit.name}");
            ClearPreviewForEnemy(enemyUnit);
        }
    }

    public void Dispose()
    {
        DebugPrinter.LogColor(LogType.System, "Disposing...");

        ClearAllPreviews();

        if (turnManager != null)
        {
            turnManager.OnPlayerTurnStart -= UpdateAllPreviews;
            turnManager.OnPlayerSkillEnd -= UpdateAllPreviews;
            turnManager.OnEnemyTurnStart -= UpdateAllPreviews;
        }

        Core.Instance.EventManager.OnUnitDied -= HandleUnitDied;

        currentPreviews.Clear();

        gridManager = null;
        turnManager = null;
    }

    public void UpdateAllPreviews()
    {
        ClearAllPreviews();

        var players = Core.Instance.UnitManager.GetAllPlayers();
        if (players == null || players.Count == 0)
            return;
        var targetPlayer = players[0];

        var enemies = Core.Instance.UnitManager.GetEnemies();
        if (enemies == null || enemies.Count == 0)
            return;

        var sortedEnemies = new List<EnemyUnit>(enemies);
        sortedEnemies.Sort(
            (a, b) =>
            {
                int xCompare = a.position.x.CompareTo(b.position.x);
                if (xCompare != 0)
                    return xCompare;
                return a.position.y.CompareTo(b.position.y);
            }
        );

        _previewOccupiedTiles.Clear();
        foreach (var enemy in sortedEnemies)
        {
            _previewOccupiedTiles.Add(enemy.position);
        }

        foreach (var enemy in sortedEnemies)
        {
            if (enemy == null)
                continue;

            var preview = SimulateEnemyAction(enemy, targetPlayer.position, _previewOccupiedTiles);
            if (preview != null)
            {
                currentPreviews[enemy] = preview;
                ShowPreviewVisual(preview);

                _previewOccupiedTiles.Remove(enemy.position);
                _previewOccupiedTiles.Add(preview.targetPosition);
            }
        }
    }

    private UnitActionPreview SimulateEnemyAction(
        UnitBase unit,
        Vector2Int playerPosition,
        HashSet<Vector2Int> occupiedTiles
    )
    {
        var decision = EnemyDecisionLogic.DecideAction(
            unit,
            playerPosition,
            isPreview: true,
            occupiedTiles: occupiedTiles
        );

        var preview = new UnitActionPreview
        {
            unit = unit,
            actionType = ConvertToPreviewActionType(decision.actionType),
            targetPosition = decision.targetPosition,
            skillIndex = decision.skillIndex,
            affectedTiles = decision.affectedTiles ?? new List<Vector2Int>(),
        };

        return preview;
    }

    private UnitActionPreview SimulateEnemyAction(UnitBase unit, Vector2Int playerPosition)
    {
        var decision = EnemyDecisionLogic.DecideAction(unit, playerPosition, isPreview: true);

        var preview = new UnitActionPreview
        {
            unit = unit,
            actionType = ConvertToPreviewActionType(decision.actionType),
            targetPosition = decision.targetPosition,
            skillIndex = decision.skillIndex,
            affectedTiles = decision.affectedTiles ?? new List<Vector2Int>(),
        };

        return preview;
    }

    private PreviewActionType ConvertToPreviewActionType(EnemyDecision.ActionType actionType)
    {
        switch (actionType)
        {
            case EnemyDecision.ActionType.Attack:
                return PreviewActionType.Attack;
            case EnemyDecision.ActionType.Move:
                return PreviewActionType.Move;
            case EnemyDecision.ActionType.Wait:
            default:
                return PreviewActionType.Wait;
        }
    }

    private void ShowPreviewVisual(UnitActionPreview preview)
    {
        if (preview.actionType == PreviewActionType.Wait)
            return;

        if (string.IsNullOrEmpty(previewArrowTag))
        {
            Debug.LogError("[PreviewManager] previewArrowTag is not set!");
            return;
        }

        GameObject visualObj = Core.Instance.PoolManager.SpawnFromPool(
            previewArrowTag,
            null,
            false
        );
        if (visualObj == null)
        {
            Debug.LogWarning(
                $"[PreviewManager] Failed to spawn preview visual from pool: {previewArrowTag}"
            );
            return;
        }

        Vector3 startPos = GridToWorld(preview.unit.position);
        Vector3 endPos = GridToWorld(preview.targetPosition);

        var previewPrefab = visualObj.GetComponent<PreviewPrefab>();
        if (previewPrefab == null)
        {
            Debug.LogError("[PreviewManager] PreviewPrefab component not found on pooled object!");
            Core.Instance.PoolManager.ReturnToPool(visualObj);
            return;
        }

        Color arrowColor =
            preview.actionType == PreviewActionType.Attack
                ? GameColors.Gameplay.AttackPreview
                : GameColors.Gameplay.MovePreview;

        previewPrefab.Init(startPos, endPos, preview.actionType, arrowColor);
        preview.visualObject = visualObj;

        previewVisuals.Add(visualObj);

        if (preview.actionType == PreviewActionType.Attack)
        {
            gridManager.HighlightDangerTiles(preview.affectedTiles);
        }
    }

    public void ClearAllPreviews()
    {
        if (previewVisuals == null)
        {
            previewVisuals = new List<GameObject>();
            return;
        }

        currentPreviews.Clear();

        foreach (var visual in previewVisuals)
        {
            if (visual != null)
                Core.Instance.PoolManager.ReturnToPool(visual);
        }
        previewVisuals.Clear();
    }

    public void ClearPreviewForEnemy(EnemyUnit enemy)
    {
        if (!currentPreviews.ContainsKey(enemy))
            return;

        var preview = currentPreviews[enemy];

        if (preview.visualObject != null)
        {
            previewVisuals.Remove(preview.visualObject);
            Core.Instance.PoolManager.ReturnToPool(preview.visualObject);
        }

        // if (preview.actionType == PreviewActionType.Attack)
        // {
        //     gridManager.ClearAllDangerHighlights();
        // }

        currentPreviews.Remove(enemy);
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x + 0.5f, 0.1f, gridPos.y + 0.5f);
    }

    public UnitActionPreview GetPreviewForEnemy(EnemyUnit enemy)
    {
        return currentPreviews.ContainsKey(enemy) ? currentPreviews[enemy] : null;
    }
}
