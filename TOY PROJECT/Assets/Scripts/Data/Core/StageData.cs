using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/StageData")]
public class StageData : ScriptableObject
{
    [ShowInInspector, HideLabel]
    [ShowIf("@UnityEngine.Application.isPlaying == false")]
    private StageMapEditor mapEditor => new StageMapEditor(this);
    
    public int width = 7;
    public int height = 7;
    public Vector2Int playerSpawn;
    public UnitType playerType;
    
    [Title("웨이브 설정")]
    public EnemyWave[] waves = new EnemyWave[0]; // 여러 웨이브의 적 정보를 담도록 변경
    
    [Title("장애물 설정")]
    public ObstacleSpawnData[] obstacleSpawns = new ObstacleSpawnData[0];
}

// 한 웨이브에 등장할 적들의 목록을 정의하는 클래스
[System.Serializable]
public class EnemyWave
{
    public string waveName; // 에디터에서 웨이브를 구분하기 위한 이름 (예: "1라운드: 거위 부대")
    public EnemySpawnData[] enemySpawns;
}


// 맵 에디터 클래스 (기존 코드 유지)
[System.Serializable]
public class StageMapEditor
{
    private StageData stageData;
    
    public StageMapEditor(StageData data)
    {
        stageData = data;
    }
    
    [Button("Edit Stage Map")]
    public void OpenMapEditor()
    {
        // 맵 에디터 창 열기 또는 다른 에디터 로직
    }
}

[System.Serializable]
public class EnemySpawnData
{
    public UnitType enemyType;
    public Vector2Int spawnPos;
}

[System.Serializable]
public class ObstacleSpawnData
{
    public ObstacleData obstacleData;
    public Vector2Int spawnPos;
}