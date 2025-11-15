// Assets/Scripts/Managers/RewardManager.cs 개선 버전
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class RewardManager : NetworkBehaviour, IManager
{
    public void BeforeInit() { }

    public void AfterInit() { }

    public void Dispose() { }

    [Header("데이터베이스 연결")]
    public GameAssetDatabase database;

    [Header("팬 시스템")]
    [Networked]
    public int CurrentFans { get; set; }

    private UnitBase playerUnit;

    public void AddFans(int amount)
    {
        if (HasStateAuthority)
        {
            CurrentFans += amount;
            Debug.Log($"{amount}명의 팬이 추가되었습니다! 현재 팬: {CurrentFans}");
        }
    }

    public void RemoveFans(int amount)
    {
        if (HasStateAuthority)
        {
            CurrentFans = Mathf.Max(0, CurrentFans - amount);
            Debug.Log($"{amount}명의 팬이 떠났습니다... 현재 팬: {CurrentFans}");
        }
    }

    public void SetPlayerUnit(UnitBase unit)
    {
        playerUnit = unit;
    }

    /// <summary>
    /// 플레이어에게 제시할 3개의 보상 목록을 생성합니다.
    /// BuildData를 활용하여 전제 조건을 확인합니다.
    /// </summary>
    public List<ScriptableObject> GenerateRewards()
    {
        if (playerUnit == null)
        {
            Debug.LogError("PlayerUnit이 설정되지 않았습니다!");
            return new List<ScriptableObject>();
        }

        List<ScriptableObject> candidates = new List<ScriptableObject>();

        // 1. 스킬 후보군 필터링
        foreach (var skillData in database.allSkills)
        {
            // 전제 조건 확인
            if (!playerUnit.BuildData.MeetsPrerequisites(skillData.prerequisites))
                continue;

            // 이미 배운 스킬 제외
            if (playerUnit.BuildData.IsEquipped(skillData))
                continue;

            candidates.Add(skillData);
        }

        // 2. 업그레이드 후보군 필터링
        foreach (var upgradeData in database.allUpgrades)
        {
            // 전제 조건 확인
            if (!playerUnit.BuildData.MeetsPrerequisites(upgradeData.prerequisites))
                continue;

            // 이미 배운 업그레이드 제외
            if (playerUnit.BuildData.IsEquipped(upgradeData))
                continue;

            candidates.Add(upgradeData);
        }

        // 3. 가중치 기반 선택
        List<ScriptableObject> selectedRewards = SelectRewardsWithWeight(candidates);

        return selectedRewards;
    }

    /// <summary>
    /// 가중치를 고려하여 보상을 선택합니다.
    /// TODO: 팬 수, HP 상태, 빌드 방향성 등을 고려한 가중치 로직 구현
    /// </summary>
    private List<ScriptableObject> SelectRewardsWithWeight(List<ScriptableObject> candidates)
    {
        List<ScriptableObject> selectedRewards = new List<ScriptableObject>();

        // 간단한 랜덤 선택 (추후 가중치 로직으로 개선)
        var shuffled = candidates.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < Mathf.Min(3, shuffled.Count); i++)
        {
            selectedRewards.Add(shuffled[i]);
        }

        return selectedRewards;
    }

    /// <summary>
    /// 플레이어가 보상을 선택했을 때 호출됩니다.
    /// 클라이언트에서 호출하면 서버로 RPC 전송
    /// </summary>
    public void ApplyReward(ScriptableObject reward)
    {
        if (playerUnit == null)
            return;

        // 보상 적용은 서버에서만 수행
        if (HasStateAuthority)
        {
            ApplyRewardInternal(reward);
        }
        else
        {
            // 클라이언트는 서버에 요청
            // TODO: ScriptableObject를 직접 RPC로 전송할 수 없으므로
            // 보상의 ID나 이름을 전송하도록 수정 필요
            Debug.LogWarning("[RewardManager] Client cannot apply reward directly. Need to implement RPC.");
        }
    }

    /// <summary>
    /// 서버에서 실제 보상을 적용하는 내부 메서드
    /// </summary>
    private void ApplyRewardInternal(ScriptableObject reward)
    {
        if (reward is SkillData skillData)
        {
            playerUnit.LearnSkill(skillData);
            Debug.Log($"[RewardManager] Player learned skill: {skillData.skillMeta.nameKey}");
        }
        else if (reward is UpgradeData upgradeData)
        {
            // 스킬별 업그레이드인지 일반 업그레이드인지 판단
            // TODO: UpgradeData에 targetSkillName 필드가 있다면 해당 스킬에 적용
            playerUnit.ApplyGeneralUpgrade(upgradeData);
            Debug.Log($"[RewardManager] Player applied upgrade: {upgradeData.upgradeName}");
        }
    }

    /// <summary>
    /// RPC를 통해 보상을 적용합니다 (향후 구현)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void ApplyRewardByNameRpc(string rewardName, bool isSkill, RpcInfo info = default)
    {
        if (!HasStateAuthority)
            return;

        ScriptableObject reward = null;

        if (isSkill)
        {
            reward = database.allSkills.FirstOrDefault(s => s.skillMeta.nameKey == rewardName);
        }
        else
        {
            reward = database.allUpgrades.FirstOrDefault(u => u.upgradeName == rewardName);
        }

        if (reward != null)
        {
            ApplyRewardInternal(reward);
        }
        else
        {
            Debug.LogError($"[RewardManager] Reward not found: {rewardName}");
        }
    }

    /// <summary>
    /// 현재 플레이어의 빌드 상태를 디버깅용으로 출력합니다.
    /// </summary>
    [ContextMenu("Print Player Build")]
    public void PrintPlayerBuild()
    {
        if (playerUnit != null)
        {
            Debug.Log(playerUnit.GetBuildSummary());
        }
    }
}
