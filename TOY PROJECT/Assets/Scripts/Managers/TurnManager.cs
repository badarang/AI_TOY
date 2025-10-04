using UnityEngine;
using System;
using System.Collections;
using Cysharp.Threading.Tasks;

public class TurnManager : MonoBehaviour
{
    // Events
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;

    // Game State
    public enum Turn { Player, Enemy }
    public Turn CurrentTurn { get; private set; }
    public enum PlayerTurnState { AwaitingUnitSelection, UnitSelected, PerformingAction, AwaitingSkillSubTarget, RewardPhase }
    public PlayerTurnState CurrentPlayerState { get; private set; }

    // Wave & Turn Limit System
    [Header("Wave & Turn System")]
    [SerializeField] private int waveTurnLimit = 5;
    private int currentWave = 1;
    private int turnInWave = 0;

    // Paused Skill State
    public SkillData PausedSkillData { get; set; }
    public SkillContext PausedSkillContext { get; set; }

    // Manager References
    private UIManager uiManager;
    private StageManager stageManager;
    private RewardManager rewardManager;
    private GridManager gridManager;

    void Start()
    {
        uiManager = Core.Instance.UIManager;
        stageManager = Core.Instance.StageManager;
        rewardManager = Core.Instance.RewardManager;
        gridManager = Core.Instance.GridManager;
        
        // 게임 시작 시 첫 웨이브를 시작하도록 호출 (예시)
        // 실제로는 게임 시작 로직을 관리하는 다른 매니저(예: GameManager)가 호출해야 합니다.
        StartFirstWave();
    }

    public void StartFirstWave()
    {
        currentWave = 1;
        turnInWave = 0;
        stageManager.SpawnWave(currentWave); // StageManager에 SpawnWave(waveIndex) 구현 필요
        StartPlayerTurn();
    }

    public void StartPlayerTurn()
    {
        CurrentTurn = Turn.Player;
        turnInWave++;
        SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        
        var player = stageManager.GetPlayer();
        if (player != null)
        {
            player.OnTurnStart();
        }

        OnPlayerTurnStart?.Invoke();
        uiManager.UpdateTurnUI(turnInWave, waveTurnLimit); // UIManager에 턴 UI 업데이트 구현 필요
    }

    public void StartEnemyTurn()
    {
        CurrentTurn = Turn.Enemy;
        SetPlayerState(PlayerTurnState.PerformingAction);

        var enemies = stageManager.GetEnemies();
        foreach (var enemy in enemies)
        {
            if (enemy != null) enemy.OnTurnStart();
        }

        OnEnemyTurnStart?.Invoke();
        StartCoroutine(EnemyTurnRoutine());
    }

