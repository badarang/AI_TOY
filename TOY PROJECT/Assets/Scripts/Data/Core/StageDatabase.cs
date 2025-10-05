using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class EncounterData
{
    public EncounterType encounterType;
    public StageData[] stageData;
}

[CreateAssetMenu(menuName = "Data/Stage Database")]
public class StageDatabase : ScriptableObject
{
    public List<EncounterData> encounters;

    public StageData GetRandomStage(EncounterType type)
    {
        var matchingEncounter = encounters.FirstOrDefault(e => e.encounterType == type);
        if (matchingEncounter != null && matchingEncounter.stageData.Length > 0)
        {
            int randomIndex = Random.Range(0, matchingEncounter.stageData.Length);
            return matchingEncounter.stageData[randomIndex];
        }
        Debug.LogWarning($"No stages found for encounter type: {type}");
        return null;
    }
}
