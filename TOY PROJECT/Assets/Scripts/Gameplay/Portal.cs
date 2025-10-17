using UnityEngine;
using TMPro;
using System;

public class Portal : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshPro titleText;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private GameObject glowEffect;
    
    private PortalData _portalData;
    private Action<int> _onEnter;
    
    public void Setup(PortalData portalData, Action<int> enterCallback)
    {
        _portalData = portalData;
        _onEnter = enterCallback;
        
        if (titleText != null)
        {
            titleText.text = portalData.displayText;
        }
        
        if (iconRenderer != null && portalData.icon != null)
        {
            iconRenderer.sprite = portalData.icon;
        }
        
        if (glowEffect != null)
        {
            glowEffect.SetActive(true);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var playerUnit = other.GetComponent<PlayerUnit>();
            if (playerUnit != null && playerUnit.Object.HasInputAuthority)
            {
                _onEnter?.Invoke(_portalData.targetRoomIndex);
            }
        }
    }
}
