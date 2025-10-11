using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour, IManager
{
    [Header("챕터 및 스테이지 데이터")]
    [SerializeField]
    private ChapterData startingChapter;

    [Header("스테이지 풀 (종류별로 할당)")]
    [SerializeField]
    private List<StageData> battleStages;

    [SerializeField]
    private List<StageData> eliteBattleStages;

    [SerializeField]
    private List<StageData> eventStages;

    [SerializeField]
    private List<StageData> shopStages;

    [SerializeField]
    private List<StageData> restStages;

    [SerializeField]
    private StageData bossStage; // 보스 스테이지는 보통 하나만 할당

    // --- 매니저 참조 ---
    private StageManager stageManager;

    // --- 게임 상태 ---
    private int currentLayerIndex = -1;
    private bool isGameStarted = false;

    public void BeforeInit() { }

    public void AfterInit()
    {
        stageManager = Core.Instance.StageManager;

        // OnStageManagerReady가 호출될 때까지 대기합니다.
    }

    /// <summary>
    /// 이 메서드는 현재 아키텍처에서는 직접 호출되지 않을 수 있습니다.
    /// StageManager가 첫 스테이지를 시작하는 역할을 담당합니다.
    /// </summary>
    public void OnStageManagerReady()
    {
        if (isGameStarted) return;
        isGameStarted = true;

        // StartNewGame(startingChapter);
    }

    public void StartNewGame(ChapterData chapter)
    {
        Debug.Log($"새로운 챕터를 시작합니다: {chapter.chapterName}");
        currentLayerIndex = -1; 

        ProceedToNextLayer();
    }

    public void ProceedToNextLayer()
    {
        currentLayerIndex++;

        if (currentLayerIndex >= startingChapter.layers.Count)
        {
            Debug.Log("챕터 클리어! 축하합니다!");
            return;
        }

        var currentLayer = startingChapter.layers[currentLayerIndex];

        if (currentLayer.possibleStageTypes.Count == 1)
        {
            StageType type = currentLayer.possibleStageTypes[0];
            StartStage(type);
        }
        else
        {
            Debug.Log($"다음 선택지: {string.Join(", ", currentLayer.possibleStageTypes)}");
            // stageManager.CreatePortals(currentLayer.possibleStageTypes);
        }
    }

    public void OnPortalSelected(StageType selectedType)
    {
        Debug.Log($"플레이어가 포탈을 선택했습니다: {selectedType}");
        StartStage(selectedType);
    }

    private void StartStage(StageType type)
    {
        StageData stageToLoad = GetRandomStage(type);
        if (stageToLoad == null)
        {
            Debug.LogError($"{type} 타입의 스테이지를 찾을 수 없습니다! StageData를 할당했는지 확인해주세요.");
            return;
        }

        Debug.Log($"[GameManager] {type} 타입의 스테이지 로드를 요청합니다: {stageToLoad.name}");
        
        // GameManager의 역할은 스테이지 로드를 "요청"하는 것까지입니다.
        // 스테이지를 구성하고 전투를 시작하는 것은 StageManager와 TurnManager의 책임입니다.
        stageManager.RequestLoadStage(stageToLoad.name);
    }

    private StageData GetRandomStage(StageType type)
    {
        List<StageData> stagePool = type switch
        {
            StageType.Battle => battleStages,
            StageType.EliteBattle => eliteBattleStages,
            StageType.Event => eventStages,
            StageType.Shop => shopStages,
            StageType.RestSite => restStages,
            StageType.Boss => new List<StageData> { bossStage },
            _ => null,
        };

        if (stagePool != null && stagePool.Count > 0)
        {
            return stagePool[Random.Range(0, stagePool.Count)];
        }

        return null;
    }
}
