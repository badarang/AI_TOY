using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UnitInfoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI apText;
    [SerializeField] private Image unitIcon;
    [SerializeField] private Transform skillsContainer;
    [SerializeField] private GameObject skillInfoPrefab;
    
    private UnitBase currentUnit;
    private List<GameObject> skillInfoItems = new List<GameObject>();

    private void Update()
    {
        if (panel.activeSelf && currentUnit != null)
        {
            UpdateDisplay();
        }
    }

    private void OnDisable()
    {
        if (currentUnit != null)
        {
            // currentUnit.OnStateChanged -= UpdateDisplay;
        }
    }

    public void Show(UnitBase unit)
    {
        if (unit == null) 
        {
            Hide();
            return;
        }
        
        currentUnit = unit;
        panel.SetActive(true);
        UpdateDisplay(); // 즉시 호출하여 UI 업데이트
    }

    public void Hide()
    {
        if (currentUnit != null)
        {
            // currentUnit.OnStateChanged -= UpdateDisplay;
        }

        panel.SetActive(false);
        currentUnit = null;
    }

    public void UpdateDisplay()
    {
        if (currentUnit == null)
        {
            Debug.LogWarning("[UnitInfoUI] UpdateDisplay called but currentUnit is null");
            return;
        }

        Debug.Log($"[UnitInfoUI] Updating display for: {currentUnit.unitData.unitMeta.nameKey}");
        Debug.Log($"[UnitInfoUI] UI Components - unitNameText: {unitNameText != null}, hpText: {hpText != null}, apText: {apText != null}");

        if (unitNameText != null)
        {
            unitNameText.text = currentUnit.unitData.unitMeta.nameKey;
            Debug.Log($"[UnitInfoUI] Set name to: {unitNameText.text}");
        }
        else
        {
            Debug.LogError("[UnitInfoUI] unitNameText is NULL! Check Inspector assignment!");
        }

        if (hpText != null)
        {
            hpText.text = $"HP: {currentUnit.hp}/{currentUnit.unitData.maxHp}";
            Debug.Log($"[UnitInfoUI] Set HP to: {hpText.text}");
        }
        else
        {
            Debug.LogError("[UnitInfoUI] hpText is NULL! Check Inspector assignment!");
        }

        if (apText != null)
        {
            apText.text = $"AP: {currentUnit.ap}/{currentUnit.unitData.maxAp}";
            Debug.Log($"[UnitInfoUI] Set AP to: {apText.text}");
        }
        else
        {
            Debug.LogError("[UnitInfoUI] apText is NULL! Check Inspector assignment!");
        }

        if (unitIcon != null && currentUnit.unitData.unitMeta.icon != null)
        {
            unitIcon.sprite = currentUnit.unitData.unitMeta.icon;
            Debug.Log("[UnitInfoUI] Set unit icon");
        }

        UpdateSkillList();
    }

    private void UpdateSkillList()
    {
        // Clear existing skill items before creating new ones
        foreach (var item in skillInfoItems)
        {
            if (item != null)
            {
                Core.Instance.PoolManager.ReturnToPool(item);
            }
        }
        skillInfoItems.Clear();

        if (currentUnit.unitData.skills == null) return;

        for (int i = 0; i < currentUnit.unitData.skills.Length; i++)
        {
            var skill = currentUnit.unitData.skills[i];
            GameObject skillItem = Core.Instance.PoolManager.SpawnFromPool("SkillInfo", skillsContainer, false);
            
            var nameText = skillItem.transform.Find("SkillName")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                string cooldownInfo = currentUnit.GetSkillCooldown(i) > 0 
                    ? $" (CD: {currentUnit.GetSkillCooldown(i)})" 
                    : "";
                nameText.text = $"{skill.skillMeta.nameKey}{cooldownInfo}";
            }

            var costText = skillItem.transform.Find("SkillCost")?.GetComponent<TextMeshProUGUI>();
            if (costText != null)
            {
                costText.text = $"AP: {skill.apCost}";
            }

            skillInfoItems.Add(skillItem);
        }
    }
}
