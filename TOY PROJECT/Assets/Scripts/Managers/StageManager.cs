using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StageManager : MonoBehaviour
{
    public GridManager gridManager;
    public StageData[] stages;

    private StageData currentStageData;
    private PlayerUnit player;
    private List<EnemyUnit> enemies = new List<EnemyUnit>();

    /// <summary>
    /// 스테이지의 기본 환경(맵, 플레이어, 장애물)을 로드합니다. 적은 소환하지 않습니다.
    /// </summary>
    public void LoadStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stages.Length)
        {
            Debug.LogError($"Invalid stage index: {stageIndex}");
            return;
        }
        
        currentStageData = stages[stageIndex];
        gridManager.GenerateGrid(currentStageData);
        SetupCamera(currentStageData.width, currentStageData.height);
        SpawnPlayer(currentStageData);
        SpawnObstacles(currentStageData);
    }

    /// <summary>
    /// 현재 스테이지 데이터에서 특정 웨이브의 적들을 소환합니다.
    /// </summary>
    public async void SpawnWave(int waveIndex)
    {
        if (currentStageData == null)
        {
            Debug.LogError("No stage loaded to spawn a wave from.");
            return;
        }
        
        int arrayIndex = waveIndex - 1; // TurnManager의 웨이브는 1부터 시작, 배열은 0부터 시작
        if (arrayIndex < 0 || arrayIndex >= currentStageData.waves.Length)
        {
            Debug.Log("모든 웨이브를 클리어했습니다! 스테이지 클리어!");
            // TODO: 스테이지 클리어 로직 (예: 결과 화면 표시, 다음 스테이지로 이동 등)
            return;
        }

        EnemyWave wave = currentStageData.waves[arrayIndex];
        Debug.Log($"Spawning Wave {waveIndex}: {wave.waveName}");

        foreach (var enemyData in wave.enemySpawns)
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
            enemy.position = enemyData.spawnPos;
            enemies.Add(enemy);
            gridManager.RegisterUnit(enemy, enemyData.spawnPos);
        }
    }

    /// <summary>
    /// 적이 죽었을 때 목록에서 제거합니다. 웨이브 클리어 판정에 사용됩니다.
    /// </summary>
    public void UnregisterEnemy(EnemyUnit enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
        }
    }

    // --- 기존 로직 (일부 수정) ---

    async void SpawnPlayer(StageData stage)
    {
        string prefabKey = GetPrefabName(stage.playerType);
        var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded) return;
        GameObject obj = Instantiate(handle.Result, GridToWorld(stage.playerSpawn), Quaternion.identity);
        player = obj.GetComponent<PlayerUnit>();
        player.position = stage.playerSpawn;
        gridManager.RegisterUnit(player, stage.playerSpawn);
    }

    async void SpawnObstacles(StageData stage)
    {
        foreach (var obsData in stage.obstacleSpawns)
        {
            if (obsData.obstacleData == null) continue;
            var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Obstacles/{obsData.obstacleData.unitMeta.nameKey}.prefab");
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded) continue;
            GameObject obj = Instantiate(handle.Result, GridToWorld(obsData.spawnPos), Quaternion.identity);
            Obstacle obstacle = obj.GetComponent<Obstacle>();
            if (obstacle != null) obstacle.data = obsData.obstacleData;
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

    public PlayerUnit GetPlayer() { return player; }
    public List<EnemyUnit> GetEnemies() { return enemies; }

    void SetupCamera(int width, int height)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        var controller = cam.GetComponent<CameraController>();
        if (controller == null) return;

        Vector3 center = new Vector3(width / 2f, 0, height / 2f);
        float maxDim = Mathf.Max(width, height);

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

        float cameraDistanceAndHeight = maxDim * 1.2f;
        controller.distance = cameraDistanceAndHeight;
        controller.height = cameraDistanceAndHeight;
        controller.orthographicSize = maxDim * 0.75f;
    }
}