    public void EndTurn()
    {
        if (CurrentPlayerState == PlayerTurnState.RewardPhase) return;

        if (CurrentTurn == Turn.Player)
        {
            StartEnemyTurn();
        }
        else // Enemy turn is ending
        {
            if (turnInWave >= waveTurnLimit)
            {
                Debug.Log("방송 시간 초과! 패널티 라운드에 돌입합니다!");
                currentWave++;
                stageManager.SpawnWave(currentWave); // 적 누적
                turnInWave = 0; // 턴 카운터 리셋
                // TODO: uiManager.ShowPenaltyAnnouncement();
            }
            StartPlayerTurn();
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return Core.Instance.EnemyAIManager.ExecuteEnemyTurns();
        if (CheckForWaveClear()) yield break;
        EndTurn();
    }

    private bool CheckForWaveClear()
    {
        if (stageManager.GetEnemies().Count == 0)
        {
            EnterRewardPhase();
            return true;
        }
        return false;
    }

    private void EnterRewardPhase()
    {
        Debug.Log($"라운드 {currentWave} 클리어! 스폰서 보급품이 도착했습니다!");
        SetPlayerState(PlayerTurnState.RewardPhase);
        turnInWave = 0;
        
        rewardManager.AddFans(100); // 예시: 웨이브 클리어 시 팬 100명 추가

        var rewards = rewardManager.GenerateRewards();
        uiManager.ShowRewardScreen(rewards); // UIManager에 보상 화면 표시 구현 필요
    }

    public void FinalizeRewardSelection()
    {
        // UI에서 보상 선택을 완료했을 때 호출됩니다.
        Debug.Log("보상 선택 완료. 다음 라운드를 준비합니다.");
        currentWave++;
        stageManager.SpawnWave(currentWave);
        StartPlayerTurn();
    }
    
    public void SetPlayerState(PlayerTurnState newState)
    {
        CurrentPlayerState = newState;
        Debug.Log($"Player state changed to: {newState}");
    }

    // --- Input Handling --- (기존 로직 유지 및 수정)

    public void HandleCellClick(Vector2Int cell)
    {
        if (CurrentPlayerState == PlayerTurnState.RewardPhase) return;

        switch (CurrentPlayerState)
        {
            case PlayerTurnState.AwaitingUnitSelection: HandleUnitSelection(cell); break;
            case PlayerTurnState.UnitSelected: HandleActionSelection(cell); break;
            case PlayerTurnState.AwaitingSkillSubTarget: HandleSkillSubTargetSelection(cell); break;
        }
    }

    public void CancelSelection()
    {
        if (CurrentPlayerState == PlayerTurnState.UnitSelected || CurrentPlayerState == PlayerTurnState.AwaitingSkillSubTarget)
        {
            CancelActionState();
        }
    }

    private void HandleUnitSelection(Vector2Int cell)
    {
        gridManager.ClearAllHighlights();
        UnitBase unit = gridManager.GetUnitAt(cell);
        if (unit != null && unit is PlayerUnit)
        {
            gridManager.TrySelectUnitAtCell(cell);
            UnitBase selectedUnit = gridManager.GetSelectedUnit();
            if (selectedUnit != null)
            {
                SetPlayerState(PlayerTurnState.UnitSelected);
                selectedUnit.ShowAvailableActions();
            }
        }
        uiManager.UpdateSkillPanel();
    }

    private async void HandleActionSelection(Vector2Int cell)
    {
        UnitBase selectedUnit = gridManager.GetSelectedUnit();
        if (selectedUnit == null) return;

        float skillDuration = 0f;
        UnitBase targetUnit = gridManager.GetTargetAt(cell);
        if (targetUnit != null)
        {
            for (int i = 0; i < selectedUnit.unitData.skills.Length; i++)
            {
                if (selectedUnit.unitData.skills[i].skillType == SkillType.Attack)
                {
                    skillDuration = selectedUnit.UseSkill(i, cell);
                    await PostActionUpdate(selectedUnit, skillDuration);
                    return;
                }
            }
            return;
        }

        if (gridManager.IsMovableTile(cell))
        {
            for (int i = 0; i < selectedUnit.unitData.skills.Length; i++)
            {
                if (selectedUnit.unitData.skills[i].skillType == SkillType.Move)
                {
                    skillDuration = selectedUnit.UseSkill(i, cell);
                    await PostActionUpdate(selectedUnit, skillDuration);
                    return;
                }
            }
            return;
        }

        UnitBase clickedUnit = gridManager.GetUnitAt(cell);
        if (clickedUnit != null && clickedUnit is PlayerUnit && clickedUnit != selectedUnit)
        {
            HandleUnitSelection(cell);
            return;
        }
        
        CancelActionState();
    }

    private async void HandleSkillSubTargetSelection(Vector2Int cell)
    {
        if (PausedSkillData == null || PausedSkillContext == null) return;
        UnitBase clickedUnit = gridManager.GetUnitAt(cell);
        if (clickedUnit != null)
        {
            gridManager.ClearAllHighlights();
            PausedSkillContext.SubTargetUnit = clickedUnit;
            await ExecuteSubSkills(PausedSkillData, PausedSkillContext);
        }
        else
        {
            CancelActionState();
        }
    }

    private async UniTask ExecuteSubSkills(SkillData skillData, SkillContext context)
    {
        if (skillData.subTargetBehaviors != null)
        {
            foreach (var behavior in skillData.subTargetBehaviors)
            {
                if (behavior != null) 
                {
                    float duration = behavior.Execute(context);
                    await UniTask.Delay(TimeSpan.FromSeconds(duration));
                }
            }
        }
        PausedSkillData = null;
        PausedSkillContext = null;
        SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        uiManager.UpdateSkillPanel();
    }

    private async UniTask PostActionUpdate(UnitBase unit, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));
        if (CheckForWaveClear()) return;
        if (unit == null) return;

        if (unit.ap > 0)
        {
            unit.ShowAvailableActions();
        }
        else
        {
            gridManager.ClearSelection();
            SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
            uiManager.UpdateSkillPanel();
        }
    }

    private void CancelActionState()
    {
        gridManager.ClearAllHighlights();
        PausedSkillData = null;
        PausedSkillContext = null;
        SetPlayerState(PlayerTurnState.AwaitingUnitSelection);
        uiManager.UpdateSkillPanel();
    }
}