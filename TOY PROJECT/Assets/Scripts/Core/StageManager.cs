using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public GridManager gridManager;
    public TurnManager turnManager;
    public StageData[] stages;

    private PlayerUnit player;
    private List<EnemyUnit> enemies = new List<EnemyUnit>();

    public void LoadStage(int stageIndex)
    {
        StageData stage = stages[stageIndex];
        gridManager.GenerateGrid(stage);
        SpawnPlayer(stage);
        SpawnEnemies(stage);
        SpawnObstacles(stage);
        turnManager.StartPlayerTurn();
    }

    void SpawnPlayer(StageData stage)
    {
        string prefabName = GetPrefabName(stage.playerType);
        GameObject playerPrefab = Resources.Load<GameObject>($"Prefabs/Units/{prefabName}");
        if (playerPrefab == null)
        {
            Debug.LogError($"Player 프리팹을 찾을 수 없습니다: {prefabName}");
            return;
        }
        GameObject obj = Instantiate(playerPrefab, GridToWorld(stage.playerSpawn), Quaternion.identity);
        player = obj.GetComponent<PlayerUnit>();
        player.position = stage.playerSpawn;
        // UnitData 등 추가 세팅 필요시 여기에
    }

    void SpawnEnemies(StageData stage)
    {
        foreach (var enemyData in stage.enemySpawns)
        {
            string prefabName = GetPrefabName(enemyData.enemyType);
            GameObject enemyPrefab = Resources.Load<GameObject>($"Prefabs/Units/{prefabName}");
            if (enemyPrefab == null)
            {
                Debug.LogError($"Enemy 프리팹을 찾을 수 없습니다: {prefabName}");
                continue;
            }
            GameObject obj = Instantiate(enemyPrefab, GridToWorld(enemyData.spawnPos), Quaternion.identity);
            EnemyUnit enemy = obj.GetComponent<EnemyUnit>();
            enemy.position = enemyData.spawnPos;
            enemies.Add(enemy);
        }
    }

    void SpawnObstacles(StageData stage)
    {
        foreach (var obsData in stage.obstacleSpawns)
        {
            if (obsData.obstacleData == null || obsData.obstacleData.prefab == null) continue;
            GameObject obj = Instantiate(obsData.obstacleData.prefab, GridToWorld(obsData.spawnPos), Quaternion.identity);
            Obstacle obstacle = obj.GetComponent<Obstacle>();
            if (obstacle != null)
                obstacle.data = obsData.obstacleData;
        }
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        // 3D 전략 게임용: 타일 위치를 월드 좌표로 변환
        return new Vector3(gridPos.x, 0, gridPos.y);
    }

    string GetPrefabName(UnitType type)
    {
        // Enum에 따라 프리팹 이름 매칭 (예시)
        switch (type)
        {
            case UnitType.Player_Zed: return "Player_Zed";
            case UnitType.Player_Lux: return "Player_Lux";
            case UnitType.Enemy_Slime: return "Enemy_Slime";
            case UnitType.Enemy_Ninja: return "Enemy_Ninja";
            default: return null;
        }
    }

    public PlayerUnit GetPlayer()
    {
        return player;
    }

    public List<EnemyUnit> GetEnemies()
    {
        return enemies;
    }
} 