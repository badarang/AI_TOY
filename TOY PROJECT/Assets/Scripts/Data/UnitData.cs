using UnityEngine;

[CreateAssetMenu(menuName = "Data/UnitData")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public int maxHp;
    public int maxAp;
    public SkillData[] skills;
} 