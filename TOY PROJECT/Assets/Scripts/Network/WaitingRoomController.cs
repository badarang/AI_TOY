using System.Collections;
using UnityEngine;
using Fusion;

namespace Network
{
    public class WaitingRoomController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int inGameSceneIndex = 2;
        [SerializeField] private float gameStartDelay = 2f;

        [Header("Prefabs")]
        [SerializeField] private GameSession gameSessionPrefab;

        private NetworkManager _networkManager;
        private GameSession _gameSession;
        private PlayerRef _localPlayer;
        private bool _isStartingGame;

        public GameSession Session => _gameSession;

        private void Awake()
        {
            _networkManager = PersistentCore.Instance.NetworkManager;

            if (_networkManager == null)
            {
                Debug.LogError("[WaitingRoomController] NetworkManager not found in PersistentCore");
                return;
            }
        }

        private void Start()
        {
            RegisterCallbacks();
            // InitializeSession(); // This call is premature and causes the spawn exception.
        }

        private void RegisterCallbacks()
        {
            if (_networkManager != null)
            {
                _networkManager.OnSceneLoadComplete += OnSceneLoaded;
            }
        }

        private void OnSceneLoaded()
        {
            Debug.Log("[WaitingRoomController] Scene loaded, initializing session.");
            InitializeSession();
        }

        private void InitializeSession()
        {
            if (_networkManager == null || _networkManager.Runner == null)
                return;

            _localPlayer = _networkManager.Runner.LocalPlayer;

            if (GameSession.Instance == null)
            {
                if (_networkManager.Runner.IsServer)
                {
                    SpawnGameSession();
                }
            }
            else
            {
                _gameSession = GameSession.Instance;
                RegisterPlayer();
                SubscribeToSession();
            }
        }

        private void SpawnGameSession()
        {
            if (gameSessionPrefab == null)
            {
                Debug.LogError("[WaitingRoomController] GameSession prefab not assigned");
                return;
            }

            var sessionObj = _networkManager.Runner.Spawn(
                gameSessionPrefab,
                Vector3.zero,
                Quaternion.identity,
                null,
                (runner, obj) =>
                {
                    Debug.Log("[WaitingRoomController] GameSession spawned");
                }
            );

            _gameSession = sessionObj.GetComponent<GameSession>();

            StartCoroutine(WaitForSessionThenRegister());
        }

        private IEnumerator WaitForSessionThenRegister()
        {
            yield return new WaitUntil(() => GameSession.Instance != null);

            _gameSession = GameSession.Instance;
            RegisterPlayer();
            SubscribeToSession();
        }

        private void RegisterPlayer()
        {
            if (_gameSession == null || _localPlayer == PlayerRef.None)
                return;

            _gameSession.RegisterPlayerRpc(_localPlayer);
            Debug.Log($"[WaitingRoomController] Registered player: {_localPlayer}");
        }

        private void SubscribeToSession()
        {
            if (_gameSession == null)
                return;

            _gameSession.OnSessionDataChanged += OnSessionDataChanged;
        }

        private void OnSessionDataChanged()
        {
            if (_gameSession == null)
                return;

            if (_gameSession.Phase == WaitingRoomPhase.Starting && !_isStartingGame)
            {
                _isStartingGame = true;
                StartCoroutine(LoadInGameScene());
            }
        }

        public void SetReady(bool isReady)
        {
            if (_gameSession == null)
            {
                Debug.LogError("[WaitingRoomController] GameSession not found");
                return;
            }

            _gameSession.SetReadyRpc(_localPlayer, isReady);
            Debug.Log($"[WaitingRoomController] Set ready: {isReady}");
        }

        public void SetDifficulty(GameDifficulty difficulty)
        {
            if (_gameSession == null)
                return;

            if (!_networkManager.IsHost)
            {
                Debug.LogWarning("[WaitingRoomController] Only host can change difficulty");
                return;
            }

            _gameSession.SetDifficultyRpc((int)difficulty);
        }

        public void StartGame()
        {
            if (_gameSession == null)
            {
                Debug.LogError("[WaitingRoomController] GameSession not found");
                return;
            }

            if (!_networkManager.IsHost)
            {
                Debug.LogWarning("[WaitingRoomController] Only host can start the game");
                return;
            }

            _gameSession.StartGameRpc();
        }

        private IEnumerator LoadInGameScene()
        {
            Debug.Log("[WaitingRoomController] Starting game in " + gameStartDelay + " seconds...");

            yield return new WaitForSeconds(gameStartDelay);

            if (_networkManager.IsHost)
            {
                var scene = SceneRef.FromIndex(inGameSceneIndex);
                _networkManager.Runner.LoadScene(scene);
                Debug.Log("[WaitingRoomController] Loading InGame scene");
            }
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.OnSceneLoadComplete -= OnSceneLoaded;
            }

            if (_gameSession != null)
            {
                _gameSession.OnSessionDataChanged -= OnSessionDataChanged;

                if (_localPlayer != PlayerRef.None && _gameSession.Object != null && _gameSession.Object.IsValid)
                {
                    _gameSession.UnregisterPlayerRpc(_localPlayer);
                }
            }
        }
    }
}
