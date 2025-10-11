using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Network;

namespace UI
{
    public class WaitingRoomUIController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Transform content;
        [SerializeField] private GameObject playerSlotPrefab;
        [SerializeField] private Button startButton;
        [SerializeField] private Button readyButton;

        private WaitingRoomController _controller;
        private NetworkManager _networkManager;
        private readonly List<GameObject> _playerSlotInstances = new List<GameObject>();
        private bool _isSessionInitialized;

        private void Awake()
        {
            _controller = FindFirstObjectByType<WaitingRoomController>();
            _networkManager = PersistentCore.Instance.NetworkManager;

            if (_controller == null)
            {
                Debug.LogError("[WaitingRoomUI] WaitingRoomController not found");
            }

            if (_networkManager == null)
            {
                Debug.LogError("[WaitingRoomUI] NetworkManager not found");
            }
        }

        private void Start()
        {
            SetupUI();
            
            // Hide session-dependent UI elements initially
            startButton.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(false);
            ClearPlayerSlots();
        }

        private void Update()
        {
            // Wait for the GameSession to be initialized by the WaitingRoomController
            if (!_isSessionInitialized && _controller != null && _controller.Session != null)
            {
                _isSessionInitialized = true;
                RegisterCallbacks();
                UpdateUI(); // Trigger the first UI update
            }
        }

        private void SetupUI()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClick);
            }

            if (readyButton != null)
            {
                readyButton.onClick.AddListener(OnReadyButtonClick);
            }
        }

        private void RegisterCallbacks()
        {
            if (_controller.Session != null)
            {
                // Subscribe to the event that fires when network data changes
                _controller.Session.OnSessionDataChanged += UpdateUI;
            }
        }

        private void UpdateUI()
        {
            if (!_isSessionInitialized || _controller == null || _controller.Session == null) return;

            UpdatePlayerSlots();
            UpdateButtons();
        }
        
        private void ClearPlayerSlots()
        {
            foreach (var slot in _playerSlotInstances)
            {
                Destroy(slot);
            }
            _playerSlotInstances.Clear();
        }

        private void UpdatePlayerSlots()
        {
            ClearPlayerSlots();

            if (playerSlotPrefab == null || content == null) return;

            for (int i = 0; i < GameSession.MAX_PLAYERS; i++)
            {
                var slotData = _controller.Session.PlayerSlots[i];
                if (slotData.IsConnected)
                {
                    GameObject slotInstance = Instantiate(playerSlotPrefab, content);
                    _playerSlotInstances.Add(slotInstance);

                    var nameText = slotInstance.GetComponentInChildren<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = $"Player {i + 1}";
                    }
                    
                    var readyIcon = slotInstance.transform.Find("ReadyIcon")?.gameObject;
                    if (readyIcon != null)
                    {
                        readyIcon.SetActive(slotData.IsReady);
                    }
                }
            }
        }

        private void UpdateButtons()
        {
            if (startButton == null || readyButton == null) return;

            int mySlotIndex = _controller.Session.GetPlayerSlotIndex(_networkManager.Runner.LocalPlayer);
            if (mySlotIndex == -1)
            {
                startButton.gameObject.SetActive(false);
                readyButton.gameObject.SetActive(false);
                return;
            }

            if (_networkManager.IsHost)
            {
                startButton.gameObject.SetActive(true);
                readyButton.gameObject.SetActive(false);
                startButton.interactable = _controller.Session.AllPlayersReady();
            }
            else 
            {
                startButton.gameObject.SetActive(false);
                readyButton.gameObject.SetActive(true);
                
                var mySlot = _controller.Session.PlayerSlots[mySlotIndex];
                var readyButtonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (readyButtonText != null)
                {
                    readyButtonText.text = mySlot.IsReady ? "Cancel" : "Ready";
                }
            }
        }

        private void OnStartButtonClick()
        {
            _controller?.StartGame();
        }

        private void OnReadyButtonClick()
        {
            if (_controller?.Session == null || _networkManager == null) return;

            int mySlotIndex = _controller.Session.GetPlayerSlotIndex(_networkManager.Runner.LocalPlayer);
            if (mySlotIndex != -1)
            {
                var mySlot = _controller.Session.PlayerSlots[mySlotIndex];
                _controller.SetReady(!mySlot.IsReady);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
            }
            if (readyButton != null)
            {
                readyButton.onClick.RemoveAllListeners();
            }

            if (_controller != null && _controller.Session != null)
            {
                _controller.Session.OnSessionDataChanged -= UpdateUI;
            }
        }
    }
}
