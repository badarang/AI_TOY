using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    public string skillName;
    public int apCost;
    public int cooldown;
    public int range;

    public abstract void Activate(UnitBase caster, Vector2Int targetPos);
} 