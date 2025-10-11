using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Fusion;
using Network;

public class StageManager : MonoBehaviour, IManager
{
    [Header("Dependencies")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private GameAssetDatabase gameDatabase;

    private NetworkManager _networkManager;
    private GameSession _session;
    private StageData _currentStageData;
    private List<GameObject> _spawnedObstacles = new List<GameObject>();
    private List<GameObject> _spawnedPortals = new List<GameObject>();

    public StageData CurrentStageData => _currentStageData;
    public bool IsStageLoaded => _session != null && _session.IsStageLoaded;

    public void BeforeInit()
    {
        _networkManager = PersistentCore.Instance.NetworkManager;
    }

    public void AfterInit()
    {
        if (GameSession.Instance != null)
        {
            _session = GameSession.Instance;
            _session.OnStageChanged += OnStageChanged;
        }
    }

    private void OnStageChanged(string stageName)
    {
        LoadStageLocal(stageName);
    }

    public void RequestLoadStage(string stageName)
    {
        if (_networkManager == null || !_networkManager.IsHost)
        {
            Debug.LogWarning("[StageManager] Only host can load stages");
            return;
        }

        if (_session == null)
        {
            Debug.LogError("[StageManager] GameSession not found");
            return;
        }

        _session.LoadStageRpc(stageName);
        LoadStageLocal(stageName);
    }

    private async void LoadStageLocal(string stageName)
    {
        var stageData = gameDatabase.GetStageByName(stageName);
        if (stageData == null)
        {
            Debug.LogError($"[StageManager] Stage not found: {stageName}");
            return;
        }

        ClearStage();
        _currentStageData = stageData;

        if (IsBattleStage(stageData.stageType))
        {
            gridManager.GenerateGrid(stageData);
            SetupCamera(stageData.width, stageData.height);
            await SpawnObstacles(stageData);
        }

        if (_networkManager.IsHost && _session != null)
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

        gridManager.ClearGrid();
    }

    private async UniTask SpawnObstacles(StageData stage)
    {
        if (stage.obstacleSpawns == null)
            return;

        foreach (var obsData in stage.obstacleSpawns)
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

    public void CreatePortals(List<StageType> portalTypes)
    {
        if (portalPrefab == null)
        {
            Debug.LogError("[StageManager] Portal prefab not assigned");
            return;
        }

        Vector3[] spawnPositions = {
            new Vector3(_currentStageData.width / 2f, 0.5f, _currentStageData.height + 1),
            new Vector3(-1, 0.5f, _currentStageData.height / 2f),
            new Vector3(_currentStageData.width + 1, 0.5f, _currentStageData.height / 2f)
        };

        for (int i = 0; i < portalTypes.Count && i < spawnPositions.Length; i++)
        {
            GameObject portalObj = Instantiate(portalPrefab, spawnPositions[i], Quaternion.identity);
            Portal portal = portalObj.GetComponent<Portal>();
            portal.Initialize(portalTypes[i]);
            _spawnedPortals.Add(portalObj);
        }
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

    private bool IsBattleStage(StageType type)
    {
        return type == StageType.Battle ||
               type == StageType.EliteBattle ||
               type == StageType.Boss;
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
}