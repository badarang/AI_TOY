
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

    // --- 사용자 변경 3: SpawnAllPlayers ---
    public void SpawnAllPlayers()
    {
        if (!currentRunner.IsServer) return;
        if (SceneManager.GetActiveScene().name != "InGame") return;

        Debug.Log("Spawning all players...");
        foreach (PlayerRef player in currentRunner.ActivePlayers)
        {
            Vector3 spawnPosition = GetSpawnPosition(player.PlayerId);
            Debug.Log($"Spawning player {player.PlayerId} at {spawnPosition}");
            // 플레이어에게 입력 권한을 부여하여 스폰합니다.
            currentRunner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
        }
    }
    
    private Vector3 GetSpawnPosition(int playerActorNumber)
    {
        // 스폰 위치 로직을 위한 플레이스홀더입니다.
        return new Vector3((playerActorNumber - 1) * 3.0f, 1, 0);
    }

    #region INetworkRunnerCallbacks

    // --- 사용자 변경 1: OnPlayerJoined ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player joined: {player.PlayerId}");

        // 호스트이고 첫 번째 플레이어인 경우 WaitingRoom으로 이동합니다.
        if (runner.IsServer && runner.ActivePlayers.Count() == 1)
        {
            Debug.Log("First player joined, loading WaitingRoom...");
            LoadSceneNetwork("WaitingRoom");
        }
    }

    // --- 사용자 변경 3: OnSceneLoadDone ---
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log($"Scene load done. Current scene: {SceneManager.GetActiveScene().name}");
        if (SceneManager.GetActiveScene().name == "InGame")
        {
            // 호스트만 플레이어를 스폰합니다.
            if (runner.IsServer)
            {
                SpawnAllPlayers();
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
