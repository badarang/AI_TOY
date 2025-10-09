using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Linq;
using Fusion.Sockets; // For INetworkRunnerCallbacks

public class NetworkManager : MonoBehaviour, IManager, INetworkRunnerCallbacks
{
    public static NetworkManager Instance { get; private set; }

    [Header("Network Settings")]
    [SerializeField] private NetworkRunner networkRunnerPrefab;
    
    private NetworkRunner currentRunner;

    public NetworkRunner CurrentRunner => currentRunner;
    public bool IsConnected => currentRunner != null && currentRunner.IsRunning;

    public void BeforeInit()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void AfterInit()
    {
        Debug.Log("[NetworkManager] Network manager ready.");
    }

    public async void StartHost(string roomName)
    {
        if (currentRunner != null)
        {
            Debug.LogWarning("[NetworkManager] Already connected to a session.");
            return;
        }

        currentRunner = Instantiate(networkRunnerPrefab);
        currentRunner.AddCallbacks(this);
        currentRunner.name = "NetworkRunner";

        var sceneManager = currentRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            SceneManager = sceneManager
        };

        var result = await currentRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log($"[NetworkManager] Started as Host in room: {roomName}");
        }
        else
        {
            Debug.LogError($"[NetworkManager] Failed to start Host: {result.ShutdownReason}");
            Destroy(currentRunner.gameObject);
            currentRunner = null;
        }
    }

    public async void JoinRoom(string roomName)
    {
        if (currentRunner != null)
        {
            Debug.LogWarning("[NetworkManager] Already connected to a session.");
            return;
        }

        currentRunner = Instantiate(networkRunnerPrefab);
        currentRunner.AddCallbacks(this);
        currentRunner.name = "NetworkRunner";

        var sceneManager = currentRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = roomName,
            SceneManager = sceneManager
        };

        var result = await currentRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log($"[NetworkManager] Joined room: {roomName}");
        }
        else
        {
            Debug.LogError($"[NetworkManager] Failed to join room: {result.ShutdownReason}");
            Destroy(currentRunner.gameObject);
            currentRunner = null;
        }
    }

    public void LoadSceneNetwork(string sceneName)
    {
        if (currentRunner == null || !currentRunner.IsServer)
        {
            Debug.LogError("[NetworkManager] Only Host can load scenes!");
            return;
        }

        int sceneIndex = GetSceneIndex(sceneName);
        if (sceneIndex == -1)
        {
            Debug.LogError($"[NetworkManager] Scene '{sceneName}' not found in Build Settings!");
            return;
        }

        Debug.Log($"[NetworkManager] Loading scene for all players: {sceneName} (Index: {sceneIndex})");
        currentRunner.LoadScene(SceneRef.FromIndex(sceneIndex));
    }

    private int GetSceneIndex(string sceneName)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (name == sceneName)
            {
                return i;
            }
        }

        return -1;
    }

    public void LeaveRoom()
    {
        if (currentRunner != null)
        {
            currentRunner.Shutdown();
            Destroy(currentRunner.gameObject);
            currentRunner = null;
            Debug.Log("[NetworkManager] Left the room.");
        }
    }

    private void OnDestroy()
    {
        LeaveRoom();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region INetworkRunnerCallbacks

public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            Debug.Log($"[NetworkManager] OnPlayerJoined on Server. Player Ref: {player}. Total Players: {runner.ActivePlayers.Count()}");

            // This logic assumes the InGame scene is already loaded.
            // In a more complex flow, you might need to wait for the scene to be loaded.
            var stageManager = Core.Instance?.StageManager;
            if (stageManager == null)
            {
                Debug.LogError("[NetworkManager] StageManager not found, cannot spawn player. Make sure the game scene is loaded.");
                return;
            }

            // Determine unit type and spawn position based on player order
            UnitType unitType;
            Vector2Int spawnPosition;
            int playerIndex = runner.ActivePlayers.Count();

            if (playerIndex == 1)
            {
                // First player (Host) - Player 1
                unitType = UnitType.Player_Hikai;
                spawnPosition = new Vector2Int(0, 0); // TODO: Get from stage data
                Debug.Log($"[NetworkManager] Spawning Player 1 (Host): {unitType} for {player} at {spawnPosition}");
            }
            else if (playerIndex == 2)
            {
                // Second player (Client) - Player 2
                unitType = UnitType.Player_Vrixa;
                spawnPosition = new Vector2Int(0, 1); // TODO: Get from stage data
                Debug.Log($"[NetworkManager] Spawning Player 2 (Client): {unitType} for {player} at {spawnPosition}");
            }
            else
            {
                Debug.LogWarning($"[NetworkManager] More than 2 players joined. Player {player} will not be spawned.");
                return;
            }
            
            stageManager.SpawnPlayer(player, unitType, spawnPosition);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }


    #endregion
}