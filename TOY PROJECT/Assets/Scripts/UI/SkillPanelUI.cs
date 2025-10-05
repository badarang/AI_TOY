using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SkillPanelUI : MonoBehaviour
{
    public GameObject skillButtonPrefab; // Assign a prefab with a Text component
    public Transform skillButtonContainer; // Assign a layout group container

    private List<GameObject> currentSkillButtons = new List<GameObject>();

public void DisplaySkills(SkillData[] skills)
    {
        ClearSkills();

        if (skills == null) return;

        foreach (var skillData in skills)
        {
            if (skillButtonPrefab == null) continue;

            GameObject skillButton = Core.Instance.PoolManager.SpawnFromPool("SkillButton", skillButtonContainer, false);
            TextMeshProUGUI skillNameText = skillButton.GetComponentInChildren<TextMeshProUGUI>();
            if (skillNameText != null && skillData.skillMeta != null)
            {
                string displayName = $"[{skillData.skillMeta.nameKey}]";
                skillNameText.text = displayName;
            }
            currentSkillButtons.Add(skillButton);
        }
    }

public void ClearSkills()
    {
        foreach(var button in currentSkillButtons)
        {
            if (button != null)
            {
                Core.Instance.PoolManager.ReturnToPool(button);
            }
        }
        currentSkillButtons.Clear();
    }


public void UpdateSkillDisplay(UnitBase unit)
    {
        if (unit == null || unit.unitData == null)
        {
            ClearSkills();
            return;
        }
        
        DisplaySkills(unit.unitData.skills);
    }
}