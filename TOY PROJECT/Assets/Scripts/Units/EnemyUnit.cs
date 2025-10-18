using Fusion;
using UnityEngine;

public class EnemyUnit : UnitBase
{
    // 이 메서드는 호스트(서버)가 유닛을 스폰할 때 호출합니다.
    public void Initialize(Vector2Int spawnPos)
    {
        Position = spawnPos;
        Owner = PlayerRef.None;
    }
}
