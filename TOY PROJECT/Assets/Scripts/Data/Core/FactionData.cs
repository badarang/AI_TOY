using UnityEngine;

public enum FactionType
{
    None = 0,
    Player,
    Neutral,
    Enemy_Red,
    Enemy_Green,
    Enemy_Blue
}

[CreateAssetMenu(menuName = "Data/FactionData")]
public class FactionData : ScriptableObject
{
    public FactionType factionType;
    public string factionName;
    public Color factionColor;
}