using UnityEngine;

public class InputManager : MonoBehaviour
{
    public TurnManager turnManager;
    public PlayerUnit playerUnit;

    void Update()
    {
        if (turnManager.CurrentTurn != TurnManager.TurnState.Player) return;
        // 예시: 스페이스바를 누르면 턴 종료
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 실제로는 이동/스킬 등 행동 처리 후에 EndTurn 호출
            turnManager.EndTurn();
        }
    }

    public void ProcessInput()
    {
        // 입력 처리, PlayerUnit에 명령 전달
    }
} 