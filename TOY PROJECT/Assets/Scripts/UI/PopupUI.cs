using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class PopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private UnityAction currentYesAction;
    private UnityAction currentNoAction;

    public void Init(string title, string message, UnityAction yesAction, UnityAction noAction = null)
    {
        titleText.text = title;
        messageText.text = message;

        if (currentYesAction != null)
        {
            yesButton.onClick.RemoveListener(currentYesAction);
        }
        if (currentNoAction != null)
        {
            noButton.onClick.RemoveListener(currentNoAction);
        }

        currentYesAction = () => {
            yesAction?.Invoke();
            Hide();
        };
        yesButton.onClick.AddListener(currentYesAction);

        if (noAction != null)
        {
            currentNoAction = () => {
                noAction.Invoke();
                Hide();
            };
            noButton.onClick.AddListener(currentNoAction);
            noButton.gameObject.SetActive(true);
        }
        else
        {
            noButton.gameObject.SetActive(false);
        }

        Show();
    }

    public void Show()
    {
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
        
        if (currentYesAction != null)
        {
            yesButton.onClick.RemoveListener(currentYesAction);
            currentYesAction = null;
        }
        if (currentNoAction != null)
        {
            noButton.onClick.RemoveListener(currentNoAction);
            currentNoAction = null;
        }
    }

    void Start()
    {
        Hide();
    }
}
