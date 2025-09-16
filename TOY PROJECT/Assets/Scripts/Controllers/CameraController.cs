using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;
    public float distance = 10f;
    public float height = 10f;
    public float orthographicSize = 10f;

    private Camera mainCamera;
    
    void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = orthographicSize;
        }
        
        SetupTarget();
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            mainCamera.orthographicSize = orthographicSize;
        }

        if (target != null)
        {
            UpdateCameraPosition();
        }
    }
    
    private void SetupTarget()
    {
        if (target == null)
        {
            GameObject targetObj = new GameObject("CameraTarget");
            target = targetObj.transform;
            target.position = Vector3.zero;
        }
    }
    
    void UpdateCameraPosition()
    {
        float yAngle = 45f; 

        float rad = yAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * distance;
        offset.y = height;

        transform.position = target.position + offset;
        transform.LookAt(target.position);
    }
}