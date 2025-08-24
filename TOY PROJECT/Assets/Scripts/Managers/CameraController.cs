using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float distance = 10f;
    public float height = 10f;
    public float dragSensitivity = 0.4f;
    public float snapAngle = 90f;
    public float snapThreshold = 45f;
    public float returnDuration = 0.4f;
    public float punchStrength = 10f;

    private float currentYAngle = 0f;
    private bool isDragging = false;
    private Vector3 lastMousePos;
    private float startYAngle;
    private float dragDelta;

    // New Input System 관련 변수들
    private PlayerInputActions inputActions;
    private Camera mainCamera;

    void Awake()
    {
        // PlayerInputActions 인스턴스 생성
        inputActions = new PlayerInputActions();
        mainCamera = Camera.main;
        
        if (target == null)
        {
            // target이 설정되지 않은 경우 자동으로 생성
            GameObject targetObj = new GameObject("CameraTarget");
            target = targetObj.transform;
            
            // 카메라가 있는 위치를 기준으로 target 위치 설정
            if (mainCamera != null)
            {
                target.position = mainCamera.transform.position + mainCamera.transform.forward * 5f;
            }
            else
            {
                target.position = Vector3.zero;
            }
        }
        
        UpdateCameraPosition(currentYAngle);
    }

    void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Click.started += OnClickStarted;
            inputActions.Gameplay.Click.performed += OnClickPerformed;
            inputActions.Gameplay.Click.canceled += OnClickCanceled;
            inputActions.Enable();
        }
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Click.started -= OnClickStarted;
            inputActions.Gameplay.Click.performed -= OnClickPerformed;
            inputActions.Gameplay.Click.canceled -= OnClickCanceled;
            inputActions.Disable();
        }
    }

    void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
        }
    }

    // 클릭 시작
    private void OnClickStarted(InputAction.CallbackContext context)
    {
        if (!IsPointerOverGridOrUI())
        {
            isDragging = true;
            lastMousePos = GetInputPosition();
            startYAngle = currentYAngle;
            dragDelta = 0f;
        }
    }

    // 클릭 중 (드래그)
    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        if (isDragging)
        {
            Vector2 currentPos = GetInputPosition();
            Vector2 delta = currentPos - (Vector2)lastMousePos;
            dragDelta += delta.x * dragSensitivity;
            currentYAngle = startYAngle + dragDelta;
            UpdateCameraPosition(currentYAngle);
            lastMousePos = currentPos;
        }
    }

    // 클릭 끝
    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (isDragging)
        {
            isDragging = false;
            float finalDelta = currentYAngle - startYAngle;

            // 45도 미만이면 원래 각도로 복귀 (쫄깃하게)
            if (Mathf.Abs(finalDelta) < snapThreshold)
            {
                AnimateToAngle(startYAngle);
            }
            else // 45도 이상이면 90도 단위로 snap (쫄깃하게)
            {
                float snappedY = Mathf.Round(currentYAngle / snapAngle) * snapAngle;
                AnimateToAngle(snappedY);
            }
        }
    }

    // 크로스 플랫폼 입력 위치 가져오기
    private Vector2 GetInputPosition()
    {
        // New Input System 사용
        if (inputActions != null)
        {
            Vector2 pointValue = inputActions.Gameplay.Point.ReadValue<Vector2>();
            if (pointValue != Vector2.zero)
            {
                return pointValue;
            }
        }

        // 폴백: 기존 Input.mousePosition 사용
        return Input.mousePosition;
    }

    void UpdateCameraPosition(float yAngle)
    {
        // 각도를 정규화 (0-360 범위로)
        yAngle = yAngle % 360f;
        if (yAngle < 0) yAngle += 360f;
        
        float rad = yAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * distance;
        offset.y = height;
        
        transform.position = target.position + offset;
        transform.LookAt(target.position);
        
        currentYAngle = yAngle;
    }

    void AnimateToAngle(float targetAngle)
    {
        // DOTween을 사용해서 각도를 부드럽게 변경
        float startAngle = currentYAngle;
        
        // 최단 경로로 회전하도록 각도 조정
        float angleDiff = Mathf.DeltaAngle(startAngle, targetAngle);
        float finalTargetAngle = startAngle + angleDiff;
        
        Sequence seq = DOTween.Sequence();
        
        // 부드러운 회전 애니메이션
        seq.Append(DOTween.To(() => currentYAngle, 
                             x => UpdateCameraPosition(x), 
                             finalTargetAngle, 
                             returnDuration)
                          .SetEase(Ease.OutBack));
        
        // Punch 효과는 target을 기준으로 적용
        seq.Append(target.DOPunchRotation(Vector3.up * punchStrength, 0.2f, 8, 1)
                        .OnComplete(() => {
                            // Punch 효과 후 target 회전 초기화
                            target.rotation = Quaternion.identity;
                        }));
    }

    // 그리드/유닛/기타 UI 위 클릭인지 체크 (임시: 항상 false)
    bool IsPointerOverGridOrUI()
    {
        // 실제 구현 시 Raycast 등으로 그리드/유닛/버튼 위 클릭인지 체크
        return false;
    }
}