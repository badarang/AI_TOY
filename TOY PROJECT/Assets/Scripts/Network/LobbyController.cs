using Fusion;
using UnityEngine;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private LobbyUIController _lobbyUIController;

    [Header("Settings")]
    [SerializeField] private int minRoomNameLength = 3;
    [SerializeField] private int maxRoomNameLength = 20;

    private NetworkManager _networkManager;

    private void Start()
    {
        _networkManager = PersistentCore.Instance.NetworkManager;
        RegisterCallbacks();
        _lobbyUIController.SetupUI();
    }

    private void RegisterCallbacks()
    {
        if (_networkManager != null)
        {
            _networkManager.OnRunnerStarted += OnNetworkStarted;
            _networkManager.OnRunnerShutdown += OnNetworkShutdown;
        }
    }

    public bool ValidateRoomName(string roomName, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(roomName))
        {
            errorMessage = "Room name cannot be empty";
            return false;
        }

        if (roomName.Length < minRoomNameLength)
        {
            errorMessage = $"Room name must be at least {minRoomNameLength} characters";
            return false;
        }

        if (roomName.Length > maxRoomNameLength)
        {
            errorMessage = $"Room name must be less than {maxRoomNameLength} characters";
            return false;
        }

        return true;
    }

    public void CreateRoom(string roomName)
    {
        if (!ValidateRoomName(roomName, out string error))
        {
            Debug.LogError($"[LobbyController] Invalid room name: {error}");
            return;
        }

        Debug.Log($"[LobbyController] Creating room: {roomName}");
        _networkManager.StartHost(roomName);
    }

    public void JoinRoom(string roomName)
    {
        if (!ValidateRoomName(roomName, out string error))
        {
            Debug.LogError($"[LobbyController] Invalid room name: {error}");
            return;
        }

        Debug.Log($"[LobbyController] Joining room: {roomName}");
        _networkManager.JoinRoom(roomName);
    }

    private void OnNetworkStarted(NetworkRunner runner)
    {
        Debug.Log($"[LobbyController] Network started successfully. Mode: {(runner.IsServer ? "Host" : "Client")}");
    }

    private void OnNetworkShutdown(ShutdownReason reason)
    {
        Debug.Log($"[LobbyController] Network shutdown: {reason}");
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
        {
            _networkManager.OnRunnerStarted -= OnNetworkStarted;
            _networkManager.OnRunnerShutdown -= OnNetworkShutdown;
        }
    }
}