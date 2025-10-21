using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, IManager
{
    public void BeforeInit() { }

    public void AfterInit() { }

    public void Dispose() { }

    [Header("Input Settings")]
    [SerializeField]
    private float clickDragThreshold = 5f;
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

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void UpdateTurnManagerReference()
    {
        if (Core.Instance != null)
        {
            turnManager = Core.Instance.TurnManager;
        }
    }

    void Start()
    {
        UpdateTurnManagerReference();
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
        if (turnManager == null)
        {
            UpdateTurnManagerReference();
        }

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
            if (TotalDragDistance < clickDragThreshold) // It's a click, not a drag
            {
                ProcessTurnClick();
            }
            OnDragEnd?.Invoke(CurrentInputPosition);
        }
    }

    private void ProcessTurnClick()
    {
        if (turnManager == null)
            return;

        if (IsPointerOverUI())
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(CurrentInputPosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            Vector2Int cell = new Vector2Int(
                Mathf.FloorToInt(hitPoint.x),
                Mathf.FloorToInt(hitPoint.z)
            );

            turnManager.HandleCellClick(cell);
        }
        else
        {
            turnManager.ClearSelection();
        }
    }

    private bool IsPointerOverUI()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
            return false;

        var eventData = new UnityEngine.EventSystems.PointerEventData(
            UnityEngine.EventSystems.EventSystem.current
        )
        {
            position = CurrentInputPosition,
        };

        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }
}
