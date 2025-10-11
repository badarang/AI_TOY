
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour, IManager, INetworkRunnerCallbacks
{
    public static NetworkManager Instance { get; private set; }

    [Header("Network Settings")]
    [SerializeField] private NetworkRunner networkRunnerPrefab;
    [SerializeField] private NetworkObject playerPrefab; // 플레이어 프리팹 참조

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
            SceneManager = sceneManager,
        };

        var result = await currentRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log($"[NetworkManager] Started as Host in room: {roomName}");
        }
        else
        {
            Debug.LogError($"[NetworkManager] Failed to start Host: {result.ShutdownReason}");
            if (currentRunner != null)
            {
                Destroy(currentRunner.gameObject);
                currentRunner = null;
            }
            return;
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
            SceneManager = sceneManager,
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
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
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
        Debug.Log($"Player joined: {player.PlayerId}");

        if (runner.IsServer && runner.ActivePlayers.Count() == 1)
        {
            Debug.Log("First player joined, loading WaitingRoom...");
            LoadSceneNetwork("WaitingRoom");
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log($"Scene load done. Current scene: {SceneManager.GetActiveScene().name}");
        if (SceneManager.GetActiveScene().name == "InGame")
        {
            if (runner.IsServer)
            {
                Debug.Log("[NetworkManager] InGame scene loaded. Requesting StageManager to spawn players.");
                Core.Instance.StageManager.InitializePlayersRpc();
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    {
        Debug.Log($"Player left: {player.PlayerId}");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    
    #endregion
}
