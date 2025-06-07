using UnityEngine;
using System;

public class TurnManager : MonoBehaviour
{
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;

    public void StartPlayerTurn()
    {
        OnPlayerTurnStart?.Invoke();
        // 행동력 리셋 등
    }

    public void StartEnemyTurn()
    {
        OnEnemyTurnStart?.Invoke();
        // 적 AI 행동 시작
    }

    public void EndTurn()
    {
        // 턴 전환
    }
} 