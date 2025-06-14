using UnityEngine;

public class TurnOrderUI : MonoBehaviour
{
    public TurnManager turnManager;
    public void UpdateOrder()
    {
        if (turnManager == null) return;
        Debug.Log($"현재 턴: {turnManager.CurrentTurn}");
        // 실제로는 UI 텍스트 등으로 표시
    }
} 