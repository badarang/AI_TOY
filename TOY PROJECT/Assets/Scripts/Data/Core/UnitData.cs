using UnityEngine;
using System.Collections.Generic;

public enum UnitType
{
    None,
    Player_Hikai,
    Player_Vrixa,
    Enemy_Goose,
}

[CreateAssetMenu(menuName = "Data/UnitData")]
public class UnitData : ScriptableObject
{
    public UnitType unitType;
    public UnitMeta unitMeta;
    public int maxHp;
    public int maxAp;
    public SkillData[] skills;
    public List<Vector2Int> movementPattern;
} 