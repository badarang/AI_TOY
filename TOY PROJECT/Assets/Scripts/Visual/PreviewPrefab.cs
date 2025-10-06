using UnityEngine;

public class PreviewPrefab : MonoBehaviour
{
    [Header("Child References")]
    [SerializeField] private Transform centerTransform;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform textTransform;
    
    private TextMesh textMesh;
    
    void Awake()
    {
        if (textTransform != null)
        {
            textMesh = textTransform.GetComponent<TextMesh>();
        }
    }
    
    public void Init(Vector3 startPos, Vector3 endPos, PreviewActionType actionType, Color color)
    {
        transform.position = Vector3.zero;
        
        SetupCenter(startPos, color);
        SetupLine(startPos, endPos, color);
        SetupText(startPos, endPos, actionType);
    }
    
    private void SetupCenter(Vector3 position, Color color)
    {
        if (centerTransform == null) return;
        
        centerTransform.position = position + Vector3.up * 0.5f;
        
        var renderer = centerTransform.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
    
    private void SetupLine(Vector3 startPos, Vector3 endPos, Color color)
    {
        if (lineRenderer == null) return;
        
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        
        Vector3 adjustedStart = startPos + Vector3.up * 0.5f;
        Vector3 adjustedEnd = endPos + Vector3.up * 0.5f;
        
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, adjustedStart);
        lineRenderer.SetPosition(1, adjustedEnd);
    }
    
    private void SetupText(Vector3 startPos, Vector3 endPos, PreviewActionType actionType)
    {
        if (textTransform == null || textMesh == null) return;
        
        Vector3 midPoint = (startPos + endPos) / 2f + Vector3.up * 1.5f;
        textTransform.position = midPoint;
        
        textMesh.text = GetActionText(actionType);
    }
    
    private string GetActionText(PreviewActionType actionType)
    {
        switch (actionType)
        {
            case PreviewActionType.Attack:
                return "ATTACK!";
            case PreviewActionType.Move:
                return "MOVE";
            case PreviewActionType.Skill:
                return "SKILL";
            case PreviewActionType.Wait:
                return "WAIT";
            default:
                return "?";
        }
    }
    
    void Update()
    {
        if (textTransform != null && Camera.main != null)
        {
            textTransform.LookAt(Camera.main.transform);
            textTransform.Rotate(0, 180, 0);
        }
    }
    
    public void ResetVisual()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }
}
