#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Fusion;
using Network;

public class DevAutoStart : MonoBehaviour
{
    [Header("Auto Start Settings")]
    [SerializeField] private bool enableAutoStart = true;
    [SerializeField] private string defaultRoomName = "DevRoom";
    [SerializeField] private int lobbySceneIndex = 0;
    [SerializeField] private bool returnToInGameScene = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float maxWaitTimeSeconds = 15f;

    private NetworkManager _networkManager;
    private bool _isAutoStarting = false;
    private int _originalSceneIndex;

    private async void Start()
    {
        if (!enableAutoStart)
        {
            LogDebug("Auto-start disabled");
            return;
        }

        if (!Application.isEditor)
        {
            LogDebug("Not in editor, skipping auto-start");
            return;
        }

        if (ShouldAutoStart())
        {
            _originalSceneIndex = SceneManager.GetActiveScene().buildIndex;
            await AutoStartSequenceAsync();
        }
        else
        {
            LogDebug("Auto-start conditions not met, skipping");
        }
    }

    private bool ShouldAutoStart()
    {
        if (PersistentCore.Instance != null)
        {
            LogDebug("PersistentCore already exists");
            return false;
        }

        var networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null && networkManager.IsConnected)
        {
            LogDebug("Network already connected");
            return false;
        }

        if (GameSession.Instance != null)
        {
            LogDebug("GameSession already exists");
            return false;
        }

        LogDebug("Should auto-start: true");
        return true;
    }

    private async UniTask AutoStartSequenceAsync()
    {
        if (_isAutoStarting)
        {
            LogDebug("Already auto-starting");
            return;
        }

        _isAutoStarting = true;
        LogDebug("=== Starting Auto-Host Sequence ===");

        try
        {
            await LoadLobbySceneAsync();
            
            await EnsurePersistentCoreAsync();
            
            await EnsureNetworkManagerAsync();
            
            await StartHostAsync();
            
            await WaitForWaitingRoomSceneAsync();
            
            await WaitForGameSessionAsync();
            
            await AutoReadyAndStartAsync();

            LogDebug("=== Auto-Host Sequence Complete ===");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DevAutoStart] Auto-start failed: {e.Message}\n{e.StackTrace}");
            _isAutoStarting = false;
        }
    }

    private async UniTask LoadLobbySceneAsync()
    {
        LogDebug($"Loading Lobby scene (index: {lobbySceneIndex})...");
        
        var asyncOp = SceneManager.LoadSceneAsync(lobbySceneIndex, LoadSceneMode.Single);
        
        while (!asyncOp.isDone)
        {
            await UniTask.Yield();
        }

        LogDebug("Lobby scene loaded");
        await UniTask.Delay(500);
    }

    private async UniTask EnsurePersistentCoreAsync()
    {
        LogDebug("Waiting for PersistentCore...");
        
        float elapsed = 0f;
        while (PersistentCore.Instance == null)
        {
            if (elapsed > maxWaitTimeSeconds)
            {
                Debug.LogError("[DevAutoStart] Timeout waiting for PersistentCore. Make sure PersistentCore exists in Lobby scene!");
                throw new System.TimeoutException("PersistentCore not found");
            }

            await UniTask.Yield();
            elapsed += Time.deltaTime;
        }

        LogDebug("PersistentCore ready");
        await UniTask.Delay(300);
    }

    private async UniTask EnsureNetworkManagerAsync()
    {
        _networkManager = PersistentCore.Instance.NetworkManager;

        if (_networkManager == null)
        {
            Debug.LogError("[DevAutoStart] NetworkManager not found in PersistentCore!");
            throw new System.Exception("NetworkManager not found");
        }

        LogDebug("NetworkManager found");
        await UniTask.Yield();
    }

    private async UniTask StartHostAsync()
    {
        LogDebug($"Starting host with room name: {defaultRoomName}");
        
        _networkManager.StartHost(defaultRoomName);
        
        await UniTask.Delay(1000);
    }

    private async UniTask WaitForWaitingRoomSceneAsync()
    {
        LogDebug("Waiting for WaitingRoom scene to load...");
        
        float elapsed = 0f;
        while (SceneManager.GetActiveScene().name != "WaitingRoom")
        {
            if (elapsed > maxWaitTimeSeconds)
            {
                Debug.LogError("[DevAutoStart] Timeout waiting for WaitingRoom scene");
                throw new System.TimeoutException("WaitingRoom scene not loaded");
            }

            await UniTask.Delay(100);
            elapsed += 0.1f;
        }

        LogDebug("WaitingRoom scene loaded");
        await UniTask.Delay(500);
    }

    private async UniTask WaitForGameSessionAsync()
    {
        LogDebug("Waiting for GameSession to spawn...");
        
        float elapsed = 0f;
        while (GameSession.Instance == null)
        {
            if (elapsed > maxWaitTimeSeconds)
            {
                Debug.LogError("[DevAutoStart] Timeout waiting for GameSession");
                throw new System.TimeoutException("GameSession not spawned");
            }

            await UniTask.Delay(100);
            elapsed += 0.1f;
        }

        LogDebug("GameSession spawned");
        await UniTask.Delay(500);
    }

    private async UniTask AutoReadyAndStartAsync()
    {
        var session = GameSession.Instance;
        var runner = _networkManager.Runner;

        if (session == null || runner == null)
        {
            Debug.LogError("[DevAutoStart] Session or Runner is null");
            return;
        }

        LogDebug("Auto-registering player...");
        session.RegisterPlayerRpc(runner.LocalPlayer);
        await UniTask.Delay(300);

        LogDebug("Setting player ready...");
        session.SetReadyRpc(runner.LocalPlayer, true);
        await UniTask.Delay(300);

        LogDebug("Starting game...");
        session.StartGameRpc();
        
        await UniTask.Delay(500);
        
        LogDebug("Game start initiated - will automatically load InGame scene");
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DevAutoStart] {message}");
        }
    }

    private void OnDestroy()
    {
        _isAutoStarting = false;
    }
}
#endif
