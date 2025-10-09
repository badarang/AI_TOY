using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

/// <summary>
/// WaitingRoom에서 플레이어들의 준비 상태를 관리하고 게임 시작을 담당하는 NetworkBehaviour
/// </summary>
public class WaitingRoomManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Networked, Capacity(4)]
    private NetworkDictionary<PlayerRef, NetworkBool> PlayerReadyStates => default;

    [Networked]
    private NetworkBool AllPlayersReady { get; set; }

    // 로컬 캐시 추가 - Host에서 즉시 반영을 위해
    private Dictionary<PlayerRef, bool> _localReadyCache = new Dictionary<PlayerRef, bool>();

    // --- UI 및 상태 변경 알림을 위한 이벤트 ---
    public event System.Action<PlayerRef, bool> OnPlayerReadyStateChanged;
    public event System.Action<bool> OnAllPlayersReadyStatusChanged;

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        // ChangeDetector 초기화
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            Debug.Log("[WaitingRoomManager] Initialized on Host. Syncing player list.");
            // 이미 존재하는 플레이어들을 PlayerReadyStates에 추가합니다.
            foreach (var player in Runner.ActivePlayers)
            {
                if (!PlayerReadyStates.ContainsKey(player))
                {
                    PlayerReadyStates.Add(player, false);
                    _localReadyCache[player] = false;
                }
            }
            CheckAllPlayersReady();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Host에서 매 틱마다 준비 상태 확인 (동기화 보장)
        if (HasStateAuthority)
        {
            CheckAllPlayersReady();
        }
    }

    public override void Render()
    {
        // 변경 감지 루프
        foreach (var changedProp in _changeDetector.DetectChanges(this, out var prev, out var curr))
        {
            switch (changedProp)
            {
                case nameof(AllPlayersReady):
                    var reader = GetPropertyReader<NetworkBool>(nameof(AllPlayersReady));
                    var (oldValue, newValue) = reader.Read(prev, curr);
                    OnAllPlayersReadyChanged(oldValue, newValue);
                    break;
            }
        }

        // NetworkDictionary 변경 감지 (수동)
        CheckDictionaryChanges();
    }

    private Dictionary<PlayerRef, bool> _lastKnownStates = new Dictionary<PlayerRef, bool>();

    private void CheckDictionaryChanges()
    {
        // 현재 상태를 확인하고 변경사항이 있으면 이벤트 발생
        var currentPlayers = new HashSet<PlayerRef>();

        foreach (var kvp in PlayerReadyStates)
        {
            currentPlayers.Add(kvp.Key);
            bool currentState = kvp.Value;

            // 이전 상태와 비교
            if (!_lastKnownStates.TryGetValue(kvp.Key, out var lastState) || lastState != currentState)
            {
                _lastKnownStates[kvp.Key] = currentState;
                OnPlayerReadyStateChanged?.Invoke(kvp.Key, currentState);
                Debug.Log($"[WaitingRoomManager] Player {kvp.Key.PlayerId} ready state changed to: {currentState}");
            }
        }

        // 제거된 플레이어 확인
        var removedPlayers = _lastKnownStates.Keys.Where(p => !currentPlayers.Contains(p)).ToList();
        foreach (var player in removedPlayers)
        {
            _lastKnownStates.Remove(player);
        }
    }

    private void OnAllPlayersReadyChanged(NetworkBool oldValue, NetworkBool newValue)
    {
        Debug.Log($"[WaitingRoomManager] AllPlayersReady changed: {oldValue} → {newValue}");
        OnAllPlayersReadyStatusChanged?.Invoke(newValue);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void ToggleReadyRpc(RpcInfo info = default)
    {
        // info.Source가 유효하지 않으면 (Host가 자신에게 호출) LocalPlayer 사용
        PlayerRef player = (info.Source == PlayerRef.None || info.Source.PlayerId < 0)
            ? Runner.LocalPlayer
            : info.Source;
        Debug.Log($"[WaitingRoomManager] ToggleReadyRpc called - info.Source: {info.Source.PlayerId}, using Player: {player.PlayerId}");

        bool currentState = GetPlayerReadyState(player);
        bool newState = !currentState;

        // NetworkDictionary와 로컬 캐시 모두 업데이트
        PlayerReadyStates.Set(player, newState);
        _localReadyCache[player] = newState;

        Debug.Log($"[WaitingRoomManager] Player {player.PlayerId} ready state: {currentState} → {newState}");

        // 즉시 상태 확인
        CheckAllPlayersReady();

        // 모든 클라이언트에 즉시 알림 (Host 포함)
        NotifyReadyStateChangedRpc(player, newState);
    }

    // Helper 메서드: Host는 로컬 캐시 우선, 클라이언트는 NetworkDictionary 사용
    private bool GetPlayerReadyState(PlayerRef player)
    {
        if (HasStateAuthority && _localReadyCache.TryGetValue(player, out bool cachedState))
        {
            return cachedState;
        }
        return PlayerReadyStates.TryGet(player, out var ready) && ready;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void NotifyReadyStateChangedRpc(PlayerRef player, NetworkBool isReady)
    {
        Debug.Log($"[WaitingRoomManager] NotifyReadyStateChangedRpc - Player {player.PlayerId}: {isReady}");
        OnPlayerReadyStateChanged?.Invoke(player, isReady);

        // 로컬 캐시도 즉시 업데이트
        _lastKnownStates[player] = isReady;
    }

    private void CheckAllPlayersReady()
    {
        if (!HasStateAuthority) return;

        var activePlayers = Runner.ActivePlayers.ToList();

        // 디버그: 현재 상태 출력
        Debug.Log($"[WaitingRoomManager] Checking ready states. Active players: {activePlayers.Count}");

        if (activePlayers.Count < 1)
        {
            AllPlayersReady = false;
            Debug.Log("[WaitingRoomManager] No active players. AllPlayersReady = false");
            return;
        }

        bool allReady = true;
        foreach (var player in activePlayers)
        {
            // 로컬 캐시 우선 사용
            bool isReady = GetPlayerReadyState(player);

            Debug.Log($"[WaitingRoomManager] Player {player.PlayerId}: Ready = {isReady}");

            if (!isReady)
            {
                allReady = false;
            }
        }

        AllPlayersReady = allReady;
        Debug.Log($"[WaitingRoomManager] CheckAllPlayersReady result: {AllPlayersReady}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RequestGameStartRpc(RpcInfo info = default)
    {
        Debug.Log($"[Host] Received game start request from {info.Source.PlayerId}.");

        // 최신 상태로 다시 확인
        CheckAllPlayersReady();

        if (AllPlayersReady)
        {
            Debug.Log("[Host] All players are ready. Loading InGame scene...");
            NetworkManager.Instance.LoadSceneNetwork("InGame");
        }
        else
        {
            Debug.LogWarning("[Host] Start request denied: Not all players are ready.");

            // 디버그: 어떤 플레이어가 준비 안 됐는지 출력
            foreach (var player in Runner.ActivePlayers)
            {
                bool isReady = PlayerReadyStates.TryGet(player, out var ready) && ready;
                if (!isReady)
                {
                    Debug.LogWarning($"[Host] Player {player.PlayerId} is NOT ready.");
                }
            }
        }
    }

    public void OnReadyButtonClicked()
    {
        ToggleReadyRpc();
    }

    public void OnStartButtonClicked()
    {
        Debug.Log("Start button clicked. Requesting game start...");
        RequestGameStartRpc();
    }

    // IPlayerJoined 인터페이스 구현
    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            Debug.Log($"[WaitingRoomManager] Player {player.PlayerId} joined. Adding to ready states.");
            if (!PlayerReadyStates.ContainsKey(player))
            {
                PlayerReadyStates.Add(player, false);
                _localReadyCache[player] = false;
            }
            CheckAllPlayersReady();
        }
    }

    // IPlayerLeft 인터페이스 구현
    public void PlayerLeft(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            Debug.Log($"[WaitingRoomManager] Player {player.PlayerId} left. Removing from ready states.");
            if (PlayerReadyStates.Remove(player))
            {
                _localReadyCache.Remove(player);
                CheckAllPlayersReady();
            }
        }
    }
}