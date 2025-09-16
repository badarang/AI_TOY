using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StageManager : MonoBehaviour
{
    public GridManager gridManager;
    public TurnManager turnManager;
    public StageData[] stages;

    private PlayerUnit player;
    private List<EnemyUnit> enemies = new List<EnemyUnit>();

    public void LoadStage(int stageIndex)
    {
        StageData stage = stages[stageIndex];
        gridManager.GenerateGrid(stage);
        SetupCamera(stage.width, stage.height);
        SpawnPlayer(stage);
        SpawnEnemies(stage);
        SpawnObstacles(stage);
        turnManager.StartPlayerTurn();
    }

    async void SpawnPlayer(StageData stage)
    {
        string prefabKey = GetPrefabName(stage.playerType);
        var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Player 프리팹을 Addressable에서 찾을 수 없습니다: {prefabKey}");
            return;
        }
        GameObject obj = Instantiate(handle.Result, GridToWorld(stage.playerSpawn), Quaternion.identity);
        player = obj.GetComponent<PlayerUnit>();
        player.position = stage.playerSpawn;

        gridManager.RegisterUnit(player, stage.playerSpawn);
    }

    async void SpawnEnemies(StageData stage)
    {
        foreach (var enemyData in stage.enemySpawns)
        {
            string prefabKey = GetPrefabName(enemyData.enemyType);
            var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Enemy 프리팹을 Addressable에서 찾을 수 없습니다: {prefabKey}");
                continue;
            }
            GameObject obj = Instantiate(handle.Result, GridToWorld(enemyData.spawnPos), Quaternion.identity);
            EnemyUnit enemy = obj.GetComponent<EnemyUnit>();
            DebugPrinter.DebugColor(DebugType.Unit, $"적 유닛 생성: {enemyData.enemyType} at {enemyData.spawnPos}");
            enemy.position = enemyData.spawnPos;
            enemies.Add(enemy);

            gridManager.RegisterUnit(enemy, enemyData.spawnPos);
        }
    }

    async void SpawnObstacles(StageData stage)
    {
        foreach (var obsData in stage.obstacleSpawns)
        {
            if (obsData.obstacleData == null || string.IsNullOrEmpty(obsData.obstacleData.unitMeta.nameKey)) continue;
            var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Obstacles/{obsData.obstacleData.unitMeta.nameKey}.prefab");
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Obstacle 프리팹을 Addressable에서 찾을 수 없습니다: {obsData.obstacleData.unitMeta.nameKey}");
                continue;
            }
            GameObject obj = Instantiate(handle.Result, GridToWorld(obsData.spawnPos), Quaternion.identity);
            Obstacle obstacle = obj.GetComponent<Obstacle>();
            DebugPrinter.DebugColor(DebugType.Unit, $"장애물 생성: {obsData.obstacleData} at {obsData.spawnPos}");

            if (obstacle != null)
                obstacle.data = obsData.obstacleData;
        }
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x + 0.5f, 0, gridPos.y + 0.5f);
    }

    string GetPrefabName(UnitType type)
    {
        switch (type)
        {
            case UnitType.Player_Hikai: return "Player_Hikai";
            case UnitType.Player_Vrixa: return "Player_Vrixa";
            case UnitType.Enemy_Goose: return "Enemy_Goose";
            default: return null;
        }
    }

    public PlayerUnit GetPlayer()
    {
        return player;
    }

    public List<EnemyUnit> GetEnemies()
    {
        return enemies;
    }

    void SetupCamera(int width, int height)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 center = new Vector3(width / 2f, 0, height / 2f);
        float maxDim = Mathf.Max(width, height);

        // Ensure camera is orthographic
        cam.orthographic = true;
        
        // Get the controller and configure it
        var controller = cam.GetComponent<CameraController>();
        if (controller != null)
        {
            // Set the target for the controller to the center of the stage
            if (controller.target == null)
            {
                var t = new GameObject("CameraTarget").transform;
                t.position = center;
                controller.target = t;
            }
            else
            {
                controller.target.position = center;
            }

            // Update controller properties to frame the stage correctly
            float cameraDistanceAndHeight = maxDim * 1.2f;
            controller.distance = cameraDistanceAndHeight;
            controller.height = cameraDistanceAndHeight;
            controller.orthographicSize = maxDim * 0.75f; // Adjust this multiplier for best fit
        }
    }
}