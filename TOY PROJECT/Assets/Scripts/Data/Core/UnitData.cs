using UnityEngine;

public enum UnitType
{
    None,
    Player_Zed,
    Player_Lux,
    Enemy_Goose,
}

[CreateAssetMenu(menuName = "Data/UnitData")]
public class UnitData : ScriptableObject
{
    public UnitType unitType;
    public string unitName;
    public int maxHp;
    public int maxAp;
    public SkillData[] skills;
} 