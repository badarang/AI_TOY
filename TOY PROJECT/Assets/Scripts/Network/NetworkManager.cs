using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network Settings")]
    [SerializeField] private int waitingRoomSceneIndex = 1;
    [SerializeField] private NetworkRunner runnerPrefab;

    private NetworkRunner _runner;
    private bool _isStarting;

    public NetworkRunner Runner => _runner;
    public bool IsHost => _runner != null && _runner.IsServer;
    public bool IsConnected => _runner != null && _runner.IsRunning;

    public event Action<NetworkRunner> OnRunnerStarted;
    public event Action<ShutdownReason> OnRunnerShutdown;
    public event Action OnSceneLoadComplete;

    private void Awake()
    {
        if (FindObjectsOfType<NetworkManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    public async void StartHost(string roomName)
    {
        if (_isStarting)
        {
            Debug.LogWarning("[NetworkManager] Already starting game");
            return;
        }

        _isStarting = true;

        try
        {
            await StartGameAsync(GameMode.Host, roomName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] Failed to start host: {e.Message}");
            _isStarting = false;
        }
    }

    public async void JoinRoom(string roomName)
    {
        if (_isStarting)
        {
            Debug.LogWarning("[NetworkManager] Already starting game");
            return;
        }

        _isStarting = true;

        try
        {
            await StartGameAsync(GameMode.Client, roomName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] Failed to join room: {e.Message}");
            _isStarting = false;
        }
    }

    private async Task StartGameAsync(GameMode mode, string sessionName)
    {
        if (_runner == null)
        {
            _runner = runnerPrefab != null ? Instantiate(runnerPrefab) : gameObject.AddComponent<NetworkRunner>();
            _runner.name = "NetworkRunner";
            DontDestroyOnLoad(_runner.gameObject);

            // Add this NetworkManager instance to the runner's callback list.
            _runner.AddCallbacks(this);
        }

        _runner.ProvideInput = true;

        var scene = SceneRef.FromIndex(waitingRoomSceneIndex);
        
        // Ensure there is a NetworkSceneManagerDefault on the runner's GameObject.
        var sceneManager = _runner.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
        {
            sceneManager = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = sceneManager
        };

        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log($"[NetworkManager] Successfully started as {mode}");
            _isStarting = false;
            OnRunnerStarted?.Invoke(_runner);
        }
        else
        {
            Debug.LogError($"[NetworkManager] Failed to start: {result.ShutdownReason}");
            _isStarting = false;
        }
    }

    public void LeaveSession()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkManager] Player joined: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkManager] Player left: {player}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[NetworkManager] Shutdown: {shutdownReason}");

        _isStarting = false;
        OnRunnerShutdown?.Invoke(shutdownReason);

        if (shutdownReason != ShutdownReason.Ok)
        {
            SceneManager.LoadScene("Lobby");
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[NetworkManager] Connected to server");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[NetworkManager] Disconnected: {reason}");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[NetworkManager] Scene load done");
        OnSceneLoadComplete?.Invoke();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[NetworkManager] Scene load start");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}