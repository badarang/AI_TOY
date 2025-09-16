using UnityEngine;

public class Core : MonoBehaviour
{
    public static Core Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GridManager gridManager;


    // 프로퍼티로 접근
    public InputManager InputManager => inputManager;
    public TurnManager TurnManager => turnManager;
    public UIManager UIManager => uiManager;
    public GridManager GridManager => gridManager;

    void Awake()
    {
        // 싱글톤 패턴 - 씬 전환시에도 유지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupManagers();
            Debug.Log("Core: Instance created and managers setup complete");
        }
        else
        {
            Debug.Log("Core: Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    private void SetupManagers()
    {
        // Inspector에서 할당하지 않았다면 자동으로 찾거나 생성
        if (inputManager == null) inputManager = FindObjectOfType<InputManager>();
        if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
    }

    // 게임 종료시 정리
    void OnApplicationQuit()
    {
        Instance = null;
    }

    // 에디터에서 플레이 모드 종료시 정리
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && Application.isEditor)
        {
            Instance = null;
        }
    }
}