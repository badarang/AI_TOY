using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using Network;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StageManager : MonoBehaviour, IManager
{
    [Header("Dependencies")]
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private UnitManager unitManager;

    [SerializeField]
    private GameObject portalPrefab;

    [SerializeField]
    private GameAssetDatabase gameDatabase;

    [Header("Settings")]
    [SerializeField]
    private string firstStageName = "Room_1";

    private int _currentWaveIndex = -1;
    private HashSet<int> _spawnedWaves = new HashSet<int>();

    private NetworkManager _networkManager;
    private GameSession _session;
    private Room _currentRoom;
    private List<GameObject> _spawnedObstacles = new List<GameObject>();
    private List<GameObject> _spawnedPortals = new List<GameObject>();

    public Room CurrentRoom => _currentRoom;
    public bool IsStageLoaded => _session != null && _session.IsStageLoaded;

    public void BeforeInit()
    {
        if (PersistentCore.Instance != null)
        {
            _networkManager = PersistentCore.Instance.NetworkManager;
        }
        else
        {
            Debug.LogWarning(
                "[StageManager] PersistentCore not available during BeforeInit. Will retry in AfterInit."
            );
        }
    }

    public void AfterInit()
    {
        if (_networkManager == null && PersistentCore.Instance != null)
        {
            _networkManager = PersistentCore.Instance.NetworkManager;
        }

        if (GameSession.Instance == null)
        {
            Debug.LogWarning("[StageManager] GameSession.Instance is null in AfterInit.");
            return;
        }

        _session = GameSession.Instance;
        _session.OnStageChanged += OnStageChanged;

        // 모든 적이 죽었을 때 이벤트 구독
        if (Core.Instance?.EventManager != null)
        {
            Core.Instance.EventManager.OnAllEnemiesDied += () => OnAllEnemiesDiedAsync().Forget();
        }

        if (
            _networkManager != null
            && _networkManager.IsHost
            && string.IsNullOrEmpty(_session.CurrentStageName.Value)
        )
        {
            RequestLoadStage(firstStageName);
        }
    }

    public void Dispose() { }

    private void OnStageChanged(string stageName)
    {
        if (_currentRoom != null && _currentRoom.name == stageName)
            return;

        LoadStageLocal(stageName);
    }

    private async UniTask OnAllEnemiesDied()
    {
        Debug.Log("[StageManager] All enemies defeated!");
        await OnAllEnemiesDiedAsync();
    }

    private async UniTask OnAllEnemiesDiedAsync()
    {
        await UniTask.Delay(500);

        // 모든 플레이어의 AP와 스킬 쿨타임 완전 회복
        var allPlayers = unitManager.GetAllPlayers();
        foreach (var player in allPlayers)
        {
            if (player != null && player.unitData != null)
            {
                player.ap = player.unitData.maxAp;
                
                // 모든 스킬 쿨타임 초기화
                var skills = player.GetSkills();
                for (int i = 0; i < skills.Count; i++)
                {
                    skills[i].currentCooldown = 0;
                }
            }
        }

        await CreatePortalsAsync();
        Core.Instance.TurnManager.SetBattleEnded();
        await UniTask.WaitUntil(() => AllPlayersAtPortal());

        int nextRoomIndex = GetNextRoomIndex();
        if (nextRoomIndex >= 0)
        {
            RequestLoadStage(GetRoomNameByIndex(nextRoomIndex));
        }
    }

    private bool AllPlayersAtPortal()
    {
        var allPlayers = Core.Instance.UnitManager.GetAllPlayers();

        if (allPlayers.Count == 0)
            return false;

        // 모든 플레이어가 포탈 영역(그리드 밖)에 있는지 확인
        // displayWidth/displayHeight 범위 밖에 있으면 포탈 영역
        foreach (var player in allPlayers)
        {
            // 포탈 영역: displayWidth/displayHeight 범위 밖
            bool isInPortalArea = player.position.x < 0 ||
                                  player.position.x >= _currentRoom.width ||
                                  player.position.y < 0 ||
                                  player.position.y >= _currentRoom.height;

            if (!isInPortalArea)
                return false;  // 아직 포탈에 도달하지 않은 플레이어 있음
        }

        return true;  // 모든 플레이어가 포탈 영역에 있음
    }

    private int GetNextRoomIndex()
    {
        // TODO: 방당 렌드 로직 추가
        return 0;
    }

    private string GetRoomNameByIndex(int index)
    {
        // TODO: 뢬 리스트에서 리단 이름 가져오기
        return "Room_1";
    }

    public void RequestLoadStage(string stageName)
    {
        if (_networkManager == null || !_networkManager.IsHost || _session == null)
        {
            return;
        }

        _session.LoadStageRpc(stageName);
    }

    private async void LoadStageLocal(string stageName)
    {
        var stageData = gameDatabase.GetRoomByName(stageName);
        if (stageData == null)
        {
            Debug.LogError($"[StageManager] Stage not found: {stageName}");
            return;
        }

        ClearStage();
        _currentRoom = stageData;

        if (IsBattleStage(stageData.type))
        {
            gridManager.GenerateGrid(stageData);
            SetupCamera(stageData.width, stageData.height);
            await SpawnObstacles(stageData);

            if (_networkManager.IsHost)
            {
                if (unitManager == null)
                {
                    Debug.LogError("[StageManager] UnitManager is not assigned!");
                    return;
                }

                Debug.Log("[StageManager] Starting player spawn...");
                await unitManager.SpawnPlayers();

                // Wait until all players registered in the session are spawned
                if (_session != null && _session.ConnectedPlayerCount > 0)
                {
                    Debug.Log(
                        $"[StageManager] Waiting for {_session.ConnectedPlayerCount} players to be spawned..."
                    );
                    await UniTask.WaitUntil(() =>
                        unitManager.GetAllPlayers().Count >= _session.ConnectedPlayerCount
                    );
                    Debug.Log(
                        $"[StageManager] All {unitManager.GetAllPlayers().Count} players spawned."
                    );
                }

                Debug.Log("[StageManager] Starting enemy spawn...");
                if (stageData.waves != null && stageData.waves.Length > 0)
                {
                    await unitManager.SpawnEnemiesImmediate(stageData.waves[0].enemySpawns);
                    _spawnedWaves.Add(stageData.waves[0].GetHashCode());
                    _currentWaveIndex = 0;
                }

                // This delay might not be necessary, but can be kept for safety.
                await UniTask.Delay(100);

                var allPlayers = unitManager.GetAllPlayers();
                Debug.Log($"[StageManager] Found {allPlayers.Count} players before StartCombat");
                foreach (var player in allPlayers)
                {
                    Debug.Log($"[StageManager] Player: {player.name} at {player.position}");
                }

                Debug.Log("[StageManager] Starting combat...");
                Core.Instance.TurnManager.StartCombat();
            }
        }

        if (_session != null)
        {
            _session.SetStageLoadedRpc(true);
        }

        Debug.Log($"[StageManager] Stage loaded: {stageName}");
    }

    private void ClearStage()
    {
        foreach (var obstacle in _spawnedObstacles)
        {
            if (obstacle != null)
                Destroy(obstacle);
        }

        _spawnedObstacles.Clear();

        foreach (var portal in _spawnedPortals)
        {
            if (portal != null)
                Destroy(portal);
        }

        _spawnedPortals.Clear();
        _spawnedWaves.Clear();
        _currentWaveIndex = -1;

        gridManager.ClearGrid();
    }

    private async UniTask SpawnObstacles(Room room)
    {
        if (room.obstacleSpawns == null)
            return;

        foreach (var obsData in room.obstacleSpawns)
        {
            if (obsData.obstacleData == null)
                continue;

            var handle = Addressables.LoadAssetAsync<GameObject>(
                $"Prefabs/Obstacles/{obsData.obstacleData.unitMeta.nameKey}.prefab"
            );

            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                continue;

            GameObject obj = Instantiate(
                handle.Result,
                GridToWorld(obsData.spawnPos),
                Quaternion.identity
            );

            Obstacle obstacle = obj.GetComponent<Obstacle>();
            if (obstacle != null)
                obstacle.data = obsData.obstacleData;

            _spawnedObstacles.Add(obj);
        }
    }

    private async UniTask CreatePortalsAsync()
    {
        if (portalPrefab == null)
        {
            Debug.LogError("[StageManager] Portal prefab not assigned");
            return;
        }

        if (_currentRoom == null)
        {
            Debug.LogError("[StageManager] Current room is null");
            return;
        }

        // 플레이어 위치 가져오기
        var players = unitManager.GetAllPlayers();
        if (players.Count == 0)
        {
            Debug.LogWarning("[StageManager] No players found for portal placement");
            return;
        }

        // 첫 번째 플레이어 위치 기준 (여러 플레이어면 평균 위치 계산 가능)
        Vector2Int playerPos = players[0].position;

        // 마름모 격자의 4개 가능한 포탈 위치 (7x7 기준: 3,0 / 0,3 / 3,6 / 6,3)
        int midX = _currentRoom.width / 2;
        int midY = _currentRoom.height / 2;
        
        Vector2Int[] possiblePortalPositions = new Vector2Int[]
        {
            new Vector2Int(midX, 0),                    // 북쪽
            new Vector2Int(0, midY),                    // 서쪽
            new Vector2Int(midX, _currentRoom.height - 1), // 남쪽
            new Vector2Int(_currentRoom.width - 1, midY)   // 동쪽
        };

        // 플레이어 위치에서 가장 가까운 2개 위치 찾기
        var sortedPositions = possiblePortalPositions
            .OrderBy(pos => Vector2Int.Distance(pos, playerPos))
            .Take(2)
            .ToList();

        Debug.Log($"[StageManager] Creating 2 portals closest to player at {playerPos}");

        for (int i = 0; i < sortedPositions.Count; i++)
        {
            Vector2Int gridPos = sortedPositions[i];
            Vector3 worldPos = new Vector3(gridPos.x + 0.5f, 0.5f, gridPos.y + 0.5f);

            GameObject portalObj = Instantiate(
                portalPrefab,
                worldPos,
                Quaternion.identity
            );

            Portal portal = portalObj.GetComponent<Portal>();
            if (portal != null)
            {
                // PortalData 생성
                var portalData = new PortalData
                {
                    displayText = $"Next Stage {i + 1}",
                    targetRoomIndex = i,
                    icon = null,
                };

                portal.Setup(
                    portalData,
                    (roomIndex) =>
                    {
                        Debug.Log($"[StageManager] Player entered portal to room {roomIndex}");
                        // 포탈 진입 처리
                    }
                );
            }

            _spawnedPortals.Add(portalObj);
            Debug.Log($"[StageManager] Portal {i} created at grid position {gridPos} (world: {worldPos})");
        }

        await UniTask.NextFrame();
        Debug.Log("[StageManager] 2 portals created successfully");
    }

    private void SetupCamera(int width, int height)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        var controller = cam.GetComponent<CameraController>();
        if (controller == null)
            return;

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

    private bool IsBattleStage(RoomType type)
    {
        return type == RoomType.Battle || type == RoomType.EliteBattle || type == RoomType.Boss;
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x + 0.5f, 0, gridPos.y + 0.5f);
    }

    private void OnDestroy()
    {
        if (_session != null)
        {
            _session.OnStageChanged -= OnStageChanged;
        }
    }

    public async void OnWaveComplete()
    {
        Debug.Log("[StageManager] Wave complete!");

        _currentWaveIndex++;

        if (_currentRoom?.waves != null && _currentWaveIndex < _currentRoom.waves.Length)
        {
            var nextWave = _currentRoom.waves[_currentWaveIndex];
            Debug.Log(
                $"[StageManager] Next wave available at turn {nextWave.spawnTurn}. Waiting..."
            );
            await UniTask.Delay(1000);
        }
        else
        {
            Debug.Log("[StageManager] No more waves! Battle complete!");
            CreatePortalsAsync().Forget();
        }
    }

    public async UniTask IncrementTurn()
    {
        if (_currentRoom?.waves == null)
            return;

        int currentTurn = Core.Instance.TurnManager.TurnNumber;

        foreach (var wave in _currentRoom.waves)
        {
            if (!_spawnedWaves.Contains(wave.GetHashCode()) && wave.spawnTurn == currentTurn)
            {
                await SpawnWaveAtTurn(wave);
            }
        }
    }

    private async UniTask SpawnWaveAtTurn(EnemyWave wave)
    {
        if (wave?.enemySpawns == null || wave.enemySpawns.Length == 0)
        {
            Debug.LogWarning("[StageManager] Wave has no enemy spawns!");
            return;
        }

        int waveHash = wave.GetHashCode();
        if (_spawnedWaves.Contains(waveHash))
        {
            Debug.LogWarning($"[StageManager] Wave {wave.spawnTurn} already spawned!");
            return;
        }

        Debug.Log($"[StageManager] Spawning wave: Wave {wave.spawnTurn}");
        _spawnedWaves.Add(waveHash);

        var playerPos = GetClosestPlayerPos();
        var sortedEnemies = wave
            .enemySpawns.OrderBy(e => Vector2Int.Distance(e.spawnPos, playerPos))
            .ThenBy(e => e.spawnPos.x)
            .ThenBy(e => e.spawnPos.y)
            .ToArray();

        await unitManager.SpawnEnemiesImmediate(sortedEnemies);

        Debug.Log($"[StageManager] Wave {wave.spawnTurn} spawned successfully!");
    }

    private Vector2Int GetClosestPlayerPos()
    {
        var players = unitManager.GetAllPlayers();
        if (players.Count == 0)
            return Vector2Int.zero;

        return players
            .OrderBy(p => (p.transform.position - new Vector3(0, 0, 0)).sqrMagnitude)
            .First()
            .Position;
    }
}
