using UnityEngine;

public class LobbyCore : MonoBehaviour
{
    public static LobbyCore Instance { get; private set; }

    [Header("Lobby Scene Managers")]
    [SerializeField] private LobbyUIManager lobbyUIManager;

    public LobbyUIManager LobbyUIManager => lobbyUIManager;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("[LobbyCore] Duplicate instance found, destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[LobbyCore] Lobby scene managers initialized.");

        InitializeLobbyManagers();
    }

    private void InitializeLobbyManagers()
    {
        Debug.Log("[LobbyCore] Initializing lobby managers...");

        if (lobbyUIManager != null)
        {
            if (lobbyUIManager is IManager iManager)
            {
                iManager.BeforeInit();
                iManager.AfterInit();
            }
        }
        else
        {
            Debug.LogWarning("[LobbyCore] LobbyUIManager is not assigned!");
        }

        Debug.Log("[LobbyCore] Lobby managers initialized.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[LobbyCore] Lobby managers cleaned up.");
        }
    }
}
