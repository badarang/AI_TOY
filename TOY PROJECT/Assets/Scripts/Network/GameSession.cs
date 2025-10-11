using System;
using Fusion;
using UnityEngine;

namespace Network
{
    public class GameSession : NetworkBehaviour
    {
        public const int MAX_PLAYERS = 2;

        [Networked, Capacity(MAX_PLAYERS)]
        public NetworkArray<PlayerSlotData> PlayerSlots => default;

        [Networked]
        public GameDifficulty Difficulty { get; set; }

        [Networked]
        public WaitingRoomPhase Phase { get; set; }

        [Networked]
        public int ConnectedPlayerCount { get; set; }

        [Networked]
        public NetworkString<_64> CurrentStageName { get; set; }

        [Networked]
        public int CurrentWaveIndex { get; set; }

        [Networked]
        public NetworkBool IsStageLoaded { get; set; }

        public static GameSession Instance { get; private set; }

        public event Action OnSessionDataChanged;
        public event Action<string> OnStageChanged;

        private ChangeDetector _changeDetector;

        public override void Spawned()
        {
            if (Instance != null && Instance != this)
            {
                Runner.Despawn(Object);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            if (Object.HasStateAuthority)
            {
                InitializeSession();
            }

            Debug.Log($"[GameSession] Spawned. IsHost: {Object.HasStateAuthority}");
        }

        private void InitializeSession()
        {
            Phase = WaitingRoomPhase.Waiting;
            Difficulty = GameDifficulty.Normal;
            ConnectedPlayerCount = 0;
            CurrentStageName = string.Empty;
            CurrentWaveIndex = 0;
            IsStageLoaded = false;

            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                var slot = new PlayerSlotData();
                slot.Reset();
                PlayerSlots.Set(i, slot);
            }

            Debug.Log("[GameSession] Initialized");
        }

        public override void Render()
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                OnSessionDataChanged?.Invoke();

                if (change == nameof(CurrentStageName))
                {
                    OnStageChanged?.Invoke(CurrentStageName.ToString());
                }
            }
        }

        public int GetPlayerSlotIndex(PlayerRef playerRef)
        {
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (PlayerSlots[i].PlayerRef == playerRef)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool IsSlotAvailable(out int availableIndex)
        {
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (!PlayerSlots[i].IsConnected)
                {
                    availableIndex = i;
                    return true;
                }
            }
            availableIndex = -1;
            return false;
        }

        public bool AllPlayersReady()
        {
            if (ConnectedPlayerCount == 0) return false;

            int readyCount = 0;
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (PlayerSlots[i].IsConnected)
                {
                    if (!PlayerSlots[i].IsReady)
                    {
                        return false;
                    }
                    readyCount++;
                }
            }
            
            return readyCount == ConnectedPlayerCount;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RegisterPlayerRpc(PlayerRef player, RpcInfo info = default)
        {
            if (GetPlayerSlotIndex(player) != -1)
            {
                Debug.LogWarning($"[GameSession] Player {player} already registered");
                return;
            }

            if (!IsSlotAvailable(out int slotIndex))
            {
                Debug.LogError("[GameSession] No available slots");
                return;
            }

            var slot = PlayerSlots[slotIndex];
            slot.PlayerRef = player;
            slot.IsConnected = true;
            slot.IsReady = (slotIndex == 0); // Host is ready by default
            slot.SelectedUnit = UnitType.Hikai; // Set character to Hikai by default
            PlayerSlots.Set(slotIndex, slot);

            ConnectedPlayerCount++;

            Debug.Log($"[GameSession] Player {player} registered at slot {slotIndex}. Ready: {slot.IsReady}");
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void UnregisterPlayerRpc(PlayerRef player, RpcInfo info = default)
        {
            int slotIndex = GetPlayerSlotIndex(player);
            if (slotIndex == -1)
                return;

            var slot = PlayerSlots[slotIndex];
            slot.Reset();
            PlayerSlots.Set(slotIndex, slot);

            ConnectedPlayerCount--;

            Debug.Log($"[GameSession] Player {player} unregistered from slot {slotIndex}");
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void SetReadyRpc(PlayerRef player, bool isReady, RpcInfo info = default)
        {
            int slotIndex = GetPlayerSlotIndex(player);
            if (slotIndex == -1)
            {
                Debug.LogError($"[GameSession] Player {player} not found");
                return;
            }

            var slot = PlayerSlots[slotIndex];
            slot.IsReady = isReady;
            PlayerSlots.Set(slotIndex, slot);

            Debug.Log($"[GameSession] Player {player} ready: {isReady}");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
        public void StartGameRpc()
        {
            if (!AllPlayersReady())
            {
                Debug.LogWarning("Host tried to start game, but not all players are ready.");
                return;
            }

            if (Phase != WaitingRoomPhase.Waiting)
                return;

            Phase = WaitingRoomPhase.Starting;
            Debug.Log("[GameSession] Starting game...");
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void SetDifficultyRpc(int difficulty, RpcInfo info = default)
        {
            Difficulty = (GameDifficulty)difficulty;
            Debug.Log($"[GameSession] Difficulty set to: {Difficulty}");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void LoadStageRpc(string stageName, RpcInfo info = default)
        {
            CurrentStageName = stageName;
            CurrentWaveIndex = 0;
            IsStageLoaded = false;

            Debug.Log($"[GameSession] Loading stage: {stageName}");
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void SetStageLoadedRpc(bool loaded, RpcInfo info = default)
        {
            IsStageLoaded = loaded;
            Debug.Log($"[GameSession] Stage loaded: {loaded}");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void AdvanceWaveRpc(RpcInfo info = default)
        {
            CurrentWaveIndex++;
            Debug.Log($"[GameSession] Advanced to wave: {CurrentWaveIndex}");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
