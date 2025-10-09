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
        
        if (turnManager != null)
        {
            turnManager.OnPlayerTurnStart += UpdateAllPreviews;
            turnManager.OnPlayerActionEnd += UpdateAllPreviews;
            turnManager.OnEnemyTurnStart += ClearAllPreviews;
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
            turnManager.OnEnemyTurnStart -= ClearAllPreviews;
        }
    }

    public void UpdateAllPreviews()
    {
        ClearAllPreviews();
        
        var players = stageManager.GetAllPlayers();
        if (players == null || players.Count == 0) return;
        var targetPlayer = players[0]; // Simple AI: always target the first player for previews
        
        var enemies = stageManager.GetEnemies();
        if (enemies == null || enemies.Count == 0) return;
        
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            
            var preview = SimulateEnemyAction(enemy, targetPlayer.position);
            if (preview != null)
            {
                currentPreviews[enemy] = preview;
                ShowPreviewVisual(preview);
            }
        }
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
        if (preview.actionType == PreviewActionType.Wait) return;

        if (string.IsNullOrEmpty(previewArrowTag))
        {
            Debug.LogError("[PreviewManager] previewArrowTag is not set!");
            return;
        }
        
        GameObject visualObj = Core.Instance.PoolManager.SpawnFromPool(previewArrowTag, null, false);
        if (visualObj == null)
        {
            Debug.LogWarning($"[PreviewManager] Failed to spawn preview visual from pool: {previewArrowTag}");
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
