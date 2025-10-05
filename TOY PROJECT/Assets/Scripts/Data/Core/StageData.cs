using Sirenix.OdinInspector;
using UnityEngine;

// NOTE: 이 파일은 이제 하나의 '방(Room)'에 대한 모든 데이터를 정의합니다.
// 전투, 이벤트, 상점 등 모든 종류의 방을 이 데이터 하나로 표현합니다.

/// <summary>
/// 한 웨이브에 등장할 적들과 해당 웨이브의 규칙을 정의합니다.
/// </summary>
[System.Serializable]
public class EnemyWave
{
    [Tooltip("에디터에서 웨이브를 구분하기 위한 이름입니다.")]
    public string waveName;
    public EnemySpawnData[] enemySpawns;

    [Header("웨이브 규칙")]
    [Tooltip("이 웨이브를 지정된 턴 안에 클리어하면 추가 보상을 받습니다.")]
    public int clearTurnLimit = 5;
    
    [Tooltip("턴 제한 내 클리어 시 받을 추가 보상 ID입니다. (미구현)")]
    public string bonusRewardId; 
}

/// <summary>
/// 적 유닛의 종류와 생성 위치를 정의합니다.
/// </summary>
[System.Serializable]
public class EnemySpawnData
{
    public UnitType enemyType;
    public Vector2Int spawnPos;
}

/// <summary>
/// 장애물의 종류와 생성 위치를 정의합니다.
/// </summary>
[System.Serializable]
public class ObstacleSpawnData
{
    public ObstacleData obstacleData;
    public Vector2Int spawnPos;
}


[CreateAssetMenu(menuName = "Data/Stage Data (Room)")]
public class StageData : ScriptableObject
{
    [Title("기본 설정")]
    [EnumPaging]
    [Tooltip("이 스테이지(방)의 종류를 결정합니다.")]
    public StageType stageType;

    // --- BATTLE-SPECIFIC DATA ---
    [Title("전투 정보")]
    [ShowIf("IsBattleType")]
    public int width = 7;

    [ShowIf("IsBattleType")]
    public int height = 7;

    [ShowIf("IsBattleType")]
    public Vector2Int playerSpawn;

    [ShowIf("IsBattleType")]
    public UnitType playerType;

    [ShowIf("IsBattleType")]
    [ReadOnly]
    public int difficulty;

    [ShowIf("IsBattleType")]
    public EnemyWave[] waves = new EnemyWave[0];

    [ShowIf("IsBattleType")]
    public ObstacleSpawnData[] obstacleSpawns = new ObstacleSpawnData[0];

    // --- EVENT-SPECIFIC DATA ---
    [Title("이벤트 정보")]
    [ShowIf("stageType", StageType.Event)]
    public EventData eventData;

    // --- SHOP-SPECIFIC DATA (Placeholder) ---
    [Title("상점 정보")]
    [ShowIf("stageType", StageType.Shop)]
    public string shopTableId; // Placeholder for shop data

    // --- REST-SITE-SPECIFIC DATA (Placeholder) ---
    [Title("휴식처 정보")]
    [ShowIf("stageType", StageType.RestSite)]
    public int healAmount = 30; // Example property

#if UNITY_EDITOR
    private bool IsBattleType()
    {
        return this.stageType == StageType.Battle || this.stageType == StageType.EliteBattle || this.stageType == StageType.Boss;
    }
#endif
}
