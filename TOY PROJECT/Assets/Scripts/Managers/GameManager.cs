using UnityEngine;

public class GameManager : MonoBehaviour, IManager
{
    [Header("Available Stages")]
    [SerializeField] private Room[] availableStages;
    
    private StageManager _stageManager;
    
    public void BeforeInit() { }
    
    public void AfterInit()
    {
        _stageManager = Core.Instance.StageManager;
    }
    
public void StartNewStage(int stageIndex = 0)
    {
        if (availableStages == null || availableStages.Length == 0)
        {
            Debug.LogError("[GameManager] No stages available!");
            return;
        }
        
        if (stageIndex < 0 || stageIndex >= availableStages.Length)
        {
            Debug.LogError($"[GameManager] Invalid stage index: {stageIndex}");
            return;
        }
        
        var room = availableStages[stageIndex];
        Debug.Log($"[GameManager] Loading room: {room.name}");
        
        _stageManager.RequestLoadStage(room.name);
    }
}
