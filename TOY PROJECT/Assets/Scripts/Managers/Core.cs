using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;
using UnityEngine.Serialization;

public class Core : MonoBehaviour
{
    public static Core Instance { get; private set; }

    [Header("Asset Database")]
    [SerializeField]
    private GameAssetDatabase gameDatabase;
    public GameAssetDatabase GameDatabase => gameDatabase;

    [Header("Managers")]
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private PoolManager poolManager;

    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private StageManager stageManager;

    [SerializeField]
    private UIManager uiManager;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private RewardManager rewardManager;

    [SerializeField]
    private EnemyAIManager enemyAIManager;

    [SerializeField]
    private PreviewManager previewManager;

    [SerializeField]
    private InputManager inputManager;

    [SerializeField]
    private UnitManager unitManager;

    [SerializeField]
    private AudioManager audioManager;
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

    public UnitManager UnitManager => unitManager;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("[InGameCore] Duplicate instance found, destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[InGameCore] InGame scene managers initialized.");

        InitializeManagers();
    }

    private void InitializeManagers()
    {
        Debug.Log("[Core] Starting Manager Initialization...");

        ValidateManagers();

        Debug.Log("[Core] === Phase 1: BeforeInit ===");
        CallBeforeInit(poolManager);
        CallBeforeInit(gridManager);
        CallBeforeInit(stageManager);
        CallBeforeInit(unitManager);
        CallBeforeInit(uiManager);
        CallBeforeInit(enemyAIManager);
        CallBeforeInit(rewardManager);
        CallBeforeInit(previewManager);
        CallBeforeInit(inputManager);
        CallBeforeInit(turnManager);
        CallBeforeInit(gameManager);

        Debug.Log("[Core] === Phase 2: AfterInit ===");
        CallAfterInit(poolManager);
        CallAfterInit(gridManager);
        CallAfterInit(stageManager);
        CallAfterInit(unitManager);
        CallAfterInit(uiManager);
        CallAfterInit(enemyAIManager);
        CallAfterInit(rewardManager);
        CallAfterInit(previewManager);
        CallAfterInit(inputManager);
        CallAfterInit(turnManager);
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
        if (gameManager == null)
            Debug.LogError("[Core] GameManager is not assigned!");
        if (poolManager == null)
            Debug.LogError("[Core] PoolManager is not assigned!");
        if (gridManager == null)
            Debug.LogError("[Core] GridManager is not assigned!");
        if (stageManager == null)
            Debug.LogError("[Core] StageManager is not assigned!");
        if (uiManager == null)
            Debug.LogError("[Core] UIManager is not assigned!");
        if (turnManager == null)
            Debug.LogError("[Core] TurnManager is not assigned!");
        if (rewardManager == null)
            Debug.LogError("[Core] RewardManager is not assigned!");
        if (enemyAIManager == null)
            Debug.LogError("[Core] EnemyAIManager is not assigned!");
        if (previewManager == null)
            Debug.LogError("[Core] PreviewManager is not assigned!");
        if (inputManager == null)
            Debug.LogError("[Core] InputManager is not assigned!");
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[InGameCore] InGame managers cleaned up.");
        }
    }
}
