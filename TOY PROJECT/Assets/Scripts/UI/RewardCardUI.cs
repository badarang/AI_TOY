using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 개별 보상 카드의 UI와 상호작용을 담당하는 스크립트입니다.
public class RewardCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI tierText; // 등급 표시용 텍스트
    [SerializeField] private Button selectButton;

    private ScriptableObject currentRewardData;

    private void Start()
    {
        selectButton.onClick.AddListener(OnCardSelected);
    }

    /// <summary>
    /// UIManager가 보상 데이터를 받아 이 카드를 설정할 때 호출합니다.
    /// </summary>
    public void Setup(ScriptableObject rewardData)
    {
        currentRewardData = rewardData;

        if (rewardData is SkillData skillData)
        {
            nameText.text = skillData.skillMeta.nameKey;
            descriptionText.text = skillData.skillMeta.descKey;
            // iconImage.sprite = skillData.skillMeta.icon; // SkillMeta에 icon 필드 필요
            tierText.text = skillData.tier.ToString();
        }
        else if (rewardData is UpgradeData upgradeData)
        {
            nameText.text = upgradeData.upgradeName;
            descriptionText.text = upgradeData.description;
            iconImage.sprite = upgradeData.icon;
            tierText.text = upgradeData.tier.ToString();
        }
        // TODO: 회복 아이템 등 다른 종류의 보상에 대한 처리 추가
    }

    /// <summary>
    /// 이 카드의 '선택' 버튼이 클릭되었을 때 호출됩니다.
    /// </summary>
private void OnCardSelected()
    {
        Debug.Log($"보상 선택: {nameText.text}");

        if (currentRewardData is SkillData skillData)
        {
            Debug.Log($"스킬 획득: {skillData.skillMeta.nameKey}");
        }
        else if (currentRewardData is UpgradeData upgradeData)
        {
            Debug.Log($"업그레이드 획듍: {upgradeData.upgradeName}");
        }

        Core.Instance.UIManager.HideRewardScreen();
        Core.Instance.TurnManager.FinalizeRewardSelection();
    }
}
