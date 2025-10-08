// Assets/Scripts/Network/NetworkStageData.cs
using Fusion;
using UnityEngine;

/// <summary>
/// 네트워크 환경에서 스테이지 데이터를 동기화하는 구조체입니다.
/// StageData SO의 이름만 전송하고, 각 클라이언트가 GameAssetDatabase에서 로드합니다.
/// </summary>
public struct NetworkStageInfo : INetworkStruct
{
    public NetworkString<_64> stageName; // StageData의 name
    public int waveIndex;
    public int currentTurn;
}

/// <summary>
/// 네트워크 환경에서 적 스폰 정보를 전송하는 구조체
/// </summary>
public struct NetworkEnemySpawnInfo : INetworkStruct
{
    public int enemyTypeIndex; // UnitType enum을 int로
    public Vector2 spawnPos;
    public int turnNumber; // 어느 턴에 스폰될지
}