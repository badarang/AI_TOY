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

            GameObject skillButton = Instantiate(skillButtonPrefab, skillButtonContainer);
            TextMeshProUGUI skillNameText = skillButton.GetComponentInChildren<TextMeshProUGUI>();
            Debug.Log($"SkillMeta - {skillData.skillMeta}, skillNameText = {skillNameText}");
            if (skillNameText != null && skillData.skillMeta != null)
            {
                // Here you would use the localization package to get the display name
                // For now, we'll just display the key itself.
                string displayName = $"[{skillData.skillMeta.nameKey}]";
                skillNameText.text = displayName;

                // TODO: Add button listener to use the skill
            }
            currentSkillButtons.Add(skillButton);
        }
    }

    public void ClearSkills()
    {
        foreach(var button in currentSkillButtons)
        {
            Destroy(button);
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