using UnityEngine;
using TMPro;

public class TurnOrderUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnInfoText;
    
    public TurnManager turnManager;
    
    void Update()
    {
        UpdateOrder();
    }
    
    public void UpdateOrder()
    {
        if (turnManager == null || turnInfoText == null) return;
        
        string turnInfo = $"Turn {turnManager.TurnNumber + 1}\n";
        
        if (turnManager.IsMyTurn)
        {
            turnInfo += "<color=green>Your Turn</color>";
        }
        else if (turnManager.CurrentTurnPlayer != Fusion.PlayerRef.None)
        {
            turnInfo += $"<color=yellow>Player {turnManager.CurrentTurnPlayer.PlayerId + 1}'s Turn</color>";
        }
        else
        {
            turnInfo += "<color=red>Enemy Turn</color>";
        }
        
        turnInfoText.text = turnInfo;
    }
}
