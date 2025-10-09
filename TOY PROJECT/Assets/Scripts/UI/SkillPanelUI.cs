using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SkillPanelUI : MonoBehaviour
{
    public GameObject skillButtonPrefab; // Assign a prefab with a Text component
    public Transform skillButtonContainer; // Assign a layout group container

    private List<GameObject> currentSkillButtons = new List<GameObject>();

public void DisplaySkills(List<Skill> skills)
    {
        ClearSkills();

        if (skills == null) return;

        for (int i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            int skillIndex = i;
            
            if (skillButtonPrefab == null) continue;

            GameObject skillButton = Core.Instance.PoolManager.SpawnFromPool("SkillButton", skillButtonContainer, false);
            if (skillButton == null)
            {
                skillButton = Instantiate(skillButtonPrefab, skillButtonContainer, false);
            }
            
            TextMeshProUGUI skillNameText = skillButton.GetComponentInChildren<TextMeshProUGUI>();
            if (skillNameText != null && skill.data.skillMeta != null)
            {
                string cooldownText = skill.currentCooldown > 0 ? $" (CD: {skill.currentCooldown})" : "";
                string displayName = $"[{skill.data.skillMeta.nameKey}]{cooldownText}";
                skillNameText.text = displayName;
            }
            
            Button btn = skillButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSkillButtonClicked(skillIndex));
                
                // 쿨다운 중이거나 AP가 부족하면 버튼 비활성화
                bool canUse = skill.currentCooldown == 0;
                btn.interactable = canUse;
            }
            
            currentSkillButtons.Add(skillButton);
        }
    }

private void OnSkillButtonClicked(int skillIndex)
    {
        Debug.Log($"스킬 버튼 클릭: {skillIndex}");
        
        // 이제 스킬은 PlayerUnit에서 직접 사용합니다
        // TurnManager에서 SelectedUnit을 통해 처리
        var turnManager = Core.Instance?.TurnManager;
        if (turnManager != null && turnManager.SelectedUnit != null)
        {
            // UI에서는 스킬 정보만 표시, 실제 사용은 HandleCellClick에서 처리
            Debug.Log($"Skill {skillIndex} button clicked for unit {turnManager.SelectedUnit.name}");
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
        
        DisplaySkills(unit.GetSkills());
    }
}