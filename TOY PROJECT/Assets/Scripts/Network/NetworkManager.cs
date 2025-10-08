using UnityEngine;
using Fusion;

public class NetworkManager : MonoBehaviour, IManager
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
}