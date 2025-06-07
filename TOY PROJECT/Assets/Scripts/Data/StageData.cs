using UnityEngine;

[CreateAssetMenu(menuName = "Data/StageData")]
public class StageData : ScriptableObject
{
    public int width = 7;
    public int height = 7;
    public Vector2Int playerSpawn;
    public EnemySpawnData[] enemySpawns;
}

[System.Serializable]
public class EnemySpawnData
{
    public string enemyType; // 적 타입(이름)
    public Vector2Int spawnPos;
} 