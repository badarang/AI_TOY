using UnityEngine;

public abstract class UnitBase : MonoBehaviour
{
    public int hp;
    public int ap; // 행동력
    public Vector2Int position;

    public abstract void Move(Vector2Int targetPos);
    public abstract void UseSkill(int skillIndex, Vector2Int targetPos);
} 