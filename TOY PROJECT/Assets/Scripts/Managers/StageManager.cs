using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StageManager : MonoBehaviour
{
    [Header("Dependencies")]
    public GridManager gridManager;
    [SerializeField] private GameObject portalPrefab; // 포탈 프리팹을 여기에 할당해야 합니다.

    // --- 현재 스테이지 상태 ---
    private StageData currentStageData;
    private PlayerUnit player;
    private List<EnemyUnit> enemies = new List<EnemyUnit>();
    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private List<GameObject> spawnedPortals = new List<GameObject>();

    /// <summary>
    /// 새로운 스테이지(방)를 로드합니다. 기존에 있던 모든 것을 지우고 새로 구성합니다.
    /// </summary>
    public void LoadStage(StageData stageData)
    {
        if (stageData == null)
        {
            Debug.LogError("LoadStage에 전달된 StageData가 null입니다!");
            return;
        }

        ClearCurrentStage(); // 이전 스테이지의 모든 오브젝트를 정리합니다.
        
        currentStageData = stageData;

        // 전투 타입의 스테이지일 경우에만 그리드와 전투 관련 오브젝트를 생성합니다.
        if (IsBattleType(stageData.stageType))
        {
            gridManager.GenerateGrid(currentStageData);
            SetupCamera(currentStageData.width, currentStageData.height);
            SpawnObstacles(currentStageData);
        }
    }

    /// <summary>
    /// 현재 맵의 가장자리에 다음 층으로 가는 포탈들을 생성합니다.
    /// </summary>
    public void CreatePortals(List<StageType> portalTypes)
    {
        if (portalPrefab == null)
        {
            Debug.LogError("Portal Prefab이 StageManager에 할당되지 않았습니다!");
            return;
        }

        // 포탈이 생성될 위치를 정의합니다. (맵 상단, 좌측, 우측 등)
        Vector3[] spawnPositions = {
            new Vector3(currentStageData.width / 2f, 0.5f, currentStageData.height + 1),
            new Vector3(-1, 0.5f, currentStageData.height / 2f),
            new Vector3(currentStageData.width + 1, 0.5f, currentStageData.height / 2f)
        };

        for (int i = 0; i < portalTypes.Count && i < spawnPositions.Length; i++)
        {
            StageType type = portalTypes[i];
            Vector3 pos = spawnPositions[i];

            GameObject portalObj = Instantiate(portalPrefab, pos, Quaternion.identity);
            Portal portal = portalObj.GetComponent<Portal>();
            portal.Initialize(type);
            
            spawnedPortals.Add(portalObj);
        }
    }

    /// <summary>
    /// 현재 스테이지에 있는 모든 적, 장애물, 포탈을 제거하고 턴 데이터를 초기화합니다.
    /// </summary>
    private void ClearCurrentStage()
    {
        // Clear all game objects
        foreach (var enemy in enemies) { if(enemy != null) Destroy(enemy.gameObject); }
        enemies.Clear();

        foreach (var obstacle in spawnedObstacles) { if(obstacle != null) Destroy(obstacle); }
        spawnedObstacles.Clear();

        foreach (var portal in spawnedPortals) { if(portal != null) Destroy(portal); }
        spawnedPortals.Clear();

        // Clear manager states
        gridManager.ClearGrid();
        Core.Instance.TurnManager.ClearTurn(); // It is StageManager's responsibility to clear turns
    }

    #region Unit Spawning

    public async void SpawnPlayer(Vector2Int spawnPosition)
    {
        if (currentStageData == null) return;

        string prefabKey = GetPrefabName(currentStageData.playerType);
        var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded) return;

        GameObject obj = Instantiate(handle.Result, GridToWorld(spawnPosition), Quaternion.identity);
        player = obj.GetComponent<PlayerUnit>();
        player.position = spawnPosition;
        gridManager.RegisterUnit(player, spawnPosition);
    }

    public async void SpawnWave(int waveIndex)
    {
        if (currentStageData == null) return;
        
        int arrayIndex = waveIndex - 1;
        if (arrayIndex < 0 || arrayIndex >= currentStageData.waves.Length)
        {
            Debug.Log("Attempted to spawn a wave that does not exist.");
            return;
        }

        EnemyWave wave = currentStageData.waves[arrayIndex];
        Debug.Log($"Spawning Wave {waveIndex}: {wave.waveName}");

        foreach (var enemyData in wave.enemySpawns)
        {
            string prefabKey = GetPrefabName(enemyData.enemyType);
            var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded) continue;

            GameObject obj = Instantiate(handle.Result, GridToWorld(enemyData.spawnPos), Quaternion.identity);
            EnemyUnit enemy = obj.GetComponent<EnemyUnit>();
            enemy.position = enemyData.spawnPos;
            enemies.Add(enemy);
            gridManager.RegisterUnit(enemy, enemyData.spawnPos);
        }
    }

    public async UniTask SpawnEnemiesForTurn(int waveIndex, int turnNumber)
    {
        var enemiesToSpawn = GetEnemiesSpawningOnTurn(waveIndex, turnNumber);
        if (enemiesToSpawn == null || enemiesToSpawn.Count == 0)
        {
            Debug.Log($"Wave {waveIndex}, Turn {turnNumber}: No enemies to spawn.");
            return;
        }

        Debug.Log($"Spawning {enemiesToSpawn.Count} enemies on Wave {waveIndex}, Turn {turnNumber}");

        foreach (var enemyData in enemiesToSpawn)
        {
            string prefabKey = GetPrefabName(enemyData.enemyType);
            var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded) continue;

            GameObject obj = Instantiate(handle.Result, GridToWorld(enemyData.spawnPos), Quaternion.identity);
            EnemyUnit enemy = obj.GetComponent<EnemyUnit>();
            enemy.position = enemyData.spawnPos;
            enemies.Add(enemy);
            gridManager.RegisterUnit(enemy, enemyData.spawnPos);
        }
    }

    async void SpawnObstacles(StageData stage)
    {
        if (stage.obstacleSpawns == null) return;
        foreach (var obsData in stage.obstacleSpawns)
        {
            if (obsData.obstacleData == null) continue;
            var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Obstacles/{obsData.obstacleData.unitMeta.nameKey}.prefab");
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded) continue;
            GameObject obj = Instantiate(handle.Result, GridToWorld(obsData.spawnPos), Quaternion.identity);
            Obstacle obstacle = obj.GetComponent<Obstacle>();
            if (obstacle != null) obstacle.data = obsData.obstacleData;
            spawnedObstacles.Add(obj);
        }
    }

    #endregion

    #region Getters & Helpers

    public void UnregisterEnemy(EnemyUnit enemy) { if (enemies.Contains(enemy)) enemies.Remove(enemy); }
    public PlayerUnit GetPlayer() { return player; }
    public List<EnemyUnit> GetEnemies() { return enemies; }
    public StageData GetCurrentStageData() { return currentStageData; }
    private bool IsBattleType(StageType type) => type == StageType.Battle || type == StageType.EliteBattle || type == StageType.Boss;
    private Vector3 GridToWorld(Vector2Int gridPos) => new Vector3(gridPos.x + 0.5f, 0, gridPos.y + 0.5f);

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

    #endregion


public List<EnemySpawnData> GetEnemiesSpawningOnTurn(int waveIndex, int turnNumber)
    {
        if (currentStageData == null) return null;
        
        int arrayIndex = waveIndex - 1;
        if (arrayIndex < 0 || arrayIndex >= currentStageData.waves.Length)
            return null;

        EnemyWave wave = currentStageData.waves[arrayIndex];
        
        if (wave.turnSpawns == null || wave.turnSpawns.Length == 0)
            return null;

        foreach (var turnSpawn in wave.turnSpawns)
        {
            if (turnSpawn.turnNumber == turnNumber)
            {
                return turnSpawn.enemies != null ? new List<EnemySpawnData>(turnSpawn.enemies) : null;
            }
        }
        
        return null;
    }
}
