using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public StageManager stageManager;
    public TurnManager turnManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 게임 시작, 첫 스테이지 로딩
        stageManager.LoadStage(0);
    }

    public void OnStageClear()
    {
        // 스테이지 클리어 처리
    }

    public void OnGameOver()
    {
        // 게임 오버 처리
    }
} 