using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ScriptExecutionOrderSetup
{
    static ScriptExecutionOrderSetup()
    {
        SetExecutionOrder<Core>(-100);
        
        SetExecutionOrder<GameManager>(-50);
        SetExecutionOrder<PoolManager>(-50);
        SetExecutionOrder<GridManager>(-40);
        SetExecutionOrder<StageManager>(-30);
        SetExecutionOrder<UIManager>(-20);
        SetExecutionOrder<TurnManager>(-10);
        SetExecutionOrder<RewardManager>(0);
        SetExecutionOrder<EnemyAIManager>(0);
        SetExecutionOrder<PreviewManager>(0);
        SetExecutionOrder<InputManager>(10);
    }

    private static void SetExecutionOrder<T>(int order) where T : MonoBehaviour
    {
        var scriptName = typeof(T).Name;
        
        var guids = AssetDatabase.FindAssets($"t:MonoScript {scriptName}");
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[ScriptExecutionOrder] Could not find script: {scriptName}");
            return;
        }

        var scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        
        if (script == null)
        {
            Debug.LogWarning($"[ScriptExecutionOrder] Could not load script: {scriptName}");
            return;
        }

        var currentOrder = MonoImporter.GetExecutionOrder(script);
        if (currentOrder != order)
        {
            MonoImporter.SetExecutionOrder(script, order);
            Debug.Log($"[ScriptExecutionOrder] Set {scriptName} execution order to {order}");
        }
    }
}
