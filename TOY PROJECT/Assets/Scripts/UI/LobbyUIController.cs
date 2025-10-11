using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private LobbyController lobbyController;

    [Header("UI References")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private TextMeshProUGUI statusText;

    private string defaultRoomName = "MyGameRoom";

    public void SetupUI()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostButtonClick);

        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinButtonClick);

        if (roomNameInput != null)
            roomNameInput.text = defaultRoomName;

        ShowLobbyPanel();
    }

    private void OnHostButtonClick()
    {
        string roomName = GetRoomName();

        if (!ValidateInput(roomName))
            return;

        Debug.Log($"[LobbyUI] Creating room: {roomName}");
        ShowWaitingPanel("Creating room...");

        lobbyController.CreateRoom(roomName);
    }

    private void OnJoinButtonClick()
    {
        string roomName = GetRoomName();

        if (!ValidateInput(roomName))
            return;

        Debug.Log($"[LobbyUI] Joining room: {roomName}");
        ShowWaitingPanel("Joining room...");

        lobbyController.JoinRoom(roomName);
    }

    private bool ValidateInput(string roomName)
    {
        if (lobbyController.ValidateRoomName(roomName, out string error))
        {
            return true;
        }

        if (statusText != null)
        {
            statusText.text = error;
        }

        Debug.LogWarning($"[LobbyUI] Validation failed: {error}");
        return false;
    }

    private string GetRoomName()
    {
        if (roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text))
        {
            return roomNameInput.text;
        }
        return defaultRoomName;
    }

    private void ShowLobbyPanel()
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);
        
        if (waitingPanel != null)
            waitingPanel.SetActive(false);
    }

    private void ShowWaitingPanel(string message)
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);
        
        if (waitingPanel != null)
            waitingPanel.SetActive(true);
        
        if (statusText != null)
            statusText.text = message;
    }

    private void OnDestroy()
    {
        if (hostButton != null)
            hostButton.onClick.RemoveListener(OnHostButtonClick);

        if (joinButton != null)
            joinButton.onClick.RemoveListener(OnJoinButtonClick);
    }
}
