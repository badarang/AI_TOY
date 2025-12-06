using UnityEngine;
using TMPro;
using System;
using Fusion;

public class Portal : NetworkBehaviour, IAttackable
{
    [Header("UI")]
    [SerializeField] private TextMeshPro titleText;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private GameObject glowEffect;

    [Networked]
    public int HP { get; set; }

    private PortalData _portalData;
    private Action<int> _onEnter;
    private bool _visualsNeedUpdate = true;

    public override void Spawned()
    {
        // HP가 설정된 후 첫 시각적 업데이트를 예약합니다.
        _visualsNeedUpdate = true;
    }

    public void Setup(PortalData portalData, Action<int> enterCallback, int playerCount)
    {
        _portalData = portalData;
        _onEnter = enterCallback;

        if (HasStateAuthority)
        {
            HP = playerCount;
        }
    }

    public override void Render()
    {
        // HP가 변경되었거나 첫 업데이트가 필요할 때만 시각적 요소를 업데이트합니다.
        if (_visualsNeedUpdate || IsProxy)
        {
            UpdateVisuals();
            _visualsNeedUpdate = false;
        }
    }

    private void UpdateVisuals()
    {
        if (titleText != null)
        {
            titleText.text = $"{_portalData?.displayText}\n({HP} left)";
        }

        if (iconRenderer != null && _portalData?.icon != null)
        {
            iconRenderer.sprite = _portalData.icon;
        }

        if (glowEffect != null)
        {
            glowEffect.SetActive(true);
        }
    }
    
    public void TakeDamage(int amount)
    {
        // 입력 권한이 있는 클라이언트만 데미지 요청을 보낼 수 있습니다.
        if (Object.HasInputAuthority)
        {
            RequestTakeDamageRpc(amount);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RequestTakeDamageRpc(int amount)
    {
        if (HP <= 0) return;

        HP -= amount;
        
        // HP가 변경되었음을 감지하고 다음 Render에서 업데이트하도록 설정합니다.
        _visualsNeedUpdate = true;

        if (HP <= 0)
        {
            HP = 0;
            _onEnter?.Invoke(_portalData.targetRoomIndex);
        }
    }
}
