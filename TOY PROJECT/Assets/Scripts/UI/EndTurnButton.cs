using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EndTurn 버튼을 제어하는 스크립트입니다.
/// 자신의 차례일 때만 버튼이 활성화됩니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class EndTurnButton : MonoBehaviour
{
    private Button button;
    private TurnManager turnManager;

    void Awake()
    {
        button = GetComponent<Button>();
        
        // 버튼 클릭 이벤트 등록
        button.onClick.AddListener(OnEndTurnClicked);
    }

    void Start()
    {
        // TurnManager 찾기
        if (Core.Instance != null)
        {
            turnManager = Core.Instance.TurnManager;
        }
        
        // 초기 상태는 비활성화
        button.interactable = false;
    }

    void OnEndTurnClicked()
    {
        if (turnManager != null)
        {
            Debug.Log("[EndTurnButton] End Turn button clicked!");
            turnManager.RequestEndTurn();
        }
        else
        {
            Debug.LogWarning("[EndTurnButton] TurnManager not found!");
        }
    }

    void OnDestroy()
    {
        // 메모리 누수 방지
        if (button != null)
        {
            button.onClick.RemoveListener(OnEndTurnClicked);
        }
    }
}
