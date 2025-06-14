using UnityEngine;

[CreateAssetMenu(menuName = "Data/ObstacleData")]
public class ObstacleData : ScriptableObject
{
    public string obstacleName;
    public int maxHp = 5;
    public GameObject prefab;
    // 추가 특성(예: 무너짐 효과 등) 필요시 여기에
} 