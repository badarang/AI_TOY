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
            if (turnManager.CurrentPlayerState == TurnManager.PlayerTurnState.UnitSelected)
            {
                GridManager.Instance.ClearSelection();
                turnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingUnitSelection);
                uiManager.UpdateSkillPanel(); // Update UI on deselect
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
        }
    }

    private void HandleUnitSelection(Vector2Int cell)
    {
        GridManager.Instance.ClearSelection(); // Clear previous selection first
        
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
                    List<Vector2Int> movableTiles = GridManager.Instance.FindMovableTiles(cell, selectedUnit.unitData.movementPattern);
                    GridManager.Instance.HighlightMovableTiles(movableTiles);
                }
            }
        }
        // Whether a unit was selected or not, update the panel
        uiManager.UpdateSkillPanel();
    }

    private void HandleMovementSelection(Vector2Int cell)
    {
        UnitBase selectedUnit = GridManager.Instance.GetSelectedUnit();
        if (selectedUnit == null) return;

        if (GridManager.Instance.IsMovableTile(cell))
        {
            selectedUnit.Move(cell);
            GridManager.Instance.ClearMovableHighlights();
            turnManager.SetPlayerState(TurnManager.PlayerTurnState.PerformingAction);
            uiManager.UpdateSkillPanel(); // Clear panel after move
            
            StartCoroutine(EndTurnAfterDelay(1.0f));
        }
        else
        {
            UnitBase clickedUnit = GridManager.Instance.GetUnitAt(cell);
            if (clickedUnit != null && clickedUnit is PlayerUnit && clickedUnit != selectedUnit)
            {
                // Switch selection to another player unit
                HandleUnitSelection(cell); // This will clear old selection and start new one
            }
            else
            {
                // Clicked on empty space, enemy, or the same unit again -> Deselect
                GridManager.Instance.ClearSelection();
                turnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingUnitSelection);
                uiManager.UpdateSkillPanel(); // Update UI on deselect
            }
        }
    }

    private System.Collections.IEnumerator EndTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        turnManager.EndTurn();
    }
}