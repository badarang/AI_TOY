using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class NetworkUnitBase : NetworkBehaviour
{
    public UnitData unitData;
    public FactionData factionData;

    [Networked] public int HP { get; set; }
    [Networked] public int AP { get; set; }
    [Networked] public Vector2Int Position { get; set; }
    [Networked] public NetworkBool HasActedThisTurn { get; set; }

    [Networked, Capacity(10)]
    private NetworkArray<int> SkillCooldowns => default;

    private List<Skill> skills = new List<Skill>();

    public override void Spawned()
    {
        if (unitData != null)
        {
            HP = unitData.maxHp;
            AP = unitData.maxAp;

            for (int i = 0; i < unitData.skills.Length; i++)
            {
                skills.Add(new Skill(unitData.skills[i]));
            }
        }
    }

    public virtual void OnTurnStart()
    {
        if (!HasStateAuthority) return;

        AP = unitData.maxAp;
        HasActedThisTurn = false;

        for (int i = 0; i < SkillCooldowns.Length; i++)
        {
            if (SkillCooldowns[i] > 0)
            {
                SkillCooldowns.Set(i, SkillCooldowns[i] - 1);
            }
        }

        Debug.Log($"{name} turn started. AP: {AP}");
    }

    public virtual void OnTurnEnd()
    {
        Debug.Log($"{name} turn ended");
    }

    public void RequestSkillUse(int skillIndex, Vector2Int targetPos)
    {
        if (!HasInputAuthority && !(this is EnemyUnit)) return;

        RPC_UseSkill(skillIndex, targetPos);
    }

    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    private void RPC_UseSkill(int skillIndex, Vector2Int targetPos)
    {
        if (skillIndex < 0 || skillIndex >= skills.Count)
        {
            Debug.LogError("Invalid skill index");
            return;
        }

        var skill = skills[skillIndex];

        if (SkillCooldowns[skillIndex] > 0)
        {
            Debug.LogWarning($"Skill on cooldown: {SkillCooldowns[skillIndex]} turns left");
            return;
        }

        int apCost = skill.GetAPCost();
        if (AP < apCost)
        {
            Debug.LogWarning("Not enough AP");
            return;
        }

        AP -= apCost;

        if (skill.data.cooldown > 0)
        {
            SkillCooldowns.Set(skillIndex, skill.data.cooldown);
        }

        // float animDuration = skill.Execute(this, targetPos);
        //
        // RPC_PlaySkillVisuals(skillIndex, targetPos, animDuration);

        HasActedThisTurn = true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlaySkillVisuals(int skillIndex, Vector2Int targetPos, float duration)
    {
        Debug.Log($"Playing skill visuals for skill {skillIndex} on {targetPos}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TakeDamage(int amount)
    {
        HP -= amount;
        Debug.Log($"{name} took {amount} damage. HP: {HP}");

        if (HP <= 0)
        {
            HP = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"{name} has died");

        if (HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    public List<Skill> GetSkills() => skills;

    public int GetSkillCooldown(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= SkillCooldowns.Length)
            return -1;
        return SkillCooldowns[skillIndex];
    }
}
