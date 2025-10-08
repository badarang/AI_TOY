using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUIManager : MonoBehaviour, IManager
{
    [Header("UI References")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private TextMeshProUGUI statusText;

    private string defaultRoomName = "MyGameRoom";

    public void BeforeInit()
    {
    }

    public void AfterInit()
    {
        SetupUI();
    }

    private void SetupUI()
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
        
        Debug.Log($"[LobbyUI] Creating room: {roomName}");
        
        PersistentCore.Instance.NetworkManager.StartHost(roomName);
        
        ShowWaitingPanel("Hosting room...");
        
        Invoke(nameof(LoadGameScene), 2f);
    }

    private void OnJoinButtonClick()
    {
        string roomName = GetRoomName();
        
        Debug.Log($"[LobbyUI] Joining room: {roomName}");
        
        PersistentCore.Instance.NetworkManager.JoinRoom(roomName);
        
        ShowWaitingPanel("Joining room...");
        
        Invoke(nameof(LoadGameScene), 2f);
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

    private void LoadGameScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("WaitingRoom");
    }

    private void OnDestroy()
    {
        if (hostButton != null)
            hostButton.onClick.RemoveListener(OnHostButtonClick);

        if (joinButton != null)
            joinButton.onClick.RemoveListener(OnJoinButtonClick);
    }
}
