using UnityEngine;

public static class GameColors
{
    public static class Log
    {
        public static readonly Color Selection = HexToColor("A5D6A7"); // 밝은 민트그린 – 선택
        public static readonly Color State = HexToColor("90CAF9");     // 밝은 스카이블루 – 상태
        public static readonly Color Action = HexToColor("FFCC80");    // 따뜻한 오렌지톤 – 행동
        public static readonly Color Combat = HexToColor("EF9A9A");    // 부드러운 레드 – 전투
        public static readonly Color Skill = HexToColor("CE93D8");     // 라벤더 – 스킬
        public static readonly Color Movement = HexToColor("80DEEA");  // 청록빛 하늘색 – 이동
        public static readonly Color Warning = HexToColor("FFAB91");   // 살구빛 오렌지 – 경고
        public static readonly Color Turn = HexToColor("9FA8DA");      // 밝은 인디고톤 – 턴
        public static readonly Color Success = HexToColor("C5E1A5");   // 라이트 그린 – 성공
        public static readonly Color Error = HexToColor("E57373");     // 부드러운 레드톤 – 오류
        public static readonly Color System = HexToColor("81D4FA");    // 밝은 블루 – 시스템
        public static readonly Color Input = HexToColor("FFD54F");     // 옅은 노랑 – 입력
        public static readonly Color Unit = HexToColor("AED581");      // 연초록 – 유닛
        public static readonly Color AI = HexToColor("B0BEC5");        // 회청색 – 인공지능
        public static readonly Color Debug = HexToColor("FFF176");     // 밝은 옐로 – 디버그
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
