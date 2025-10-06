using UnityEngine;
using TMPro;

public class EnemyPreviewVisual : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private GameObject textObject;
    private TextMeshPro textMesh;
    
    public void Initialize(Vector3 startPos, Vector3 endPos, PreviewActionType actionType, Color color)
    {
        transform.position = startPos;
        
        CreateArrow(startPos, endPos, color);
        CreateLabel(startPos, endPos, actionType);
    }
    
    private void CreateArrow(Vector3 startPos, Vector3 endPos, Color color)
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = color;
        lineRenderer.material = lineMat;
        
        Vector3 adjustedStart = startPos + Vector3.up * 0.5f;
        Vector3 adjustedEnd = endPos + Vector3.up * 0.5f;
        
        lineRenderer.SetPosition(0, adjustedStart);
        lineRenderer.SetPosition(1, adjustedEnd);
        
        CreateArrowHead(adjustedEnd, adjustedStart, color);
    }
    
    private void CreateArrowHead(Vector3 endPos, Vector3 startPos, Color color)
    {
        GameObject arrowHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrowHead.transform.SetParent(transform);
        arrowHead.transform.position = endPos;
        arrowHead.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        Vector3 direction = (endPos - startPos).normalized;
        arrowHead.transform.rotation = Quaternion.LookRotation(direction);
        
        var renderer = arrowHead.GetComponent<Renderer>();
        Material headMat = new Material(Shader.Find("Sprites/Default"));
        headMat.color = color;
        renderer.material = headMat;
        
        Destroy(arrowHead.GetComponent<Collider>());
    }
    
    private void CreateLabel(Vector3 startPos, Vector3 endPos, PreviewActionType actionType)
    {
        textObject = new GameObject("PreviewLabel");
        textObject.transform.SetParent(transform);
        
        Vector3 midPoint = (startPos + endPos) / 2f + Vector3.up * 1f;
        textObject.transform.position = midPoint;
        
        textMesh = textObject.AddComponent<TextMeshPro>();
        textMesh.text = GetActionText(actionType);
        textMesh.fontSize = 3;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;
        
        textMesh.outlineWidth = 0.2f;
        textMesh.outlineColor = Color.black;
        
        textObject.transform.rotation = Quaternion.Euler(90, 0, 0);
    }
    
    private string GetActionText(PreviewActionType actionType)
    {
        switch (actionType)
        {
            case PreviewActionType.Attack:
                return "← ATTACK";
            case PreviewActionType.Move:
                return "→ MOVE";
            case PreviewActionType.Skill:
                return "★ SKILL";
            case PreviewActionType.Wait:
                return "○ WAIT";
            default:
                return "?";
        }
    }
    
    void Update()
    {
        if (textObject != null && Camera.main != null)
        {
            textObject.transform.LookAt(Camera.main.transform);
            textObject.transform.Rotate(0, 180, 0);
        }
    }
}
