using UnityEngine;

public enum DebugType
{
    System,
    Input,
    Unit,
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
            case DebugType.Input:
                return "F49303";
            case DebugType.Unit:
                return "32CD32";
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
            case DebugType.Input:
                return "입력";
            case DebugType.Unit:
                return "유닛";
            default:
                return "Unknown";
        }
    }
}