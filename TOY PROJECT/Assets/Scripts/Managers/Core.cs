using UnityEngine;

public class Core : MonoBehaviour
{
    public static Core Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PoolManager poolManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private RewardManager rewardManager;
    [SerializeField] private EnemyAIManager enemyAIManager;
    [SerializeField] private PreviewManager previewManager;
    [SerializeField] private InputManager inputManager;

    public GameManager GameManager => gameManager;
    public PoolManager PoolManager => poolManager;
    public GridManager GridManager => gridManager;
    public StageManager StageManager => stageManager;
    public UIManager UIManager => uiManager;
    public TurnManager TurnManager => turnManager;
    public RewardManager RewardManager => rewardManager;
    public EnemyAIManager EnemyAIManager => enemyAIManager;
    public PreviewManager PreviewManager => previewManager;
    public InputManager InputManager => inputManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("Core: Instance created.");
            
            InitializeManagers();
        }
        else
        {
            Debug.Log("Core: Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }
    
    private void InitializeManagers()
    {
        Debug.Log("[Core] Starting Manager Initialization...");
        
        ValidateManagers();
        
        Debug.Log("[Core] === Phase 1: BeforeInit ===");
        CallBeforeInit(gameManager);
        CallBeforeInit(poolManager);
        CallBeforeInit(gridManager);
        CallBeforeInit(stageManager);
        CallBeforeInit(uiManager);
        CallBeforeInit(turnManager);
        CallBeforeInit(rewardManager);
        CallBeforeInit(enemyAIManager);
        CallBeforeInit(previewManager);
        CallBeforeInit(inputManager);
        
        Debug.Log("[Core] === Phase 2: AfterInit ===");
        CallAfterInit(poolManager);
        CallAfterInit(gridManager);
        CallAfterInit(stageManager);
        CallAfterInit(uiManager);
        CallAfterInit(turnManager);
        CallAfterInit(rewardManager);
        CallAfterInit(enemyAIManager);
        CallAfterInit(previewManager);
        CallAfterInit(inputManager);
        CallAfterInit(gameManager);
        
        Debug.Log("[Core] All managers initialized successfully.");
    }

    private void CallBeforeInit(MonoBehaviour manager)
    {
        if (manager is IManager iManager)
        {
            Debug.Log($"[Core] BeforeInit: {manager.GetType().Name}");
            iManager.BeforeInit();
        }
    }
    
    private void CallAfterInit(MonoBehaviour manager)
    {
        if (manager is IManager iManager)
        {
            Debug.Log($"[Core] AfterInit: {manager.GetType().Name}");
            iManager.AfterInit();
        }
    }

    
    private void ValidateManagers()
    {
        if (gameManager == null) Debug.LogError("[Core] GameManager is not assigned!");
        if (poolManager == null) Debug.LogError("[Core] PoolManager is not assigned!");
        if (gridManager == null) Debug.LogError("[Core] GridManager is not assigned!");
        if (stageManager == null) Debug.LogError("[Core] StageManager is not assigned!");
        if (uiManager == null) Debug.LogError("[Core] UIManager is not assigned!");
        if (turnManager == null) Debug.LogError("[Core] TurnManager is not assigned!");
        if (rewardManager == null) Debug.LogError("[Core] RewardManager is not assigned!");
        if (enemyAIManager == null) Debug.LogError("[Core] EnemyAIManager is not assigned!");
        if (previewManager == null) Debug.LogError("[Core] PreviewManager is not assigned!");
        if (inputManager == null) Debug.LogError("[Core] InputManager is not assigned!");
    }

    void OnApplicationQuit()
    {
        Instance = null;
    }
}
