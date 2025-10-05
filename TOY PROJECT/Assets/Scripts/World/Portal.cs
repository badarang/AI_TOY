using UnityEngine;

/// <summary>
/// 다음 스테이지로 이동하는 포탈의 동작을 정의합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    public StageType stageType { get; private set; }

    private bool _isInitialized = false;

    /// <summary>
    /// StageManager가 포탈을 생성할 때 호출하여 포탈의 목적지를 설정합니다.
    /// </summary>
    public void Initialize(StageType type)
    {
        stageType = type;
        // TODO: 타입에 따라 포탈의 색상이나 파티클 등 외형을 변경하는 로직 추가
        _isInitialized = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isInitialized) return;

        // 플레이어가 포탈에 닿았는지 확인합니다. (플레이어 오브젝트에 "Player" 태그가 있어야 합니다)
        if (other.CompareTag("Player"))
        {
            Debug.Log($"플레이어가 포탈에 진입: {stageType}");

            // GameManager에 플레이어의 선택을 알립니다.
            Core.Instance.GameManager.OnPortalSelected(stageType);
            
            // 중복 입력을 막기 위해 즉시 비활성화합니다.
            gameObject.SetActive(false); 
        }
    }
}
