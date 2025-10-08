// Assets/Scripts/Managers/NetworkStageManager.cs
using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 네트워크 환경에서 스테이지 데이터를 관리하는 매니저
/// 서버가 StageData 이름을 브로드캐스트하면, 각 클라이언트가 GameAssetDatabase에서 로드합니다.
/// </summary>
public class NetworkStageManager : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameAssetDatabase gameDatabase;

    [Networked]
    private NetworkStageInfo CurrentStageInfo { get; set; }

    [Networked, Capacity(20)]
    private NetworkArray<NetworkEnemySpawnInfo> EnemySpawnQueue => default;

    private StageData loadedStageData;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            // 서버: 첫 스테이지 로드
            LoadStageOnServerRpc("Stage_1");
        }
    }

    /// <summary>
    /// 서버가 StageData를 로드하고 이름을 네트워크에 브로드캐스트합니다.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void LoadStageOnServerRpc(string stageName)
    {
        if (!HasStateAuthority) return;

        // GameAssetDatabase에서 StageData 찾기
        loadedStageData = gameDatabase.GetStageByName(stageName);

        if (loadedStageData == null)
        {
            Debug.LogError($"Failed to find StageData: {stageName} in GameAssetDatabase");
            return;
        }

        // 네트워크 정보 설정 (이름만 전송)
        CurrentStageInfo = new NetworkStageInfo
        {
            stageName = stageName,
            waveIndex = 0,
            currentTurn = 0
        };

        // 첫 웨이브의 적 스폰 정보 설정
        if (loadedStageData.waves.Length > 0)
        {
            SetupWaveSpawns(0);
        }

        Debug.Log($"[Server] Loaded stage: {stageName}");

        // 클라이언트들에게 스테이지 로드 알림
        NotifyStageLoadedRpc(stageName);
    }

    /// <summary>
    /// 클라이언트에게 스테이지가 로드되었음을 알립니다.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void NotifyStageLoadedRpc(NetworkString<_64> stageName)
    {
        if (HasStateAuthority) return; // 서버는 이미 로드함

        // 클라이언트: GameAssetDatabase에서 StageData 로드
        loadedStageData = gameDatabase.GetStageByName(stageName.ToString());

        if (loadedStageData == null)
        {
            Debug.LogError($"[Client] Failed to find StageData: {stageName} in GameAssetDatabase");
            return;
        }

        Debug.Log($"[Client] Loaded stage from database: {stageName}");

        // 클라이언트에서 StageManager에 스테이지 로드 요청
        if (Core.Instance?.StageManager != null)
        {
            Core.Instance.StageManager.LoadStage(loadedStageData);
        }
    }

    /// <summary>
    /// 웨이브의 스폰 정보를 네트워크 배열에 설정합니다.
    /// </summary>
    private void SetupWaveSpawns(int waveIndex)
    {
        if (waveIndex >= loadedStageData.waves.Length) return;

        var wave = loadedStageData.waves[waveIndex];
        int spawnIndex = 0;

        // 턴별 스폰이 있는 경우
        if (wave.turnSpawns != null && wave.turnSpawns.Length > 0)
        {
            foreach (var turnSpawn in wave.turnSpawns)
            {
                foreach (var enemySpawn in turnSpawn.enemies)
                {
                    if (spawnIndex >= EnemySpawnQueue.Length) break;

                    EnemySpawnQueue.Set(spawnIndex, new NetworkEnemySpawnInfo
                    {
                        enemyTypeIndex = (int)enemySpawn.enemyType,
                        spawnPos = enemySpawn.spawnPos,
                        turnNumber = turnSpawn.turnNumber
                    });
                    spawnIndex++;
                }
            }
        }
        // 즉시 스폰
        else if (wave.enemySpawns != null)
        {
            foreach (var enemySpawn in wave.enemySpawns)
            {
                if (spawnIndex >= EnemySpawnQueue.Length) break;

                EnemySpawnQueue.Set(spawnIndex, new NetworkEnemySpawnInfo
                {
                    enemyTypeIndex = (int)enemySpawn.enemyType,
                    spawnPos = enemySpawn.spawnPos,
                    turnNumber = 0 // 즉시 스폰
                });
                spawnIndex++;
            }
        }
    }

    /// <summary>
    /// 현재 로드된 StageData를 반환합니다.
    /// </summary>
    public StageData GetCurrentStageData()
    {
        return loadedStageData;
    }

    /// <summary>
    /// 현재 스테이지 정보를 조회합니다.
    /// </summary>
    public NetworkStageInfo GetCurrentStageInfo()
    {
        return CurrentStageInfo;
    }

    /// <summary>
    /// UnitType으로부터 UnitData를 가져옵니다.
    /// </summary>
    public UnitData GetUnitData(UnitType unitType)
    {
        return gameDatabase.GetUnitData(unitType);
    }

    /// <summary>
    /// 특정 턴에 스폰될 적 목록을 반환합니다.
    /// </summary>
    public List<NetworkEnemySpawnInfo> GetEnemySpawnsForTurn(int turnNumber)
    {
        List<NetworkEnemySpawnInfo> spawns = new List<NetworkEnemySpawnInfo>();

        for (int i = 0; i < EnemySpawnQueue.Length; i++)
        {
            var spawnInfo = EnemySpawnQueue[i];
            if (spawnInfo.enemyTypeIndex != 0 && spawnInfo.turnNumber == turnNumber)
            {
                spawns.Add(spawnInfo);
            }
        }

        return spawns;
    }
}