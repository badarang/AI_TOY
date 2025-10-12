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

    private void Start()
    {
        Hide();
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
        UpdateDisplay();
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentUnit = null;
    }

    public void UpdateDisplay()
    {
        if (currentUnit == null) return;

        unitNameText.text = currentUnit.unitData.unitMeta.nameKey;
        hpText.text = $"HP: {currentUnit.hp}/{currentUnit.unitData.maxHp}";
        apText.text = $"AP: {currentUnit.ap}/{currentUnit.unitData.maxAp}";

        if (currentUnit.unitData.unitMeta.icon != null)
        {
            unitIcon.sprite = currentUnit.unitData.unitMeta.icon;
        }

        UpdateSkillList();
    }

    private void UpdateSkillList()
    {
        foreach (var item in skillInfoItems)
        {
            Core.Instance.PoolManager.ReturnToPool(item);
        }
        skillInfoItems.Clear();

        for (int i = 0; i < currentUnit.unitData.skills.Length; i++)
        {
            var skill = currentUnit.unitData.skills[i];
            GameObject skillItem = Core.Instance.PoolManager.SpawnFromPool("SkillInfo", skillsContainer, false);
            
            var nameText = skillItem.transform.Find("SkillName")?.GetComponent<TextMeshProUGUI>();
            string cooldownInfo = currentUnit.GetSkillCooldown(i) > 0 
                ? $" (CD: {currentUnit.GetSkillCooldown(i)})" 
                : "";
            nameText.text = $"{skill.skillMeta.nameKey}{cooldownInfo}";

            var costText = skillItem.transform.Find("SkillCost")?.GetComponent<TextMeshProUGUI>();
            costText.text = $"AP: {skill.apCost}";

            skillInfoItems.Add(skillItem);
        }
    }
}
