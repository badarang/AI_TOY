using Sirenix.OdinInspector;
using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    public SkillMeta skillMeta;

    [Button("Activate Skill")]
    public abstract void Activate(UnitBase caster, Vector2Int targetPos);
} 