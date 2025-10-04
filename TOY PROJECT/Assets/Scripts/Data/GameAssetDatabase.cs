using System.Collections.Generic;
using UnityEngine;

// 게임에 존재하는 모든 스킬과 업그레이드 애셋을 담아두는 중앙 데이터베이스입니다.
// "쇼러너 AI"는 이 데이터베이스를 참조하여 보상 목록을 생성합니다.
[CreateAssetMenu(menuName = "Data/Game Asset Database")]
public class GameAssetDatabase : ScriptableObject
{
    [Header("스킬 라이브러리")]
    public List<SkillData> allSkills;

    [Header("업그레이드 라이브러리")]
    public List<UpgradeData> allUpgrades;
}
