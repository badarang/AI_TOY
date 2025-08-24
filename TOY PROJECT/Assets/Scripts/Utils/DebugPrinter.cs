using UnityEngine;

public enum DebugType
{
    System,     // 시스템
    Unit,       // 유닛
    Battle,     // 전투
    Warning,    // 경고
    Error,      // 에러
    Skill,      // 스킬
    Item        // 아이템
}

public class DebugPrinter : MonoBehaviour
{
    public static void DebugColor(DebugType type, string message)
    {
        string colorHex = GetColorHex(type);
        string typeName = GetTypeName(type);
        string coloredMessage = $"<color=#{colorHex}>[{typeName}] {message}</color>";
        Debug.Log(coloredMessage);
    }
    
    private static string GetColorHex(DebugType type)
    {
        switch (type)
        {
            case DebugType.System:
                return "00BFFF";
            case DebugType.Unit:
                return "32CD32";
            case DebugType.Battle:
                return "FF4500";
            case DebugType.Warning:
                return "FFD700";
            case DebugType.Error:
                return "FF0000";
            case DebugType.Skill:
                return "9370DB";
            case DebugType.Item:
                return "FFA500";
            default:
                return "FFFFFF";
        }
    }
    
    private static string GetTypeName(DebugType type)
    {
        switch (type)
        {
            case DebugType.System:
                return "시스템";
            case DebugType.Unit:
                return "유닛";
            case DebugType.Battle:
                return "전투";
            case DebugType.Warning:
                return "경고";
            case DebugType.Error:
                return "에러";
            case DebugType.Skill:
                return "스킬";
            case DebugType.Item:
                return "아이템";
            default:
                return "정보";
        }
    }
}