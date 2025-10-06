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

public class PreviewManager : MonoBehaviour
{
    private Dictionary<UnitBase, UnitActionPreview> currentPreviews = new Dictionary<UnitBase, UnitActionPreview>();
    private StageManager stageManager;
    private GridManager gridManager;
    private TurnManager turnManager;

    public string previewArrowTag;
    public List<GameObject> previewVisuals;

    void Start()
    {
        stageManager = Core.Instance.StageManager;
        gridManager = Core.Instance.GridManager;
        turnManager = Core.Instance.TurnManager;
        
        if (turnManager != null)
        {
            turnManager.OnPlayerActionEnd += UpdateAllPreviews;
            turnManager.OnEnemyTurnStart += UpdateAllPreviews;
        }
    }

    void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnPlayerActionEnd -= UpdateAllPreviews;
            turnManager.OnEnemyTurnStart -= UpdateAllPreviews;
        }
    }

public void UpdateAllPreviews()
    {
        ClearAllPreviews();
        
        if (turnManager == null || turnManager.CurrentTurn != TurnManager.Turn.Player)
            return;
        
        var player = stageManager.GetPlayer();
        if (player == null)
            return;
        
        var enemies = stageManager.GetEnemies();
        if (enemies == null || enemies.Count == 0)
        {
            DebugPrinter.LogColor(LogType.AI, "No enemies to show preview for.");
            return;
        }
        
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
        if (unit.unitData == null || unit.unitData.skills == null)
            return null;
        
        var preview = new UnitActionPreview
        {
            unit = unit,
            actionType = PreviewActionType.Wait,
            targetPosition = unit.position
        };
        
        for (int i = 0; i < unit.unitData.skills.Length; i++)
        {
            var skill = unit.unitData.skills[i];
            
            if (skill.skillType == SkillType.Attack)
            {
                int distance = GridUtils.ChebyshevDistance(unit.position, playerPosition);
                
                if (distance <= skill.range && unit.GetSkillCooldown(i) == 0 && unit.ap >= skill.apCost)
                {
                    preview.actionType = PreviewActionType.Attack;
                    preview.targetPosition = playerPosition;
                    preview.skillIndex = i;
                    preview.affectedTiles.Add(playerPosition);
                    
                    DebugPrinter.LogColor(LogType.AI, $"{unit.name} will ATTACK player at {playerPosition}");
                    return preview;
                }
            }
        }
        
        int moveSkillIndex = GetMoveSkillIndex(unit);
        if (moveSkillIndex >= 0)
        {
            var moveSkill = unit.unitData.skills[moveSkillIndex];
            
            if (unit.GetSkillCooldown(moveSkillIndex) == 0 && unit.ap >= moveSkill.apCost)
            {
                var movableTiles = gridManager.GetWalkableTilesInRange(unit.position, moveSkill.range);
                
                Vector2Int? bestMoveTarget = null;
                int closestDistance = int.MaxValue;
                
                foreach (var tile in movableTiles)
                {
                    int distanceToPlayer = GridUtils.ChebyshevDistance(tile, playerPosition);
                    if (distanceToPlayer < closestDistance)
                    {
                        closestDistance = distanceToPlayer;
                        bestMoveTarget = tile;
                    }
                }
                
                if (bestMoveTarget.HasValue && bestMoveTarget.Value != unit.position)
                {
                    preview.actionType = PreviewActionType.Move;
                    preview.targetPosition = bestMoveTarget.Value;
                    
                    DebugPrinter.LogColor(LogType.AI, $"{unit.name} will MOVE to {bestMoveTarget.Value}");
                    return preview;
                }
            }
        }
        
        DebugPrinter.LogColor(LogType.AI, $"{unit.name} will WAIT");
        return preview;
    }

    private int GetMoveSkillIndex(UnitBase enemy)
    {
        if (enemy.unitData == null || enemy.unitData.skills == null)
            return -1;
        
        for (int i = 0; i < enemy.unitData.skills.Length; i++)
        {
            if (enemy.unitData.skills[i].skillType == SkillType.Move)
                return i;
        }
        return -1;
    }

private void ShowPreviewVisual(UnitActionPreview preview)
    {
        if (preview.actionType == PreviewActionType.Wait)
            return;
        
        GameObject visualObj = Core.Instance.PoolManager.SpawnFromPool("PreviewArrow", null, false);
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
        
        Color arrowColor = preview.actionType == PreviewActionType.Attack ? Color.red : Color.yellow;
        previewPrefab.Init(startPos, endPos, preview.actionType, arrowColor);
        
        previewVisuals.Add(visualObj);
        
        if (preview.actionType == PreviewActionType.Attack)
        {
            gridManager.HighlightDangerTiles(preview.affectedTiles);
        }
    }





private void ClearAllPreviews()
    {
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
