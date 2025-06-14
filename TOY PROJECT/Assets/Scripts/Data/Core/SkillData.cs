using UnityEngine;

[CreateAssetMenu(menuName = "Data/SkillData")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public string description;
    public int apCost;
    public int cooldown;
    public int range;
} 