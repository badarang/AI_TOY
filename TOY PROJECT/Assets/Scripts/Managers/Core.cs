using UnityEngine;

public class Core : MonoBehaviour
{
    public static Core Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private InputManager inputManager;


    // 프로퍼티로 접근
    public InputManager InputManager => inputManager;

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
        SetupInputManager();
        // 다른 매니저들도 여기서 설정
    }

    private void SetupInputManager()
    {
        // Inspector에서 할당하지 않았다면 자동으로 찾거나 생성
        if (inputManager == null)
        {
            // 1. 자식 오브젝트에서 InputManager 찾기
            inputManager = GetComponentInChildren<InputManager>();

            // 2. 없다면 새로 생성
            if (inputManager == null)
            {
                GameObject inputManagerObj = new GameObject("InputManager");
                inputManagerObj.transform.SetParent(transform);
                inputManager = inputManagerObj.AddComponent<InputManager>();
                Debug.Log("Core: InputManager created automatically");
            }
            else
            {
                Debug.Log("Core: InputManager found in children");
            }
        }
        else
        {
            Debug.Log("Core: InputManager assigned in Inspector");
        }
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