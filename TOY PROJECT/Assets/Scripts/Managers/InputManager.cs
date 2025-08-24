using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    public float dragSensitivity = 1f;

    public event Action<Vector2> OnDragStart;
    public event Action<Vector2> OnDrag;
    public event Action<Vector2> OnDragEnd;

    public event Action<Vector2> OnClick;

    public bool IsDragging { get; private set; }
    public Vector2 DragDelta { get; private set; }
    public Vector2 CurrentInputPosition { get; private set; }
    public Vector2 DragStartPosition { get; private set; }
    public float TotalDragDistance { get; private set; }

    private PlayerInputActions inputActions;
    private bool wasClickingLastFrame = false;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void Start()
    {
        SetupInputEvents();
    }

    void SetupInputEvents()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Click.started -= OnClickStarted;
            inputActions.Gameplay.Click.canceled -= OnClickCanceled;

            inputActions.Gameplay.Click.started += OnClickStarted;
            inputActions.Gameplay.Click.canceled += OnClickCanceled;

            inputActions.Enable();
            Debug.Log("InputManager: Input Actions enabled and events registered");
        }
        else
        {
            Debug.LogError("InputManager: InputActions is null!");
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

            if (TotalDragDistance < 5f)
            {
                OnClick?.Invoke(CurrentInputPosition);
            }

            OnDragEnd?.Invoke(CurrentInputPosition);
        }
    }

    public Vector2 GetScreenToWorldPoint(Camera camera)
    {
        if (camera != null)
        {
            return camera.ScreenToWorldPoint(new Vector3(CurrentInputPosition.x, CurrentInputPosition.y, camera.nearClipPlane));
        }
        return Vector2.zero;
    }

    public bool IsInputOverUI()
    {
        return false;
    }

    [ContextMenu("Debug Input System")]
    void DebugInputSystem()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActions is null!");
            return;
        }

        Debug.Log($"Gameplay action map enabled: {inputActions.Gameplay.enabled}");
        Debug.Log($"Click action enabled: {inputActions.Gameplay.Click.enabled}");
        Debug.Log($"Current Input Position: {CurrentInputPosition}");
        Debug.Log($"Is Dragging: {IsDragging}");
    }
}