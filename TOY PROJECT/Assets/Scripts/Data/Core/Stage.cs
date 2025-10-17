using UnityEngine;

[CreateAssetMenu(menuName = "Data/Stage")]
public class Stage : ScriptableObject
{
    [Header("Stage Info")]
    public string stageName = "Fire Temple";
    
    [Header("Room Sequence")]
    [Tooltip("이 스테이지의 모든 방을 순서대로 배치")]
    public RoomNode[] rooms;
    
    [Header("Starting Point")]
    public int startRoomIndex = 0;
}
