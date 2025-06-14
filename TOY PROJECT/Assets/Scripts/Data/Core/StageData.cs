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
    public EnemySpawnData[] enemySpawns = new EnemySpawnData[0];
}

// 맵 에디터 클래스 추가
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