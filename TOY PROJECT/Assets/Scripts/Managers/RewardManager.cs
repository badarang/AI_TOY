using System.Collections.Generic;
using UnityEngine;

// "쇼러너 AI"의 역할을 하는 핵심 매니저입니다.
// 플레이어의 상태와 팬심을 기반으로 보상을 생성하고 관리합니다.
public class RewardManager : MonoBehaviour, IManager
{

public void BeforeInit()
    {
    }

    public void AfterInit()
    {
    }

    [Header("데이터베이스 연결")]
    public GameAssetDatabase database;

    [Header("팬 시스템")]
    [SerializeField] private int currentFans = 0;
    public int CurrentFans => currentFans;

    // 임시로 플레이어 유닛을 직접 참조합니다. 추후 PlayerManager 등으로 대체될 수 있습니다.
    private UnitBase playerUnit;

    public void AddFans(int amount)
    {
        currentFans += amount;
        Debug.Log($"{amount}명의 팬이 추가되었습니다! 현재 팬: {currentFans}");
        // TODO: 팬 수에 따라 후원사 등급 변경 및 UI 업데이트 로직
    }

    public void RemoveFans(int amount)
    {
        currentFans = Mathf.Max(0, currentFans - amount);
        Debug.Log($"{amount}명의 팬이 떠났습니다... 현재 팬: {currentFans}");
    }

    /// <summary>
    /// 플레이어에게 제시할 3개의 보상 목록을 생성합니다.
    /// </summary>
    /// <returns>선별된 3개의 보상 (SkillData 또는 UpgradeData)</returns>
    public List<ScriptableObject> GenerateRewards()
    {
        // TODO: 플레이어 유닛 참조 로직 구현
        // playerUnit = ...;

        List<ScriptableObject> generatedRewards = new List<ScriptableObject>();

        // 1. [보상 종류 결정] 스킬, 업그레이드, 회복 중 어떤 카테고리의 보상을 몇 개 보여줄지 룰렛을 돌립니다.
        // 예: [스킬, 스킬, 업그레이드] 또는 [회복, 업그레이드, 업그레이드] 등

        // 2. [후보 목록 필터링] 데이터베이스의 전체 목록에서, 플레이어가 아직 만족하지 못한 '전제 조건(prerequisites)'을 가진 보상들을 제외합니다.

        // 3. [가중치 계산] 필터링된 후보 목록을 대상으로, '쇼러너 AI'의 규칙에 따라 가중치를 계산합니다.
        //    - 플레이어의 HP가 낮으면 -> '회복' 또는 '방어' 태그 보상의 가중치 증가
        //    - 플레이어가 '화염' 빌드를 타고 있으면 -> '화염' 태그 보상의 가중치 증가
        //    - 팬(후원사) 등급이 높을수록 -> 높은 Tier의 보상 가중치 증가

        // 4. [최종 선택] 계산된 가중치에 따라 룰렛을 돌려, 최종 보상 3개를 선택합니다.

        // 임시 반환값
        Debug.LogWarning("GenerateRewards 로직이 아직 구현되지 않았습니다. 임시 데이터를 반환합니다.");
        if (database.allSkills.Count > 0) generatedRewards.Add(database.allSkills[0]);

        return generatedRewards;
    }
}
