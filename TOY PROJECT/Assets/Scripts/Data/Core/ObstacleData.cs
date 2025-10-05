using UnityEngine;

[CreateAssetMenu(menuName = "Data/ObstacleData")]
public class ObstacleData : ScriptableObject
{
    public UnitMeta unitMeta;
    public int maxHp = 5;
        public UnitData unitData;
public GameObject prefab;
    // 추가 특성(예: 무너짐 효과 등) 필요시 여기에
} 