using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 챕터의 각 층(계층)에서 가능한 스테이지 종류의 목록을 정의합니다.
/// </summary>
[System.Serializable]
public class StageLayer
{
    [Tooltip("이 층에서 플레이어가 선택할 수 있는 스테이지(방) 종류의 목록입니다.")]
    public List<StageType> possibleStageTypes;
}

/// <summary>
/// 하나의 챕터 전체의 흐름과 구조를 정의하는 데이터입니다.
/// </summary>
[CreateAssetMenu(menuName = "Data/Chapter Data")]
public class ChapterData : ScriptableObject
{
    [Tooltip("챕터의 이름입니다.")]
    public string chapterName;

    [Tooltip("챕터를 구성하는 스테이지 계층들의 목록입니다. 순서대로 진행됩니다.")]
    public List<StageLayer> layers;
}
