using UnityEngine;

public class Obstacle : UnitBase
{
        
    protected override void Awake()
    {
        if (data != null && data.unitData != null)
        {
            unitData = data.unitData;
        }
        base.Awake();
    }
public ObstacleData data;
    public int hp;
    public bool isCollapsed = false;



    // 공격받을 때 호출
public override void TakeDamage(int amount)
    {
        if (isCollapsed) return;
        base.TakeDamage(amount);
    }

    // 무너질 때 호출, attackDir의 반대 방향으로 쓰러짐
protected override void Die()
    {
        isCollapsed = true;
        Debug.Log($"장애물이 무너집니다!");
        base.Die();
    }
} 