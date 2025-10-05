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

        // 1. 보상 적용
        if (currentRewardData is SkillData skillData)
        {
            // TODO: 플레이어 유닛에게 이 스킬을 추가하는 로직 필요
            // 예: Core.Instance.PlayerManager.AddSkill(skillData);
        }
        else if (currentRewardData is UpgradeData upgradeData)
        {
            // TODO: 플레이어 유닛에게 이 업그레이드를 적용하는 로직 필요
            // 예: upgradeData.behavior.Apply(Core.Instance.PlayerManager.GetPlayer());
        }

        // 2. 보상 단계 종료 및 다음 웨이브 시작 요청
        // UIManager를 통해 다른 카드들이 선택되지 않도록 하고, 화면을 닫은 후 TurnManager에 알립니다.
        Core.Instance.UIManager.HideRewardScreen();
        Core.Instance.TurnManager.FinalizeRewardSelection();
    }
}
