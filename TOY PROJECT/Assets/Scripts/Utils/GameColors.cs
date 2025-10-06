using UnityEngine;

public static class GameColors
{
    public static class Log
    {
        public static readonly Color Selection = HexToColor("4CAF50");
        public static readonly Color State = HexToColor("2196F3");
        public static readonly Color Action = HexToColor("FF9800");
        public static readonly Color Combat = HexToColor("F44336");
        public static readonly Color Skill = HexToColor("9C27B0");
        public static readonly Color Movement = HexToColor("00BCD4");
        public static readonly Color Warning = HexToColor("FF5722");
        public static readonly Color Turn = HexToColor("3F51B5");
        public static readonly Color Success = HexToColor("8BC34A");
        public static readonly Color Error = HexToColor("D32F2F");
        public static readonly Color System = HexToColor("00BFFF");
        public static readonly Color Input = HexToColor("F49303");
        public static readonly Color Unit = HexToColor("32CD32");
        public static readonly Color AI = HexToColor("808080");
        public static readonly Color Debug = HexToColor("FFFF00");
        public static readonly Color Default = Color.white;
        
        public static string GetHex(LogType type)
        {
            return ColorToHex(GetColor(type));
        }
        
        public static Color GetColor(LogType type)
        {
            switch (type)
            {
                case LogType.Selection: return Selection;
                case LogType.State: return State;
                case LogType.Action: return Action;
                case LogType.Combat: return Combat;
                case LogType.Skill: return Skill;
                case LogType.Movement: return Movement;
                case LogType.Warning: return Warning;
                case LogType.Turn: return Turn;
                case LogType.Success: return Success;
                case LogType.Error: return Error;
                case LogType.System: return System;
                case LogType.Input: return Input;
                case LogType.Unit: return Unit;
                case LogType.AI: return AI;
                case LogType.Debug: return Debug;
                default: return Default;
            }
        }
    }
    
    public static class UI
    {
        public static readonly Color Primary = HexToColor("2196F3");
        public static readonly Color Secondary = HexToColor("FF9800");
        public static readonly Color Success = HexToColor("4CAF50");
        public static readonly Color Danger = HexToColor("F44336");
        public static readonly Color Warning = HexToColor("FF5722");
        public static readonly Color Info = HexToColor("00BCD4");
        public static readonly Color Light = HexToColor("F5F5F5");
        public static readonly Color Dark = HexToColor("212121");
        public static readonly Color Disabled = HexToColor("9E9E9E");
    }
    
    public static class Gameplay
    {
        public static readonly Color PlayerTeam = HexToColor("4CAF50");
        public static readonly Color EnemyTeam = HexToColor("F44336");
        public static readonly Color NeutralTeam = HexToColor("9E9E9E");
        
        public static readonly Color AttackPreview = HexToColor("FF0000");
        public static readonly Color MovePreview = HexToColor("FFFF00");
        public static readonly Color SkillPreview = HexToColor("9C27B0");
        
        public static readonly Color HealthFull = HexToColor("4CAF50");
        public static readonly Color HealthMedium = HexToColor("FF9800");
        public static readonly Color HealthLow = HexToColor("F44336");
        
        public static readonly Color APFull = HexToColor("2196F3");
        public static readonly Color APEmpty = HexToColor("424242");
    }
    
    public static class Grid
    {
        public static readonly Color Movable = HexToColor("00FF00", 0.3f);
        public static readonly Color Attackable = HexToColor("FF0000", 0.3f);
        public static readonly Color Danger = HexToColor("FF0000", 0.3f);
        public static readonly Color Selected = HexToColor("FFFF00", 0.5f);
        public static readonly Color Hover = HexToColor("FFFFFF", 0.2f);
        public static readonly Color Blocked = HexToColor("808080", 0.3f);
    }
    
    public static class Rarity
    {
        public static readonly Color Common = HexToColor("FFFFFF");
        public static readonly Color Uncommon = HexToColor("00FF00");
        public static readonly Color Rare = HexToColor("0070DD");
        public static readonly Color Epic = HexToColor("A335EE");
        public static readonly Color Legendary = HexToColor("FF8000");
    }
    
    public static Color HexToColor(string hex, float alpha = 1f)
    {
        hex = hex.Replace("#", "");
        
        if (hex.Length == 6)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, (byte)(alpha * 255));
        }
        
        Debug.LogWarning($"Invalid hex color: {hex}");
        return Color.white;
    }
    
    public static string ColorToHex(Color color)
    {
        Color32 c = color;
        return $"{c.r:X2}{c.g:X2}{c.b:X2}";
    }
    
    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
}
