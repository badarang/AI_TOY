using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class RoomNode
{
    [Title("Room Info")]
    public string roomName = "Room 1";
    public RoomType roomType = RoomType.Battle;
    
    [Title("Room Content")]
    [Tooltip("이 방의 실제 데이터")]
    public Room room;
    
    [Title("Portals")]
    [Tooltip("방 완료 후 생성될 포탈들 (비어있으면 자동 진행)")]
    public PortalData[] portals;
    
    [Title("Turn & Penalty")]
    [Tooltip("권장 턴 수 (0 = 무제한)")]
    public int recommendedTurns = 0;
    
    [ShowIf("@recommendedTurns > 0")]
    [Tooltip("권장 턴 초과 시 턴당 Fan 감소량")]
    public int fanPenaltyPerTurn = 10;
}

[System.Serializable]
public class PortalData
{
    [Tooltip("포탈 위에 표시할 텍스트")]
    public string displayText = "전투";
    
    [Tooltip("포탈 아이콘")]
    public Sprite icon;
    
    [Tooltip("포탈이 생성될 벽 방향 (0=북, 1=동, 2=남, 3=서)")]
    [Range(0, 3)]
    public int wallDirection = 0;
    
    [Tooltip("이 포탈이 연결되는 방 인덱스")]
    public int targetRoomIndex;
    
    [Tooltip("UI에 표시할 타입")]
    public RoomType previewType;
}

public enum RoomType
{
    Battle,
    EliteBattle,
    Boss,
    Event,
    Shop,
    Rest
}
