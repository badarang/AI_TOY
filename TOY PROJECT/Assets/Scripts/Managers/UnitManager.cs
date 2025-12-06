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
    [SerializeField] private GridManager gridManager;

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

    public void Dispose() { }

    public async UniTask SpawnPlayers()
    {
        if (!_networkManager.IsHost || _session == null)
            return;

        Debug.Log($"[UnitManager] Spawning players based on GameSession. Connected players: {_session.ConnectedPlayerCount}");

        var spawnTasks = new List<UniTask>();
        int playerIndex = 0;
        for (int i = 0; i < GameSession.MAX_PLAYERS; i++)
        {
            var slot = _session.PlayerSlots[i];
            if (slot.IsConnected)
            {
                PlayerRef player = slot.PlayerRef;
                UnitType unitType = slot.SelectedUnit;
                Vector2Int spawnPos = GetPlayerSpawnPosition(playerIndex);

                Debug.Log($"[UnitManager] Spawning player {player} (Slot {i}) of type {unitType} at {spawnPos}");
                spawnTasks.Add(SpawnPlayer(player, unitType, spawnPos));
                playerIndex++;
            }
        }
        await UniTask.WhenAll(spawnTasks);
        Debug.Log($"[UnitManager] Finished spawning {playerIndex} players.");
    }

    public async UniTask SpawnPlayer(
        PlayerRef playerRef,
        UnitType unitType,
        Vector2Int spawnPosition)
    {
        if (!_networkManager.IsHost)
            return;

        string prefabKey = GetPrefabName(unitType);
        if (string.IsNullOrEmpty(prefabKey))
        {
            Debug.LogError($"[UnitManager] Invalid unit type: {unitType}");
            return;
        }

        var handle = Addressables.LoadAssetAsync<GameObject>($"Prefabs/Units/{prefabKey}.prefab");
        GameObject prefab = await handle.Task;

        if (prefab == null || prefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError($"[UnitManager] Invalid prefab or missing NetworkObject: {prefabKey}");
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
            Debug.LogError($"[UnitManager] Failed to spawn player");
            return;
        }

        var playerUnit = playerNO.GetComponent<PlayerUnit>();
        if (playerUnit != null)
        {
            Debug.Log($"[UnitManager] Initializing PlayerUnit for {playerRef}");
            playerUnit.Initialize(spawnPosition, playerRef);
            gridManager.RegisterUnit(playerUnit, spawnPosition);
            Debug.Log($"[UnitManager] PlayerUnit registered at {spawnPosition}");
        }
        else
        {
            Debug.LogError($"[UnitManager] PlayerUnit component not found on spawned object!");
        }
    }

    public async UniTask SpawnEnemiesImmediate(EnemySpawnData[] enemies)
    {
        if (!_networkManager.IsHost)
            return;

        Debug.Log($"[UnitManager] Spawning {enemies.Length} enemies immediately");

        List<UniTask> tasks = new List<UniTask>();
        foreach (var enemy in enemies)
        {
            tasks.Add(SpawnEnemy(enemy.enemyType, enemy.spawnPos));
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

    public void KillAllEnemies()
    {
        // To avoid modifying the list while iterating, create a copy.
        var enemiesToKill = new List<EnemyUnit>(_spawnedEnemies);
        foreach (var enemy in enemiesToKill)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(enemy.hp); // 유닛의 현재 체력만큼 데미지를 주어 즉시 처치
            }
        }
    }

    public void UnregisterEnemy(EnemyUnit enemy)
    {
        if (enemy == null) return;

        if (_spawnedEnemies.Contains(enemy))
        {
            _spawnedEnemies.Remove(enemy);
        }
        gridManager.UnregisterUnit(enemy.position);

        // 모든 적이 죽었는지 확인
        if (_spawnedEnemies.Count == 0)
        {
            Debug.Log("[UnitManager] All enemies have been defeated!");
            if (Core.Instance?.EventManager != null)
            {
                Core.Instance.EventManager.TriggerAllEnemiesDied();
            }
        }
    }

    public List<PlayerUnit> GetAllPlayers()
    {
        return gridManager.GetAllUnits().OfType<PlayerUnit>().ToList();
    }

    public List<EnemyUnit> GetEnemies()
    {
        return _spawnedEnemies;
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
