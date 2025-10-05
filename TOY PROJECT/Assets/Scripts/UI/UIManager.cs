using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Component References")]
    public TurnOrderUI turnOrderUI;
        public UnitInfoUI unitInfoUI;
public SkillPanelUI skillPanelUI;

    [Header("In-Game UI")]
    [SerializeField] private TextMeshProUGUI turnCounterText;

    [Header("Reward Screen")]
    [SerializeField] private GameObject rewardScreenPanel;
    [SerializeField] private GameObject rewardCardPrefab;
    [SerializeField] private Transform rewardCardsContainer;

    [Header("Node Selection Screen")]
    [SerializeField] private GameObject nodeSelectionScreenPanel;
    [SerializeField] private GameObject nodeButtonPrefab; // 각 노드를 나타낼 버튼 프리팹
    [SerializeField] private Transform nodeButtonsContainer; // 버튼들이 생성될 부모 객체

    void Start()
    {
        if (rewardScreenPanel != null) rewardScreenPanel.SetActive(false);
        if (nodeSelectionScreenPanel != null) nodeSelectionScreenPanel.SetActive(false);
    }

void PositionUIToBottomRight()
    {
        if (unitInfoUI != null)
        {
            RectTransform rt = unitInfoUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = new Vector2(-20, 20);
                rt.sizeDelta = new Vector2(300, 400);
            }
        }

        if (skillPanelUI != null)
        {
            RectTransform rt = skillPanelUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = new Vector2(-20, 430);
                rt.sizeDelta = new Vector2(300, 200);
            }
        }
    }


    public void UpdateTurnUI(int currentTurnInWave, int turnLimit)
    {
        if (turnCounterText != null)
        {
            int turnsLeft = turnLimit - currentTurnInWave + 1;
            turnCounterText.text = $"증원까지: {turnsLeft}턴";
        }
    }

public void ShowUnitInfo(UnitBase unit)
    {
        if (unitInfoUI != null)
        {
            unitInfoUI.Show(unit);
        }
    }

    public void HideUnitInfo()
    {
        if (unitInfoUI != null)
        {
            unitInfoUI.Hide();
        }
    }

public void HideSkillPanel()
    {
        if (skillPanelUI != null)
        {
            skillPanelUI.ClearSkills();
        }
    }



    public void ShowRewardScreen(List<ScriptableObject> rewards)
    {
        if (rewardScreenPanel == null) return;

        foreach (Transform child in rewardCardsContainer) Destroy(child.gameObject);

        rewardScreenPanel.SetActive(true);

        foreach (var rewardData in rewards)
        {
            GameObject cardInstance = Instantiate(rewardCardPrefab, rewardCardsContainer);
            cardInstance.GetComponent<RewardCardUI>()?.Setup(rewardData);
        }
    }

    public void HideRewardScreen()
    {
        if (rewardScreenPanel != null) rewardScreenPanel.SetActive(false);
    }

    /// <summary>
    /// 갈림길 선택 화면을 표시합니다. TurnManager가 호출합니다.
    /// </summary>
    public void ShowNodeSelectionScreen(List<MapNodeData> nextNodes)
    {
        if (nodeSelectionScreenPanel == null) return;

        foreach (Transform child in nodeButtonsContainer) Destroy(child.gameObject);

        nodeSelectionScreenPanel.SetActive(true);

        foreach (var nodeData in nextNodes)
        {
            GameObject buttonInstance = Instantiate(nodeButtonPrefab, nodeButtonsContainer);
            // TODO: NodeButtonUI 스크립트를 가져와서 데이터를 설정해야 합니다.
            // 예시: buttonInstance.GetComponent<NodeButtonUI>().Setup(nodeData);
        }
    }

    /// <summary>
    /// 갈림길 선택 화면을 숨깁니다.
    /// </summary>
    public void HideNodeSelectionScreen()
    {
        if (nodeSelectionScreenPanel != null) nodeSelectionScreenPanel.SetActive(false);
    }

    // --- 기존 메서드들 ---
    public void ShowBattleLog(string message) { }
    public void UpdateSkillPanel() { /* ... */ }
    public void UpdateTurnOrder() { /* ... */ }
}