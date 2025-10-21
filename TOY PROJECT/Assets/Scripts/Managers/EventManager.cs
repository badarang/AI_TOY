using System;
using UnityEngine;

public class EventManager : MonoBehaviour, IManager
{
    public static EventManager Instance { get; private set; }

    // 유닛이 죽을 때 발생하는 이벤트
    public event Action<UnitBase> OnUnitDied;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[EventManager] Duplicate instance found, destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void BeforeInit()
    {
        // 초기화 전 필요한 작업
    }

    public void AfterInit()
    {
        // 초기화 후 필요한 작업
    }

    /// <summary>
    /// 유닛이 죽었을 때 호출되는 메서드
    /// </summary>
    public void TriggerUnitDied(UnitBase unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("[EventManager] TriggerUnitDied called with null unit!");
            return;
        }

        DebugPrinter.LogColor(LogType.Event, $"[EventManager] Unit {unit.name} died event triggered.");
        OnUnitDied?.Invoke(unit);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
