using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;
using System.Linq;

public class WaitingRoomUIManager : MonoBehaviour, IManager
{
    [SerializeField] private WaitingRoomManager waitingRoomManager;

    [Header("UI References")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI roomNameText;

    private Dictionary<PlayerRef, GameObject> playerSlots = new Dictionary<PlayerRef, GameObject>();
    private bool isReady = false;

    public void BeforeInit() { }

    public void AfterInit()
    {
        SetupUI();
        UpdatePlayerList();
    }

    private void SetupUI()
    {
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonClick);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameButtonClick);
            startGameButton.gameObject.SetActive(false);
        }

        var networkManager = PersistentCore.Instance.NetworkManager;
        if (networkManager != null && networkManager.CurrentRunner != null)
        {
            bool isHost = networkManager.CurrentRunner.IsServer;
            
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(isHost);
            }
        }
    }

    private void Update()
    {
        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        var networkManager = PersistentCore.Instance.NetworkManager;
        if (networkManager == null || networkManager.CurrentRunner == null) return;

        var runner = networkManager.CurrentRunner;

        foreach (var player in runner.ActivePlayers)
        {
            if (!playerSlots.ContainsKey(player))
            {
                CreatePlayerSlot(player);
            }
        }

        var playersToRemove = new List<PlayerRef>();
        foreach (var kvp in playerSlots)
        {
            if (!runner.ActivePlayers.Contains(kvp.Key))
            {
                playersToRemove.Add(kvp.Key);
            }
        }

        foreach (var player in playersToRemove)
        {
            RemovePlayerSlot(player);
        }

        if (roomNameText != null)
        {
            roomNameText.text = $"Room: {runner.SessionInfo.Name}";
        }
    }

    private void CreatePlayerSlot(PlayerRef player)
    {
        if (playerSlotPrefab == null || playerListContainer == null) return;

        var slot = Instantiate(playerSlotPrefab, playerListContainer);
        var text = slot.GetComponentInChildren<TextMeshProUGUI>();
        
        if (text != null)
        {
            text.text = $"Player {player.PlayerId}";
        }

        playerSlots[player] = slot;
        Debug.Log($"[WaitingRoomUI] Player {player.PlayerId} joined the room");
    }

    private void RemovePlayerSlot(PlayerRef player)
    {
        if (playerSlots.TryGetValue(player, out var slot))
        {
            Destroy(slot);
            playerSlots.Remove(player);
            Debug.Log($"[WaitingRoomUI] Player {player.PlayerId} left the room");
        }
    }

    private void OnReadyButtonClick()
    {
        isReady = !isReady;

        if (readyButton != null)
        {
            var text = readyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = isReady ? "Cancel" : "Ready";
            }
        }

        Debug.Log($"[WaitingRoomUI] Ready state: {isReady}");
    }

    private void OnStartGameButtonClick()
    {
        Debug.Log("[WaitingRoomUI] Starting game...");
        waitingRoomManager.OnStartButtonClicked();
    }

    private void OnDestroy()
    {
        if (readyButton != null)
            readyButton.onClick.RemoveListener(OnReadyButtonClick);

        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(OnStartGameButtonClick);
    }
}
