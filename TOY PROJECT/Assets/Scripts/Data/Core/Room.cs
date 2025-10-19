using UnityEngine;

[CreateAssetMenu(menuName = "Data/Room")]
public class Room : ScriptableObject
{
    [Header("Room Type")]
    public RoomType type = RoomType.Battle;
    
    [Header("Grid Settings")]
    public int width = 7;
    public int height = 7;
    
    [Header("Enemy Waves")]
    public EnemyWave[] waves;
    
    [Header("Obstacles")]
    public ObstacleSpawnData[] obstacleSpawns;
    
    [Header("Difficulty")]
    public int difficulty;
}

[System.Serializable]
public class EnemyWave
{
    public string waveName;
    public int spawnTurn;
    
    [Tooltip("이 웨이브에 즉시 스폰될 적들")]
    public EnemySpawnData[] enemySpawns;
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
