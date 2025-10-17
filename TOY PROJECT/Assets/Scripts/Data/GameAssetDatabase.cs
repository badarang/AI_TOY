using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Game Asset Database")]
public class GameAssetDatabase : ScriptableObject
{
    [Header("스킬 라이브러리")]
    public List<SkillData> allSkills;

    [Header("업그레이드 라이브러리")]
    public List<UpgradeData> allUpgrades;

    [Header("유닛 라이브러리")]
    public List<UnitData> allUnits;

    [Header("방 라이브러리")]
    public List<Room> allRooms;

    public UnitData GetUnitData(UnitType unitType)
    {
        return allUnits.Find(u => u.unitType == unitType);
    }

    public SkillData GetSkillByName(string skillName)
    {
        return allSkills.Find(s => s.skillMeta.nameKey == skillName);
    }

    public UpgradeData GetUpgradeByName(string upgradeName)
    {
        return allUpgrades.Find(u => u.upgradeName == upgradeName);
    }

    public Room GetRoomByName(string roomName)
    {
        return allRooms.Find(r => r.name == roomName);
    }
}
