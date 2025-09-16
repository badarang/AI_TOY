using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TurnOrderUI turnOrderUI;
    public SkillPanelUI skillPanelUI; // Assign in inspector

    public void ShowBattleLog(string message) { }

    public void UpdateSkillPanel() 
    {
        if (skillPanelUI == null || GridManager.Instance == null) return;

        UnitBase selectedUnit = GridManager.Instance.GetSelectedUnit();

        if (selectedUnit != null && selectedUnit.unitData != null)
        {
            skillPanelUI.DisplaySkills(selectedUnit.unitData.skills);
        }
        else
        {
            skillPanelUI.ClearSkills();
        }
    }

    public void UpdateTurnOrder() { turnOrderUI?.UpdateOrder(); }
}