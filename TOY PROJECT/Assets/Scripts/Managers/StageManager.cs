using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Fusion;

// StageManager is now a NetworkBehaviour to handle networked stage logic.
public class StageManager : NetworkBehaviour, IManager
{
    [Header("Dependencies")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private GameAssetDatabase gameDatabase; // For loading stage/unit data

    // --- Networked State ---
    [Networked]
    private NetworkStageInfo CurrentStageInfo { get; set; }

    [Networked, Capacity(50)] // Increased capacity just in case
    private NetworkArray<NetworkEnemySpawnInfo> EnemySpawnQueue => default;

    // --- Local State ---
    private StageData currentStageData;
    private PlayerUnit player;
    private List<EnemyUnit> enemies = new List<EnemyUnit>();
    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private List<GameObject> spawnedPortals = new List<GameObject>();

    #region IManager & Fusion Lifecycle

    public void BeforeInit() { }
    public void AfterInit() { }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            LoadStageOnServerRpc("Stage_1");
        }
    }

    #endregion

    #region Stage Loading (Networked)

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void LoadStageOnServerRpc(string stageName)
    {
        if (!HasStateAuthority) return;

        var stageDataToLoad = gameDatabase.GetStageByName(stageName);
        if (stageDataToLoad == null)
        {
            Debug.LogError($"[Server] StageData not found: {stageName}");
            return;
        }

        currentStageData = stageDataToLoad;

        CurrentStageInfo = new NetworkStageInfo
        {
            stageName = stageName,
            waveIndex = 0,
            currentTurn = 0
        };

        if (currentStageData.waves.Length > 0)
        {
            SetupWaveSpawns(0);
        }

        Debug.Log($"[Server] Loaded stage: {stageName}. Notifying clients.");

        NotifyStageLoadedRpc(stageName);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void NotifyStageLoadedRpc(NetworkString<_64> stageName)
    {
        Debug.Log($"[{ (HasStateAuthority ? "Server" : "Client")}] Received notification to load stage: {stageName}");

        var stageDataToLoad = gameDatabase.GetStageByName(stageName.ToString());
        if (stageDataToLoad == null)
        {
            Debug.LogError($"[Client] Failed to find StageData: {stageName} in GameAssetDatabase");
            return;
        }

        LoadStage(stageDataToLoad);
    }

    public void LoadStage(StageData stageData)
    {
        if (stageData == null)
        {
            Debug.LogError("LoadStage called with null StageData!");
            return;
        }

        ClearCurrentStage();
        currentStageData = stageData;

        if (IsBattleType(stageData.stageType))
        {
            gridManager.GenerateGrid(currentStageData);
            SetupCamera(currentStageData.width, currentStageData.height);
            SpawnObstacles(currentStageData);
        }
    }

    private void ClearCurrentStage()
    {
        if (HasStateAuthority)
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.Object != null)
                {
                    Runner.Despawn(enemy.Object);
                }
            }
        }
        enemies.Clear();

        foreach (var obstacle in spawnedObstacles) { if (obstacle != null) Destroy(obstacle); }
        spawnedObstacles.Clear();

        foreach (var portal in spawnedPortals) { if (portal != null) Destroy(portal); }
        spawnedPortals.Clear();

        gridManager.ClearGrid();
        if (Core.Instance?.TurnManager != null)
        {
            Core.Instance.TurnManager.ClearTurn();
        }
    }

    #endregion

    #region Spawning & Wave Management (Server-Authoritative)

    public void AdvanceToNextWave()
    {
        if (!HasStateAuthority) return;

        var newWaveIndex = CurrentStageInfo.waveIndex + 1;
        if (newWaveIndex >= currentStageData.waves.Length)
        {
            Debug.LogWarning("[Server] Tried to advance beyond the final wave.");
            return;
        }

        var info = CurrentStageInfo;
        info.waveIndex = newWaveIndex;
        CurrentStageInfo = info;

        SetupWaveSpawns(newWaveIndex);
        Debug.Log($"[Server] Advanced to Wave {newWaveIndex}. Spawn queue updated.");
    }

    private void SetupWaveSpawns(int waveIndex)
    {
        if (!HasStateAuthority || waveIndex >= currentStageData.waves.Length) return;

        var wave = currentStageData.waves[waveIndex];
        int spawnIndex = 0;
        EnemySpawnQueue.Clear();

        if (wave.turnSpawns != null && wave.turnSpawns.Length > 0)
        {
            foreach (var turnSpawn in wave.turnSpawns)
            {
                foreach (var enemySpawn in turnSpawn.enemies)
                {
                    if (spawnIndex >= EnemySpawnQueue.Length) break;
                    EnemySpawnQueue.Set(spawnIndex++, new NetworkEnemySpawnInfo
                    {
                        enemyTypeIndex = (int)enemySpawn.enemyType,
                        spawnPos = enemySpawn.spawnPos,
                        turnNumber = turnSpawn.turnNumber
                    });
                }
            }
        }
        else if (wave.enemySpawns != null)
        {
            foreach (var enemySpawn in wave.enemySpawns)
            {
                if (spawnIndex >= EnemySpawnQueue.Length) break;
                EnemySpawnQueue.Set(spawnIndex++, new NetworkEnemySpawnInfo
                {
                    enemyTypeIndex = (int)enemySpawn.enemyType,
                    spawnPos = enemySpawn.spawnPos,
                    turnNumber = 0
                });
            }
        }
    }

    public async UniTask SpawnPlayer(Vector2Int spawnPosition)
    {
        if (!HasStateAuthority || currentStageData == null) return;

        string prefabKey = GetPrefabName(currentStageData.playerType);
        var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded) return;

        NetworkObject playerNO = Runner.Spawn(handle.Result, GridToWorld(spawnPosition), Quaternion.identity);
        player = playerNO.GetComponent<PlayerUnit>();
        player.position = spawnPosition;
        
        gridManager.RegisterUnit(player, spawnPosition);
    }

    public async UniTask SpawnEnemiesForTurn(int turnNumber)
    {
        if (!HasStateAuthority) return;

        var enemiesToSpawn = GetEnemySpawnsForTurn(turnNumber);
        if (enemiesToSpawn.Count == 0) return;

        Debug.Log($"[Server] Spawning {enemiesToSpawn.Count} enemies for turn {turnNumber}");

        foreach (var spawnInfo in enemiesToSpawn)
        {
            UnitType enemyType = (UnitType)spawnInfo.enemyTypeIndex;
            string prefabKey = GetPrefabName(enemyType);
            if (string.IsNullOrEmpty(prefabKey)) continue;

            var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded) continue;

            NetworkObject enemyNO = Runner.Spawn(handle.Result, GridToWorld(spawnInfo.spawnPos), Quaternion.identity);
            EnemyUnit enemy = enemyNO.GetComponent<EnemyUnit>();
            enemy.position = spawnInfo.spawnPos;
            
            enemies.Add(enemy);
            gridManager.RegisterUnit(enemy, spawnInfo.spawnPos);
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

    public void RegisterEnemy(EnemyUnit enemy) { if (!enemies.Contains(enemy)) enemies.Add(enemy); }
    public void UnregisterEnemy(EnemyUnit enemy) { if (enemies.Contains(enemy)) enemies.Remove(enemy); }
    public PlayerUnit GetPlayer() { return player; }
    public List<EnemyUnit> GetEnemies() { return enemies; }
    public StageData GetCurrentStageData() { return currentStageData; }
    public NetworkStageInfo GetCurrentStageInfo() { return CurrentStageInfo; }

    public List<NetworkEnemySpawnInfo> GetEnemySpawnsForTurn(int turnNumber)
    {
        var spawns = new List<NetworkEnemySpawnInfo>();
        for (int i = 0; i < EnemySpawnQueue.Length; i++)
        {
            var spawnInfo = EnemySpawnQueue.Get(i);
            if (spawnInfo.turnNumber == turnNumber && spawnInfo.enemyTypeIndex > 0)
            {
                spawns.Add(spawnInfo);
            }
        }
        return spawns;
    }

    public void CreatePortals(List<StageType> portalTypes)
    {
        if (portalPrefab == null)
        {
            Debug.LogError("Portal Prefab is not assigned in StageManager!");
            return;
        }

        Vector3[] spawnPositions = {
            new Vector3(currentStageData.width / 2f, 0.5f, currentStageData.height + 1),
            new Vector3(-1, 0.5f, currentStageData.height / 2f),
            new Vector3(currentStageData.width + 1, 0.5f, currentStageData.height / 2f)
        };

        for (int i = 0; i < portalTypes.Count && i < spawnPositions.Length; i++)
        {
            GameObject portalObj = Instantiate(portalPrefab, spawnPositions[i], Quaternion.identity);
            Portal portal = portalObj.GetComponent<Portal>();
            portal.Initialize(portalTypes[i]);
            spawnedPortals.Add(portalObj);
        }
    }

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
}