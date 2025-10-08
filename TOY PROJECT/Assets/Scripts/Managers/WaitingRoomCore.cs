using UnityEngine;

public class WaitingRoomCore : MonoBehaviour
{
    public static WaitingRoomCore Instance { get; private set; }

    [Header("Waiting Room Managers")]
    [SerializeField] private WaitingRoomUIManager waitingRoomUIManager;

    public WaitingRoomUIManager WaitingRoomUIManager => waitingRoomUIManager;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("[WaitingRoomCore] Duplicate instance found, destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[WaitingRoomCore] Waiting room managers initialized.");

        InitializeManagers();
    }

    private void InitializeManagers()
    {
        Debug.Log("[WaitingRoomCore] Initializing waiting room managers...");

        if (waitingRoomUIManager != null)
        {
            if (waitingRoomUIManager is IManager iManager)
            {
                iManager.BeforeInit();
                iManager.AfterInit();
            }
        }

        Debug.Log("[WaitingRoomCore] Waiting room managers initialized.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[WaitingRoomCore] Waiting room managers cleaned up.");
        }
    }
}
