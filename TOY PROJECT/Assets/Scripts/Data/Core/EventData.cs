using UnityEngine;

/// <summary>
/// 이벤트의 선택지를 정의하는 데이터입니다.
/// </summary>
[System.Serializable]
public class EventChoice
{
    [Tooltip("선택지에 표시될 텍스트입니다.")]
    public string choiceText;

    [Tooltip("이 선택지를 골랐을 때 얻게 될 보상입니다. (미구현)")]
    public string rewardId; // TODO: 보상 시스템과 연동
}

/// <summary>
/// 하나의 완전한 이벤트 내용을 정의하는 데이터입니다.
/// </summary>
[System.Serializable]
public class EventData
{
    [Tooltip("이벤트의 제목입니다.")]
    public string eventTitle;

    [Tooltip("이벤트의 설명 텍스트입니다."), TextArea(3, 10)]
    public string description;

    [Tooltip("플레이어가 선택할 수 있는 선택지 목록입니다.")]
    public EventChoice[] choices;
}
