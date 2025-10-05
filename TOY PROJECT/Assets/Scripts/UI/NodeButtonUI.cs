using UnityEngine;
using UnityEngine.UI;
using TMPro;

// "아레나의 갈림길" 선택 화면에 표시될 개별 노드 버튼의 UI와 상호작용을 담당합니다.
public class NodeButtonUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nodeTypeText; // "정예 전투", "이벤트" 등
    [SerializeField] private TextMeshProUGUI fanGainText;  // "팬 +250" 등
    [SerializeField] private Button selectButton;

    private MapNodeData currentNodeData;

    private void Start()
    {
        selectButton.onClick.AddListener(OnNodeSelected);
    }

    /// <summary>
    /// UIManager가 노드 데이터를 받아 이 버튼을 설정할 때 호출합니다.
    /// </summary>
    public void Setup(MapNodeData nodeData)
    {
        currentNodeData = nodeData;

        if (nodeTypeText != null)
        {
            nodeTypeText.text = nodeData.nodeType.ToString(); // Enum 이름을 그대로 사용
        }

        if (fanGainText != null)
        {
            // TODO: 팬심 예측 시스템이 구현되면, nodeData에서 예상 팬 수를 가져와야 합니다.
            // fanGainText.text = $"팬 +{nodeData.estimatedFanGain}";
            fanGainText.text = "팬 +???"; // 임시 텍스트
        }
    }

    /// <summary>
    /// 이 버튼이 클릭되었을 때 호출됩니다.
    /// </summary>
    private void OnNodeSelected()
    {
        if (currentNodeData == null) return;

        Debug.Log($"노드 선택: {currentNodeData.name}");

        // 1. 갈림길 선택 화면을 닫습니다.
        Core.Instance.UIManager.HideNodeSelectionScreen();

        // 2. TurnManager에게 플레이어의 선택을 알리고, 다음 라운드를 시작하도록 요청합니다.
        Core.Instance.TurnManager.OnNodeSelected(currentNodeData);
    }
}
