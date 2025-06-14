using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public ObstacleData data;
    public int hp;
    public bool isCollapsed = false;

    private void Start()
    {
        if (data != null)
            hp = data.maxHp;
    }

    // 공격받을 때 호출
    public void TakeDamage(int amount, Vector2Int attackDir)
    {
        if (isCollapsed) return;
        hp -= amount;
        if (hp <= 0)
        {
            Collapse(attackDir);
        }
    }

    // 무너질 때 호출, attackDir의 반대 방향으로 쓰러짐
    void Collapse(Vector2Int attackDir)
    {
        isCollapsed = true;
        Vector2Int collapseDir = -attackDir;
        // 실제로는 애니메이션/이펙트 등 추가
        Debug.Log($"장애물이 {collapseDir} 방향으로 무너집니다!");
        // TODO: 해당 방향 타일에 영향(피해, 막힘 등) 처리
        Destroy(gameObject, 1.0f); // 1초 후 오브젝트 제거(예시)
    }
} 