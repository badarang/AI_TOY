using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;
    public float distance = 10f;
    public float height = 10f;
    
    [Header("Rotation Settings")]
    public float rotationSensitivity = 0.4f;
    public float snapAngle = 90f;
    public float snapThreshold = 45f;
    
    [Header("Animation Settings")]
    public float returnDuration = 0.4f;
    public float punchStrength = 10f;

    private float currentYAngle = 0f;
    private float startYAngle;
    private float dragDelta;
    private Camera mainCamera;
    
    void Awake()
    {
        mainCamera = Camera.main;
        SetupTarget();
        UpdateCameraPosition(currentYAngle);
    }
    
    void Start()
    {
        if (Core.Instance?.InputManager != null)
        {
            Core.Instance.InputManager.OnDragStart += HandleDragStart;
            Core.Instance.InputManager.OnDrag += HandleDrag;
            Core.Instance.InputManager.OnDragEnd += HandleDragEnd;
        }
    }
    
    void OnDestroy()
    {
        if (Core.Instance?.InputManager != null)
        {
            Core.Instance.InputManager.OnDragStart -= HandleDragStart;
            Core.Instance.InputManager.OnDrag -= HandleDrag;
            Core.Instance.InputManager.OnDragEnd -= HandleDragEnd;
        }
    }
    
    private void SetupTarget()
    {
        if (target == null)
        {
            GameObject targetObj = new GameObject("CameraTarget");
            target = targetObj.transform;

            if (mainCamera != null)
                target.position = mainCamera.transform.position + mainCamera.transform.forward * 5f;
            else
                target.position = Vector3.zero;
        }
    }
    
    private void HandleDragStart(Vector2 startPosition)
    {
        DebugPrinter.DebugColor(DebugType.Input, $"Drag started at {startPosition}");
        startYAngle = currentYAngle;
        dragDelta = 0f;
    }
    
    private void HandleDrag(Vector2 deltaMovement)
    {
        dragDelta += deltaMovement.x * rotationSensitivity;
        currentYAngle = startYAngle + dragDelta;
        UpdateCameraPosition(currentYAngle);
    }
    
    private void HandleDragEnd(Vector2 endPosition)
    {
        DebugPrinter.DebugColor(DebugType.Input, $"Drag ended at {endPosition}");

        float finalDelta = currentYAngle - startYAngle;

        if (Mathf.Abs(finalDelta) < snapThreshold)
        {
            AnimateToAngle(startYAngle);
        }
        else
        {
            float snappedY = Mathf.Round(currentYAngle / snapAngle) * snapAngle;
            AnimateToAngle(snappedY);
        }
    }
    
    void UpdateCameraPosition(float yAngle)
    {
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
        float startAngle = currentYAngle;
        float angleDiff = Mathf.DeltaAngle(startAngle, targetAngle);
        float finalTargetAngle = startAngle + angleDiff;

        Sequence seq = DOTween.Sequence();

        seq.Append(DOTween.To(() => currentYAngle,
                             x => UpdateCameraPosition(x),
                             finalTargetAngle,
                             returnDuration)
                          .SetEase(Ease.OutBack));

        seq.Append(target.DOPunchRotation(Vector3.up * punchStrength, 0.2f, 8, 1)
                        .OnComplete(() =>
                        {
                            target.rotation = Quaternion.identity;
                        }));
    }
    
    // 외부에서 카메라 각도를 직접 설정할 때 사용
    public void SetCameraAngle(float angle)
    {
        currentYAngle = angle;
        UpdateCameraPosition(currentYAngle);
    }
    
    public void SetCameraAngleAnimated(float angle, float duration = 0.5f)
    {
        DOTween.To(() => currentYAngle,
                   x => UpdateCameraPosition(x),
                   angle,
                   duration)
                .SetEase(Ease.OutQuart);
    }
}