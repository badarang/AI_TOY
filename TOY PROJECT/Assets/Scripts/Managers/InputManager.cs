using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public TurnManager turnManager;
    public PlayerUnit playerUnit;

    // New Input System 관련 변수들
    private PlayerInputActions inputActions;

    void Awake()
    {
        // PlayerInputActions 인스턴스 생성
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Click.performed += OnSpacePressed;
            inputActions.Enable();
        }
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Click.performed -= OnSpacePressed;
            inputActions.Disable();
        }
    }

    // 입력 위치 가져오기 (필요시 사용)
    public Vector2 GetInputPosition()
    {
        if (inputActions != null)
        {
            Vector2 pointValue = inputActions.Gameplay.Point.ReadValue<Vector2>();
            if (pointValue != Vector2.zero)
            {
                return pointValue;
            }
        }
        return Input.mousePosition;
    }

    void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
        }
    }

    // 스페이스바나 다른 입력으로 턴 종료
    private void OnSpacePressed(InputAction.CallbackContext context)
    {
        if (turnManager.CurrentTurn != TurnManager.TurnState.Player) return;
        
        // 실제로는 이동/스킬 등 행동 처리 후에 EndTurn 호출
        turnManager.EndTurn();
    }

    public void ProcessInput()
    {
        // 입력 처리, PlayerUnit에 명령 전달
    }
} 