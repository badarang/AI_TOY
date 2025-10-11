using Fusion;
using UnityEngine;

public class EnemyUnit : UnitBase
{
    // 이 메서드는 호스트(서버)가 유닛을 스폰할 때 호출합니다.
    public void Initialize(Vector2Int spawnPos)
    {
        // Position은 UnitBase에 있는 [Networked] 프로퍼티입니다.
        Position = spawnPos;
        
        // 적 유닛은 주인이 없으므로 PlayerRef.None으로 설정합니다.
        Owner = PlayerRef.None; 
    }
}
