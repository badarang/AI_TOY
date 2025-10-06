using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 게임의 전체적인 흐름과 상태를 관리하는 최상위 매니저입니다.
/// ChapterData를 기반으로 게임의 진행을 총괄합니다.
/// </summary>
public class GameManager : MonoBehaviour, IManager
{

    public void BeforeInit()
    {
    }

    public void AfterInit()
    {
        stageManager = Core.Instance.StageManager;
        turnManager = Core.Instance.TurnManager;

        StartNewGame(startingChapter);
    }

    [Header("챕터 및 스테이지 데이터")]
    [SerializeField] private ChapterData startingChapter;

    [Header("스테이지 풀 (종류별로 할당)")]
    [SerializeField] private List<StageData> battleStages;
    [SerializeField] private List<StageData> eliteBattleStages;
    [SerializeField] private List<StageData> eventStages;
    [SerializeField] private List<StageData> shopStages;
    [SerializeField] private List<StageData> restStages;
    [SerializeField] private StageData bossStage; // 보스 스테이지는 보통 하나만 할당

    // --- 매니저 참조 ---
    private StageManager stageManager;
    private TurnManager turnManager;

    // --- 게임 상태 ---
    private int currentLayerIndex = -1;
    private bool isPlayerSpawned = false;

    public void StartNewGame(ChapterData chapter)
    {
        Debug.Log($"새로운 챕터를 시작합니다: {chapter.chapterName}");
        currentLayerIndex = -1; // 다음 함수에서 0으로 증가하며 시작
        isPlayerSpawned = false;
        
        // TODO: 기존 게임 데이터 초기화 (인벤토리, 플레이어 스탯 등)

        ProceedToNextLayer();
    }

    /// <summary>
    /// 스테이지 클리어 또는 포탈 선택 후 다음 층으로 진행합니다.
    /// </summary>
    public void ProceedToNextLayer()
    {
        currentLayerIndex++;

        if (currentLayerIndex >= startingChapter.layers.Count)
        {
            Debug.Log("챕터 클리어! 축하합니다!");
            // TODO: 챕터 클리어 로직 (결과 화면, 메인 메뉴로 돌아가기 등)
            return;
        }

        var currentLayer = startingChapter.layers[currentLayerIndex];
        
        if (currentLayer.possibleStageTypes.Count == 1)
        {
            // 선택지가 하나뿐이면 바로 해당 타입의 스테이지 시작
            StageType type = currentLayer.possibleStageTypes[0];
            StartStage(type);
        }
        else
        {
            // 선택지가 여러 개이면, 플레이어가 선택할 수 있도록 포탈 생성 요청
            Debug.Log($"다음 선택지: {string.Join(", ", currentLayer.possibleStageTypes)}");
            stageManager.CreatePortals(currentLayer.possibleStageTypes);
            // TODO: 게임 상태를 '자유 이동'으로 변경
        }
    }

    /// <summary>
    /// 플레이어가 포탈에 진입했을 때 호출됩니다. (StageManager가 호출해 줄 예정)
    /// </summary>
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

        Debug.Log($"{type} 타입의 스테이지를 로드합니다: {stageToLoad.name}");
        stageManager.LoadStage(stageToLoad);

        if (!isPlayerSpawned)
        {
            stageManager.SpawnPlayer(stageToLoad.playerSpawn);
            isPlayerSpawned = true;
        }

        // 스테이지 타입에 따라 다른 로직 수행
        if (stageToLoad.stageType == StageType.Battle || stageToLoad.stageType == StageType.EliteBattle || stageToLoad.stageType == StageType.Boss)
        {
            turnManager.StartFirstWave();
        }
        else
        {
            Debug.Log($"{stageToLoad.stageType} 타입의 스테이지에 진입했습니다. (전투 아님)");
            // TODO: 이벤트, 상점 등 비전투 스테이지의 UI 및 로직 실행
            // 예: UIManager.ShowEvent(stageToLoad.eventData);
        }
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
