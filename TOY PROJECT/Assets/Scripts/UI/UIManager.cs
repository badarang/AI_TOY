using UnityEngine;
using System.Collections.Generic;
using TMPro; // TextMeshPro 사용을 위해 추가

public class UIManager : MonoBehaviour
{
    [Header("Component References")]
    public TurnOrderUI turnOrderUI;
    public SkillPanelUI skillPanelUI;

    [Header("In-Game UI")]
    [Tooltip("\'증원까지: 5턴\'과 같이 표시될 텍스트입니다.")]
    [SerializeField] private TextMeshProUGUI turnCounterText;

    [Header("Reward Screen")]
    [Tooltip("보상 선택 화면의 부모 패널입니다.")]
    [SerializeField] private GameObject rewardScreenPanel;
    [Tooltip("보상 카드의 프리팹입니다. 카드에는 RewardCardUI 스크립트가 있어야 합니다.")]
    [SerializeField] private GameObject rewardCardPrefab;
    [Tooltip("보상 카드들이 생성될 부모 객체(Layout Group)입니다.")]
    [SerializeField] private Transform rewardCardsContainer;

    void Start()
    {
        // 게임 시작 시 보상 화면은 숨겨둡니다.
        if (rewardScreenPanel != null)
        {
            rewardScreenPanel.SetActive(false);
        }
    }

    public void ShowBattleLog(string message) { }

    public void UpdateSkillPanel()
    {
        if (skillPanelUI == null || Core.Instance.GridManager == null) return;
        UnitBase selectedUnit = Core.Instance.GridManager.GetSelectedUnit();
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

    /// <summary>
    /// 화면의 턴 카운터 UI를 업데이트합니다. TurnManager가 호출합니다.
    /// </summary>
    public void UpdateTurnUI(int currentTurnInWave, int turnLimit)
    {
        if (turnCounterText != null)
        {
            // 턴이 1부터 시작하고, 제한이 5일 때, 1턴에는 '5턴 남음', 5턴에는 '1턴 남음'으로 표시
            int turnsLeft = turnLimit - currentTurnInWave + 1;
            turnCounterText.text = $"증원까지: {turnsLeft}턴";
        }
    }

    /// <summary>
    /// 보상 선택 화면을 표시합니다. TurnManager가 호출합니다.
    /// </summary>
    public void ShowRewardScreen(List<ScriptableObject> rewards)
    {
        if (rewardScreenPanel == null || rewardCardPrefab == null || rewardCardsContainer == null)
        {
            Debug.LogError("Reward Screen UI components are not assigned in the UIManager.");
            return;
        }

        // 기존에 생성된 카드가 있다면 모두 삭제합니다.
        foreach (Transform child in rewardCardsContainer)
        {
            Destroy(child.gameObject);
        }

        rewardScreenPanel.SetActive(true);

        foreach (var rewardData in rewards)
        {
            GameObject cardInstance = Instantiate(rewardCardPrefab, rewardCardsContainer);
            
            // TODO: RewardCardUI 스크립트를 가져와서 데이터를 설정해야 합니다.
            // 예시: cardInstance.GetComponent<RewardCardUI>().Setup(rewardData);
            // 이 카드 UI의 버튼은 TurnManager.FinalizeRewardSelection()을 호출하도록 설정해야 합니다.
        }
        
        Debug.Log($"{rewards.Count}개의 보상을 화면에 표시합니다.");
    }

    /// <summary>
    /// 보상 선택 화면을 숨깁니다. TurnManager가 호출합니다.
    /// </summary>
    public void HideRewardScreen()
    {
        if (rewardScreenPanel != null)
        {
            rewardScreenPanel.SetActive(false);
        }
    }
}