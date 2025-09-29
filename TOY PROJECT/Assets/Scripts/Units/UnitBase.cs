using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class UnitBase : MonoBehaviour
{
    public UnitData unitData;
    public FactionData factionData;
    public int hp;
    public int ap; // 행동력
    public Vector2Int position;
    private Action OnSelected;
    private Action OnDeselected;

    protected virtual void Awake()
    {
        if (unitData != null)
        {
            hp = unitData.maxHp;
            ap = unitData.maxAp;
        }
    }

    public abstract void Move(Vector2Int targetPos);
    public abstract void UseSkill(int skillIndex, Vector2Int targetPos);

    public virtual void TakeDamage(int amount)
    {
        hp -= amount;
        Debug.Log($"{name} took {amount} damage, remaining HP: {hp}");

        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"{name} has died.");
        Core.Instance.GridManager.UnregisterUnit(position);
        Destroy(gameObject);
    }

    public virtual void OnEnable()
    {
        OnSelected += () => { DebugPrinter.DebugColor(DebugType.Unit, $"{factionData.factionName} is Selected"); };
        OnDeselected += () => { DebugPrinter.DebugColor(DebugType.Unit, $"{factionData.factionName} is DeSelected"); };
    }

    public virtual void OnDisable()
    {
        OnSelected = null;
        OnDeselected = null;
    }

    public virtual void Select()
    {
        var outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = true;
        OnSelected?.Invoke();
    }

    public virtual void Deselect()
    {
        var outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;
        OnDeselected?.Invoke();
    }
} 