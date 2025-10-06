using UnityEngine;
using System.Collections.Generic;

public enum PreviewActionType
{
    Attack,
    Move,
    Skill,
    Wait
}

public class UnitActionPreview
{
    public UnitBase unit;
    public PreviewActionType actionType;
    public Vector2Int targetPosition;
    public List<Vector2Int> affectedTiles = new List<Vector2Int>();
    public int skillIndex = -1;
}

public class PreviewManager : MonoBehaviour, IManager
{

public void BeforeInit()
    {
        if (previewVisuals == null)
            previewVisuals = new List<GameObject>();
    }

    public void AfterInit()
    {
        stageManager = Core.Instance.StageManager;
        gridManager = Core.Instance.GridManager;
        turnManager = Core.Instance.TurnManager;
        
        DebugPrinter.LogColor(LogType.AI, "[PreviewManager] AfterInit - Registering events");
        
        if (turnManager != null)
        {
            turnManager.OnPlayerTurnStart += UpdateAllPreviews;
            turnManager.OnPlayerActionEnd += UpdateAllPreviews;
            DebugPrinter.LogColor(LogType.AI, "[PreviewManager] Events registered successfully");
        }
        else
        {
            DebugPrinter.LogColor(LogType.AI, "[PreviewManager] TurnManager is null!");
        }
    }

    private Dictionary<UnitBase, UnitActionPreview> currentPreviews = new Dictionary<UnitBase, UnitActionPreview>();
    private StageManager stageManager;
    private GridManager gridManager;
    private TurnManager turnManager;

    public string previewArrowTag;
    public List<GameObject> previewVisuals;



void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnPlayerTurnStart -= UpdateAllPreviews;
            turnManager.OnPlayerActionEnd -= UpdateAllPreviews;
        }
    }

public void UpdateAllPreviews()
    {
        DebugPrinter.LogColor(LogType.AI, "[PreviewManager] UpdateAllPreviews called");
        
        ClearAllPreviews();
        
        if (turnManager == null || turnManager.CurrentTurn != TurnManager.Turn.Player)
        {
            DebugPrinter.LogColor(LogType.AI, "[PreviewManager] Not player turn, skipping preview update");
            return;
        }
        
        var player = stageManager.GetPlayer();
        if (player == null)
        {
            DebugPrinter.LogColor(LogType.AI, "[PreviewManager] No player found");
            return;
        }
        
        var enemies = stageManager.GetEnemies();
        if (enemies == null || enemies.Count == 0)
        {
            DebugPrinter.LogColor(LogType.AI, "No enemies to show preview for.");
            return;
        }
        
        DebugPrinter.LogColor(LogType.AI, $"[PreviewManager] Processing {enemies.Count} enemies for preview");
        
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            
            var preview = SimulateEnemyAction(enemy, player.position);
            if (preview != null)
            {
                currentPreviews[enemy] = preview;
                ShowPreviewVisual(preview);
            }
        }
        
        DebugPrinter.LogColor(LogType.AI, $"Updated previews for {currentPreviews.Count} enemies");
    }

    public void UpdatePreviewsAfterPlayerAction()
    {
        UpdateAllPreviews();
    }

private UnitActionPreview SimulateEnemyAction(UnitBase unit, Vector2Int playerPosition)
    {
        var decision = EnemyDecisionLogic.DecideAction(unit, playerPosition);

        var preview = new UnitActionPreview
        {
            unit = unit,
            actionType = ConvertToPreviewActionType(decision.actionType),
            targetPosition = decision.targetPosition,
            skillIndex = decision.skillIndex,
            affectedTiles = decision.affectedTiles ?? new List<Vector2Int>()
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

        GameObject visualObj = Core.Instance.PoolManager.SpawnFromPool(previewArrowTag, null, false);
        if (visualObj == null)
        {
            Debug.LogWarning($"Failed to spawn preview visual from pool: {previewArrowTag}");
            return;
        }
        
        Vector3 startPos = GridToWorld(preview.unit.position);
        Vector3 endPos = GridToWorld(preview.targetPosition);
        
        var previewPrefab = visualObj.GetComponent<PreviewPrefab>();
        if (previewPrefab == null)
        {
            Debug.LogError("PreviewPrefab component not found on pooled object!");
            Core.Instance.PoolManager.ReturnToPool(visualObj);
            return;
        }

        Color arrowColor = preview.actionType == PreviewActionType.Attack
            ? GameColors.Gameplay.AttackPreview
            : GameColors.Gameplay.MovePreview;
        
        previewPrefab.Init(startPos, endPos, preview.actionType, arrowColor);
        
        previewVisuals.Add(visualObj);
        
        if (preview.actionType == PreviewActionType.Attack)
        {
            gridManager.HighlightDangerTiles(preview.affectedTiles);
        }
    }

private void ClearAllPreviews()
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

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x + 0.5f, 0.1f, gridPos.y + 0.5f);
    }

    public UnitActionPreview GetPreviewForEnemy(EnemyUnit enemy)
    {
        return currentPreviews.ContainsKey(enemy) ? currentPreviews[enemy] : null;
    }
}
