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
        // UnitData 등 추가 세팅 필요시 여기에
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
            Debug.Log($"적 유닛 생성: {enemyData.enemyType} at {enemyData.spawnPos}");
            enemy.position = enemyData.spawnPos;
            enemies.Add(enemy);
        }
    }

    async void SpawnObstacles(StageData stage)
    {
        foreach (var obsData in stage.obstacleSpawns)
        {
            if (obsData.obstacleData == null || string.IsNullOrEmpty(obsData.obstacleData.obstacleName)) continue;
            var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Obstacles/{obsData.obstacleData.obstacleName}.prefab");
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Obstacle 프리팹을 Addressable에서 찾을 수 없습니다: {obsData.obstacleData.obstacleName}");
                continue;
            }
            GameObject obj = Instantiate(handle.Result, GridToWorld(obsData.spawnPos), Quaternion.identity);
            Obstacle obstacle = obj.GetComponent<Obstacle>();
            if (obstacle != null)
                obstacle.data = obsData.obstacleData;
        }
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        // 각 타일의 중앙에 위치하도록 변환
        return new Vector3(gridPos.x + 0.5f, 0, gridPos.y + 0.5f);
    }

    string GetPrefabName(UnitType type)
    {
        // Enum에 따라 프리팹 이름 매칭 (예시)
        switch (type)
        {
            case UnitType.Player_Zed: return "Player_Zed";
            case UnitType.Player_Lux: return "Player_Lux";
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
        float camHeight = Mathf.Max(width, height) * 1.2f;
        cam.transform.position = center + new Vector3(0, camHeight, camHeight);
        cam.transform.LookAt(center);
        cam.orthographic = false;
        cam.fieldOfView = 45f;
        // CameraController 자동 세팅
        var controller = cam.GetComponent<CameraController>();
        if (controller != null)
        {
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
            controller.distance = camHeight;
            controller.height = camHeight;
        }
    }
} 