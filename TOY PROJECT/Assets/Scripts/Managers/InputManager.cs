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
                selectedUnit.ShowAvailableActions();
            }
        }
        uiManager.UpdateSkillPanel();
    }

    private void HandleMovementSelection(Vector2Int cell)
    {
        UnitBase selectedUnit = GridManager.Instance.GetSelectedUnit();
        if (selectedUnit == null) return;

        // 1. Check for Attack Target
        UnitBase targetUnit = GridManager.Instance.GetTargetAt(cell);
        if (targetUnit != null)
        {
            // Find first attack skill and use it
            for (int i = 0; i < selectedUnit.unitData.skills.Length; i++)
            {
                if (selectedUnit.unitData.skills[i].range > 0)
                {
                    selectedUnit.UseSkill(i, cell);
                    GridManager.Instance.ClearSelection();
                    turnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingUnitSelection);
                    uiManager.UpdateSkillPanel();
                    return; // Action taken
                }
            }
            return; // No attack skill found
        }

        // 2. Check for Move
        if (GridManager.Instance.IsMovableTile(cell))
        {
            // Find the first move skill and use it
            for (int i = 0; i < selectedUnit.unitData.skills.Length; i++)
            {
                var skill = selectedUnit.unitData.skills[i];
                if (skill.movementPattern != null && skill.movementPattern.Count > 0)
                {
                    selectedUnit.UseSkill(i, cell);
                    GridManager.Instance.ClearSelection();
                    turnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingUnitSelection);
                    uiManager.UpdateSkillPanel();
                    return; // Action taken
                }
            }
            return; // No move skill found
        }

        // 3. Check for clicking another friendly unit to switch selection
        UnitBase clickedUnit = GridManager.Instance.GetUnitAt(cell);
        if (clickedUnit != null && clickedUnit is PlayerUnit && clickedUnit != selectedUnit)
        {
            HandleUnitSelection(cell); // Reselect to the new unit
            return;
        }
        
        // 4. If nothing else, cancel selection
        CancelSkillState();
    }

    private void HandleSkillSubTargetSelection(Vector2Int cell)
    {
        var pausedSkillData = turnManager.PausedSkillData;
        var context = turnManager.PausedSkillContext;

        if (pausedSkillData == null || context == null) return;

        UnitBase clickedUnit = GridManager.Instance.GetUnitAt(cell);

        // TODO: Add validation to check if the clicked unit is a valid sub-target
        if (clickedUnit != null)
        {
            GridManager.Instance.ClearAllHighlights();
            context.SubTargetUnit = clickedUnit;
            StartCoroutine(ExecuteSubSkillsCoroutine(pausedSkillData, context));
        }
        else
        {
            CancelSkillState();
        }
    }

    private System.Collections.IEnumerator ExecuteSubSkillsCoroutine(SkillData skillData, SkillContext context)
    {
        if (skillData.subTargetBehaviors != null)
        {
            foreach (var behavior in skillData.subTargetBehaviors)
            {
                if (behavior != null) 
                {
                    behavior.Execute(context);
                    yield return new WaitForSeconds(0.5f); // Wait half a second between each sub-skill
                }
            }
        }

        // Clean up and reset state after all sub-skills are done
        turnManager.PausedSkill = null;
        turnManager.PausedSkillData = null;
        turnManager.PausedSkillContext = null;
        turnManager.SetPlayerState(TurnManager.PlayerTurnState.AwaitingUnitSelection);
        uiManager.UpdateSkillPanel();
    }

    private void CancelSkillState()
    {
        Debug.Log("Selection cancelled.");
        GridManager.Instance.ClearAllHighlights();
        
        turnManager.PausedSkill = null;
        turnManager.PausedSkillData = null;
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
