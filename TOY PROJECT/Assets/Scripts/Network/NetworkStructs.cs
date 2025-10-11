using Fusion;

public struct PlayerSlotData : INetworkStruct
{
    public NetworkBool IsConnected;
    public NetworkBool IsReady;
    public UnitType SelectedUnit;
    public PlayerRef PlayerRef;

    public void Reset()
    {
        IsConnected = false;
        IsReady = false;
        SelectedUnit = default;
        PlayerRef = PlayerRef.None;
    }
}

public enum GameDifficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}

public enum WaitingRoomPhase
{
    Waiting = 0,
    Starting = 1,
    Loading = 2
}