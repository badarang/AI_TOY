using UnityEngine;

public static class GridUtils
{
    // 체비쇼프 거리 계산 (8방향 이동, 모든 방향 비용 동일)
    // 두 점의 x좌표 차이와 y좌표 차이 중 더 큰 값을 거리로 계산
    public static int ChebyshevDistance(Vector2Int posA, Vector2Int posB)
    {
        return Mathf.Max(Mathf.Abs(posA.x - posB.x), Mathf.Abs(posA.y - posB.y));
    }
}
