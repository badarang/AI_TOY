using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    public float dragSensitivity = 1f;

    public event Action<Vector2> OnDragStart;
    public event Action<Vector2> OnDrag;
    public event Action<Vector2> OnDragEnd;

    public bool IsDragging { get; private set; }
    public Vector2 DragDelta { get; private set; }
    public Vector2 CurrentInputPosition { get; private set; }
    public Vector2 DragStartPosition { get; private set; }
    public float TotalDragDistance { get; private set; }

    private PlayerInputActions inputActions;
    
    private TurnManager turnManager;
    private UIManager uiManager;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void Start()
    {
        if (Core.Instance != null)
        {
            turnManager = Core.Instance.TurnManager;
            uiManager = Core.Instance.UIManager;
        }
        SetupInputEvents();
    }

    void SetupInputEvents()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Click.started += OnClickStarted;
            inputActions.Gameplay.Click.canceled += OnClickCanceled;
            inputActions.Enable();
        }
    }

    void OnEnable()
    {
        if (inputActions != null && !inputActions.Gameplay.enabled)
        {
            inputActions.Enable();
        }
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Click.started -= OnClickStarted;
            inputActions.Gameplay.Click.canceled -= OnClickCanceled;
            inputActions.Dispose();
        }
    }

    void Update()
    {
        UpdateCurrentInputPosition();

        if (IsDragging)
        {
            ProcessDrag();
        }
    }

    private void UpdateCurrentInputPosition()
    {
        if (inputActions != null)
        {
            CurrentInputPosition = inputActions.Gameplay.Point.ReadValue<Vector2>();
        }
    }

    private void ProcessDrag()
    {
        Vector2 dragValue = inputActions.Gameplay.Drag.ReadValue<Vector2>();

        if (dragValue != Vector2.zero)
        {
            DragDelta = dragValue * dragSensitivity;
            TotalDragDistance += DragDelta.magnitude;
            OnDrag?.Invoke(DragDelta);
        }
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        IsDragging = true;
        DragStartPosition = CurrentInputPosition;
        TotalDragDistance = 0f;
        OnDragStart?.Invoke(DragStartPosition);
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (IsDragging)
        {
            IsDragging = false;
            if (TotalDragDistance < 5f) // It's a click, not a drag
            {
                ProcessTurnClick();
            }
            OnDragEnd?.Invoke(CurrentInputPosition);
        }
    }

    private void ProcessTurnClick()
    {
        if (turnManager.CurrentTurn != TurnManager.Turn.Player) return;

        Ray ray = Camera.main.ScreenPointToRay(CurrentInputPosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out float distance))
        { 
            if (turnManager.CurrentPlayerState == TurnManager.PlayerTurnState.UnitSelected || turnManager.CurrentPlayerState == TurnManager.PlayerTurnState.AwaitingSkillSubTarget)
            {
                CancelSkillState();
            }
            return;
        }
        
        Vector3 hitPoint = ray.GetPoint(distance);
        Vector2Int cell = new Vector2Int(Mathf.FloorToInt(hitPoint.x), Mathf.FloorToInt(hitPoint.z));

        switch (turnManager.CurrentPlayerState)
        {
            case TurnManager.PlayerTurnState.AwaitingUnitSelection:
                HandleUnitSelection(cell);
                break;

            case TurnManager.PlayerTurnState.UnitSelected:
                HandleMovementSelection(cell);
                break;
            
            case TurnManager.PlayerTurnState.AwaitingSkillSubTarget:
                HandleSkillSubTargetSelection(cell);
                break;
        }
    }

    private void HandleUnitSelection(Vector2Int cell)
    {
        GridManager.Instance.ClearAllHighlights();
        
        UnitBase unit = GridManager.Instance.GetUnitAt(cell);
        if (unit != null && unit is PlayerUnit)
        {
            GridManager.Instance.TrySelectUnitAtCell(cell);
            
            UnitBase selectedUnit = GridManager.Instance.GetSelectedUnit();
            if (selectedUnit != null)
            {
                turnManager.SetPlayerState(TurnManager.PlayerTurnState.UnitSelected);
                if (selectedUnit.unitData != null)
                {
                    // TODO: 향후 스킬 사거리 표시 등과 통합 필요
                    List<Vector2Int> movableTiles = GridManager.Instance.FindMovableTiles(cell, selectedUnit.unitData.movementPattern);
                    GridManager.Instance.HighlightMovableTiles(movableTiles);
                }
            }
        }
        uiManager.UpdateSkillPanel();
    }

    private void HandleMovementSelection(Vector2Int cell)
    {
        UnitBase selectedUnit = GridManager.Instance.GetSelectedUnit();
        if (selectedUnit == null) return;

        // 이동 가능 범위 클릭 시
        if (GridManager.Instance.IsMovableTile(cell))
        {
            GridManager.Instance.ClearAllHighlights();
            
            // 임시 로직: 0번 스킬이 있으면 스킬 사용, 없으면 기본 이동
            var playerUnit = selectedUnit as PlayerUnit;
            if (playerUnit != null && playerUnit.HasSkills)
            {
                // 0번 스킬 사용
                playerUnit.UseSkill(0, cell);
            }
            else
            {
                // 스킬이 없으면 기본 이동
                selectedUnit.Move(cell);
                turnManager.SetPlayerState(TurnManager.PlayerTurnState.PerformingAction);
                StartCoroutine(EndTurnAfterDelay(1.0f));
            }
            uiManager.UpdateSkillPanel();
        }
        else
        {
            UnitBase clickedUnit = GridManager.Instance.GetUnitAt(cell);
            if (clickedUnit != null && clickedUnit is PlayerUnit && clickedUnit != selectedUnit)
            {
                HandleUnitSelection(cell);
            }
            else
            {
                CancelSkillState();
            }
        }
    }

    private void HandleSkillSubTargetSelection(Vector2Int cell)
    {
        var pausedSkill = turnManager.PausedSkill;
        var context = turnManager.PausedSkillContext;

        if (pausedSkill == null || context == null) return;

        UnitBase clickedUnit = GridManager.Instance.GetUnitAt(cell);

        if (clickedUnit != null && context.HighlightedTargets.Contains(clickedUnit))
        {
            GridManager.Instance.ClearAllHighlights();
            pausedSkill.ActivateSubTarget(clickedUnit);
        }
        else
        {
            CancelSkillState();
        }
    }

    private void CancelSkillState()
    {
        Debug.Log("Selection cancelled.");
        GridManager.Instance.ClearAllHighlights();
        
        turnManager.PausedSkill = null;
        turnManager.PausedSkillContext = null;
        turnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingUnitSelection);
        uiManager.UpdateSkillPanel();
    }

    private System.Collections.IEnumerator EndTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        turnManager.EndTurn();
    }
}
