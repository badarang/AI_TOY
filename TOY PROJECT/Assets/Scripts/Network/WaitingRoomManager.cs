using UnityEngine;
using Fusion;
using System.Linq;

/// <summary>
/// WaitingRoom에서 플레이어들의 준비 상태를 관리하고 게임 시작을 담당하는 NetworkBehaviour
/// </summary>
public class WaitingRoomManager : NetworkBehaviour
{
    [Networked, Capacity(4)]
    private NetworkDictionary<PlayerRef, NetworkBool> PlayerReadyStates => default;

    [Networked]
    private NetworkBool AllPlayersReady { get; set; }

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
            Debug.Log("[WaitingRoomManager] Initialized on Host. Clearing ready states.");
            PlayerReadyStates.Clear();
            foreach (var player in Runner.ActivePlayers)
                PlayerReadyStates.Add(player, false);

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
    }

    private void OnAllPlayersReadyChanged(NetworkBool oldValue, NetworkBool newValue)
    {
        Debug.Log($"[WaitingRoomManager] AllPlayersReady changed: {oldValue} → {newValue}");
        OnAllPlayersReadyStatusChanged?.Invoke(newValue);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void ToggleReadyRpc(RpcInfo info = default)
    {
        PlayerRef player = info.Source;
        bool currentState = PlayerReadyStates.TryGet(player, out var ready) && ready;
        bool newState = !currentState;

        PlayerReadyStates.Set(player, newState);
        Debug.Log($"[WaitingRoomManager] Player {player.PlayerId} ready state set to: {newState}");

        CheckAllPlayersReady();
        NotifyReadyStateChangedRpc(player, newState);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void NotifyReadyStateChangedRpc(PlayerRef player, NetworkBool isReady)
    {
        OnPlayerReadyStateChanged?.Invoke(player, isReady);
    }

    private void CheckAllPlayersReady()
    {
        if (!HasStateAuthority) return;

        var activePlayers = Runner.ActivePlayers;
        if (activePlayers.Count() < 1)
        {
            AllPlayersReady = false;
            return;
        }

        foreach (var player in activePlayers)
        {
            if (!PlayerReadyStates.TryGet(player, out var ready) || !ready)
            {
                AllPlayersReady = false;
                return;
            }
        }

        AllPlayersReady = true;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RequestGameStartRpc(RpcInfo info = default)
    {
        Debug.Log($"[Host] Received game start request from {info.Source.PlayerId}.");
        if (AllPlayersReady)
        {
            Debug.Log("[Host] All players are ready. Loading InGame scene...");
            NetworkManager.Instance.LoadSceneNetwork("InGame");
        }
        else
        {
            Debug.LogWarning("[Host] Start request denied: Not all players are ready.");
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

}
