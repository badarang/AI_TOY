using Sirenix.OdinInspector;
using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    public string skillName;
    public int apCost;
    public int cooldown;
    public int range;

    [Button("Activate Skill")]
    public abstract void Activate(UnitBase caster, Vector2Int targetPos);
} 