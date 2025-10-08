using UnityEngine;

public class PersistentCore : MonoBehaviour
{
    public static PersistentCore Instance { get; private set; }

    [Header("Persistent Managers (DontDestroyOnLoad)")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private AudioManager audioManager;

    public NetworkManager NetworkManager => networkManager;
    public AudioManager AudioManager => audioManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PersistentCore] Instance created and will persist across scenes.");
            
            InitializePersistentManagers();
        }
        else
        {
            Debug.Log("[PersistentCore] Duplicate instance destroyed.");
            Destroy(gameObject);
        }
    }

    private void InitializePersistentManagers()
    {
        Debug.Log("[PersistentCore] Initializing persistent managers...");

        if (networkManager != null)
        {
            if (networkManager is IManager iManager)
            {
                iManager.BeforeInit();
                iManager.AfterInit();
            }
        }
        else
        {
            Debug.LogWarning("[PersistentCore] NetworkManager is not assigned!");
        }

        if (audioManager != null)
        {
            if (audioManager is IManager iManager)
            {
                iManager.BeforeInit();
                iManager.AfterInit();
            }
        }

        Debug.Log("[PersistentCore] Persistent managers initialized.");
    }

    private void OnApplicationQuit()
    {
        Instance = null;
    }
}
