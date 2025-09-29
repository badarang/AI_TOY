using UnityEngine;

public class Core : MonoBehaviour
{
    public static Core Instance { get; private set; }

    [Header("Managers")]
    // 의존성이 적은 순서대로 정렬
    [SerializeField] private GridManager gridManager;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private EnemyAIManager enemyAIManager;
    [SerializeField] private InputManager inputManager;

    // 프로퍼티도 위와 동일한 순서로 정렬
    public GridManager GridManager => gridManager;
    public StageManager StageManager => stageManager;
    public UIManager UIManager => uiManager;
    public TurnManager TurnManager => turnManager;
    public EnemyAIManager EnemyAIManager => enemyAIManager;
    public InputManager InputManager => inputManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // SetupManagers()는 Inspector에서 모두 할당하는 것을 가정하고 제거
            Debug.Log("Core: Instance created.");
        }
        else
        {
            Debug.Log("Core: Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    void OnApplicationQuit()
    {
        Instance = null;
    }
}