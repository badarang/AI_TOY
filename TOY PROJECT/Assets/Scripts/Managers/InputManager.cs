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
    private GridManager gridManager;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        if (Core.Instance != null)
        {
            turnManager = Core.Instance.TurnManager;
            gridManager = Core.Instance.GridManager;
        }
    }

    void Start()
    {
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
            // Clicked outside the grid, deselect if a unit is selected
            if (turnManager.CurrentPlayerState == TurnManager.PlayerTurnState.UnitSelected)
            {
                gridManager.ClearSelection();
                turnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingUnitSelection);
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
        UnitBase unit = gridManager.GetUnitAt(cell);
        if (unit != null && unit is PlayerUnit)
        {
            gridManager.TrySelectUnitAtCell(cell); // Use the existing selection logic
            
            UnitBase selectedUnit = gridManager.GetSelectedUnit();
            if (selectedUnit != null && selectedUnit.unitData != null)
            {
                List<Vector2Int> movableTiles = gridManager.FindMovableTiles(cell, selectedUnit.unitData.movementPattern);
                gridManager.HighlightMovableTiles(movableTiles);
                turnManager.SetPlayerState(TurnManager.PlayerTurnState.UnitSelected);
            }
        }
        else
        {
            // Clicked on empty cell or enemy, clear selection
            gridManager.ClearSelection();
        }
    }

    private void HandleMovementSelection(Vector2Int cell)
    {
        UnitBase selectedUnit = gridManager.GetSelectedUnit();
        if (selectedUnit == null) return;

        if (gridManager.IsMovableTile(cell))
        {
            selectedUnit.Move(cell);
            gridManager.ClearMovableHighlights();
            turnManager.SetPlayerState(TurnManager.PlayerTurnState.PerformingAction);
            
            // For now, end turn after moving. This can be changed later.
            // Using a coroutine to wait a bit before ending the turn.
            StartCoroutine(EndTurnAfterDelay(1.0f));
        }
        else
        {
            // Clicked somewhere else (not a valid move tile)
            // If the click is on the same unit, do nothing. If it's another player unit, switch selection.
            UnitBase clickedUnit = gridManager.GetUnitAt(cell);
            if (clickedUnit != null && clickedUnit is PlayerUnit && clickedUnit != selectedUnit)
            {
                // Switch selection to another player unit
                gridManager.ClearSelection();
                HandleUnitSelection(cell);
            }
            else
            {
                // Clicked on empty space, enemy, or the same unit again -> Deselect
                gridManager.ClearSelection();
                turnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingUnitSelection);
            }
        }
    }

    private System.Collections.IEnumerator EndTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        turnManager.EndTurn();
    }
}