using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TurnOrderUI turnOrderUI;
    public void ShowBattleLog(string message) { }
    public void UpdateSkillPanel() { }
    public void UpdateTurnOrder() { turnOrderUI?.UpdateOrder(); }
} 