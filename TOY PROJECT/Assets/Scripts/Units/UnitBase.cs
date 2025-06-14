using UnityEngine;
using UnityEngine.UI;

public abstract class UnitBase : MonoBehaviour
{
    public int hp;
    public int ap; // 행동력
    public Vector2Int position;

    public abstract void Move(Vector2Int targetPos);
    public abstract void UseSkill(int skillIndex, Vector2Int targetPos);

    public virtual void Select()
    {
        var outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = true;
    }

    public virtual void Deselect()
    {
        var outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;
    }
} 