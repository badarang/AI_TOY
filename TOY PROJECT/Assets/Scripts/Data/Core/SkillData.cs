using UnityEngine;

[CreateAssetMenu(menuName = "Data/SkillData")]
public class SkillData : ScriptableObject
{
    public UnitMeta unitMeta;
    public int apCost;
    public int cooldown;
    public int range;
} 