using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using Network;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class UnitManager : MonoBehaviour, IManager
{
    [Header("Dependencies")]
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private StageManager stageManager;

    private NetworkManager _networkManager;
    private GameSession _session;
    private List<EnemyUnit> _spawnedEnemies = new List<EnemyUnit>();

    public List<EnemyUnit> SpawnedEnemies => _spawnedEnemies;

public void BeforeInit()
    {
        if (PersistentCore.Instance != null)
        {
            _networkManager = PersistentCore.Instance.NetworkManager;
        }
        else
        {
            Debug.LogWarning("[UnitManager] PersistentCore not available during BeforeInit. Will retry in AfterInit.");
        }
    }

public void AfterInit()
    {
        if (_networkManager == null && PersistentCore.Instance != null)
        {
            _networkManager = PersistentCore.Instance.NetworkManager;
        }
        
        _session = GameSession.Instance;
    }

    public async UniTask SpawnPlayers()
    {
        if (!_networkManager.IsHost)
            return;

        int playerIndex = 0;
        foreach (PlayerRef player in _networkManager.Runner.ActivePlayers)
        {
            int slotIndex = _session.GetPlayerSlotIndex(player);
            if (slotIndex == -1)
                continue;

            var slot = _session.PlayerSlots[slotIndex];
            UnitType unitType = slot.SelectedUnit;
            Vector2Int spawnPos = GetPlayerSpawnPosition(playerIndex);

            await SpawnPlayer(player, unitType, spawnPos);
            playerIndex++;
        }
    }

    public async UniTask SpawnPlayer(
        PlayerRef playerRef,
        UnitType unitType,
        Vector2Int spawnPosition
    )
    {
        if (!_networkManager.IsHost)
            return;

        string prefabKey = GetPrefabName(unitType);
        if (string.IsNullOrEmpty(prefabKey))
        {
            Debug.LogError($"[UnitSpawner] Invalid unit type: {unitType}");
            return;
        }

        var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
        GameObject prefab = await handle.Task;

        if (prefab == null || prefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError($"[UnitSpawner] Invalid prefab or missing NetworkObject: {prefabKey}");
            Addressables.Release(handle);
            return;
        }

        NetworkObject playerNO = await _networkManager.Runner.SpawnAsync(
            prefab,
            GridToWorld(spawnPosition),
            Quaternion.identity,
            playerRef
        );
        
        Addressables.Release(handle);

        if (playerNO == null)
        {
            Debug.LogError($"[UnitSpawner] Failed to spawn player");
            return;
        }

        var playerUnit = playerNO.GetComponent<PlayerUnit>();
        if (playerUnit != null)
        {
            playerUnit.Initialize(spawnPosition, playerRef);
            gridManager.RegisterUnit(playerUnit, spawnPosition);
        }
    }
    
    public async UniTask SpawnEnemiesForTurn(int turnNumber)
    {
        if (!_networkManager.IsHost)
            return;

        var enemySpawns = GetEnemySpawnsForTurn(turnNumber);
        if (enemySpawns == null || enemySpawns.Count == 0)
        {
            // 이 로그가 출력된다면, GetEnemySpawnsForTurn 내부의 진단 로그를 확인해야 합니다.
            Debug.LogWarning($"[UnitManager] No enemies found to spawn for turn {turnNumber}. Check StageData asset.");
            return;
        }

        Debug.Log($"[UnitManager] Found {enemySpawns.Count} enemies to spawn for turn {turnNumber}");

        List<UniTask> tasks = new List<UniTask>();
        foreach (var spawnData in enemySpawns)
        {
            tasks.Add(SpawnEnemy(spawnData.enemyType, spawnData.spawnPos));
        }

        await UniTask.WhenAll(tasks);
    }

    private async UniTask SpawnEnemy(UnitType enemyType, Vector2Int spawnPos)
    {
        string prefabKey = GetPrefabName(enemyType);
        if (string.IsNullOrEmpty(prefabKey))
            return;

        var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
        GameObject prefab = await handle.Task;

        if (prefab == null || prefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError($"[UnitManager] Failed to load enemy prefab or it's missing a NetworkObject component: {prefabKey}");
            Addressables.Release(handle);
            return;
        }

        NetworkObject enemyNO = await _networkManager.Runner.SpawnAsync(
            prefab,
            GridToWorld(spawnPos),
            Quaternion.identity
        );

        Addressables.Release(handle);

        if (enemyNO != null)
        {
            EnemyUnit enemy = enemyNO.GetComponent<EnemyUnit>();
            enemy.Initialize(spawnPos);
            _spawnedEnemies.Add(enemy);
            gridManager.RegisterUnit(enemy, spawnPos);
        }
        else
        {
            Debug.LogError($"[UnitManager] Failed to spawn enemy: {prefabKey}");
        }
    }

    public void DespawnEnemy(EnemyUnit enemy)
    {
        if (!_networkManager.IsHost)
            return;

        if (_spawnedEnemies.Contains(enemy))
        {
            _spawnedEnemies.Remove(enemy);
        }

        if (enemy.Object != null)
        {
            _networkManager.Runner.Despawn(enemy.Object);
        }
    }

    public void ClearAllEnemies()
    {
        if (!_networkManager.IsHost)
            return;

        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null && enemy.Object != null)
            {
                _networkManager.Runner.Despawn(enemy.Object);
            }
        }
        _spawnedEnemies.Clear();
    }

    public void UnregisterEnemy(EnemyUnit enemy)
    {
        if (enemy == null) return;

        if (_spawnedEnemies.Contains(enemy))
        {
            _spawnedEnemies.Remove(enemy);
        }
        gridManager.UnregisterUnit(enemy.position);
    }

    public List<PlayerUnit> GetAllPlayers()
    {
        return gridManager.GetAllUnits().OfType<PlayerUnit>().ToList();
    }

    public List<EnemyUnit> GetEnemies()
    {
        return _spawnedEnemies;
    }

    private List<EnemySpawnData> GetEnemySpawnsForTurn(int turnNumber)
    {
        var spawns = new List<EnemySpawnData>();
        var stageData = stageManager.CurrentStageData;
        if (stageData == null) {
            Debug.LogError("[UnitManager-Diagnosis] stageData is NULL.");
            return spawns;
        }

        int waveIndex = _session.CurrentWaveIndex;
        if (waveIndex >= stageData.waves.Length) {
            Debug.LogError($"[UnitManager-Diagnosis] CurrentWaveIndex ({waveIndex}) is out of bounds. StageData only has {stageData.waves.Length} waves.");
            return spawns;
        }

        var wave = stageData.waves[waveIndex];
        if (wave.turnSpawns == null || wave.turnSpawns.Length == 0) {
            Debug.LogError("[UnitManager-Diagnosis] The first wave in StageData has an empty 'turnSpawns' array.");
            return spawns;
        }

        bool foundTurnData = false;
        foreach (var turnSpawn in wave.turnSpawns)
        {
            if (turnSpawn.turnNumber == turnNumber)
            {
                foundTurnData = true;
                if (turnSpawn.enemies == null || turnSpawn.enemies.Length == 0) {
                    Debug.LogError($"[UnitManager-Diagnosis] Found data for Turn {turnNumber}, but its 'enemies' array is empty.");
                } else {
                    spawns.AddRange(turnSpawn.enemies);
                }
                break; // 해당 턴 데이터를 찾았으므로 루프 종료
            }
        }

        if (!foundTurnData) {
            Debug.LogError($"[UnitManager-Diagnosis] Could not find any data for Turn {turnNumber} in the first wave.");
        }

        return spawns;
    }

    private Vector2Int GetPlayerSpawnPosition(int playerIndex)
    {
        Vector2Int[] spawnPositions = new Vector2Int[]
        {
            new Vector2Int(1, 1),
            new Vector2Int(2, 1),
        };

        return playerIndex < spawnPositions.Length
            ? spawnPositions[playerIndex]
            : new Vector2Int(playerIndex, 1);
    }

    private string GetPrefabName(UnitType type)
    {
        switch (type)
        {
            case UnitType.Hikai:
                return "Player_Hikai";
            case UnitType.Vrixa:
                return "Player_Vrixa";
            case UnitType.Enemy_Goose:
                return "Enemy_Goose";
            default:
                return null;
        }
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x + 0.5f, 0, gridPos.y + 0.5f);
    }
}
