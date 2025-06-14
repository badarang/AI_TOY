using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    public Transform target; // 회전 중심(맵 중앙)
    public float distance = 10f;
    public float height = 10f;
    public float dragSensitivity = 0.4f;
    public float snapAngle = 90f;
    public float snapThreshold = 45f;
    public float returnDuration = 0.4f;
    public float punchStrength = 10f;

    private bool isDragging = false;
    private Vector3 lastMousePos;
    private float currentYAngle = 0f; // 현재 Y 회전 각도를 직접 관리
    private float startYAngle;
    private float dragDelta;

    void Start()
    {
        if (target == null)
            target = new GameObject("CameraTarget").transform;
        
        // 초기 각도 설정
        currentYAngle = 0f;
        UpdateCameraPosition(currentYAngle);
    }

    void Update()
    {
        // 마우스 클릭(좌클릭) 시작
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverGridOrUI())
            {
                isDragging = true;
                lastMousePos = Input.mousePosition;
                startYAngle = currentYAngle; // eulerAngles 대신 직접 관리하는 각도 사용
                dragDelta = 0f;
            }
        }

        // 드래그 중
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            dragDelta += delta.x * dragSensitivity;
            currentYAngle = startYAngle + dragDelta;
            UpdateCameraPosition(currentYAngle);
            lastMousePos = Input.mousePosition;
        }

        // 드래그 끝
        if (isDragging && Input.GetMouseButtonUp(0))
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