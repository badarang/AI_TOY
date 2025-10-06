using UnityEngine;

public enum LogType
{
    Selection,
    State,
    Action,
    Combat,
    Skill,
    Movement,
    Warning,
    Turn,
    Success,
    Error,
    System,
    Input,
    Unit,
    AI,
    Debug
}

public class DebugPrinter : MonoBehaviour
{
public static void LogColor(LogType type, string message)
    {
        string colorHex = GetColorHex(type);
        string typeName = GetTypeName(type);
        string coloredMessage = $"<color=#{colorHex}>[{typeName}]</color> {message}";
        
        if (type == LogType.Warning)
            Debug.LogWarning(coloredMessage);
        else if (type == LogType.Error)
            Debug.LogError(coloredMessage);
        else
            Debug.Log(coloredMessage);
    }
    
private static string GetColorHex(LogType type)
    {
        return GameColors.Log.GetHex(type);
    }
    
private static string GetTypeName(LogType type)
    {
        switch (type)
        {
            case LogType.Selection: return "선택";
            case LogType.State: return "상태";
            case LogType.Action: return "행동";
            case LogType.Combat: return "전투";
            case LogType.Skill: return "스킬";
            case LogType.Movement: return "이동";
            case LogType.Warning: return "경고";
            case LogType.Turn: return "턴";
            case LogType.Success: return "성공";
            case LogType.Error: return "오류";
            case LogType.System: return "시스템";
            case LogType.Input: return "입력";
            case LogType.Unit: return "유닛";
            case LogType.AI: return "AI";
            case LogType.Debug: return "디버그";
            default: return "Unknown";
        }
    }
}