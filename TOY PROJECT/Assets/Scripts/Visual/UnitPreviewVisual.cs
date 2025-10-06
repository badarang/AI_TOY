using UnityEngine;

public class UnitPreviewVisual : MonoBehaviour
{

private bool isInitialized = false;
    
    public void ResetVisual()
    {
        if (lineRenderer != null)
        {
            Destroy(lineRenderer);
            lineRenderer = null;
        }
        
        if (textObject != null)
        {
            Destroy(textObject);
            textObject = null;
        }
        
        foreach (Transform child in transform)
        {
            if (child.name.Contains("ArrowHead"))
                Destroy(child.gameObject);
        }
        
        isInitialized = false;
    }

    private LineRenderer lineRenderer;
    private GameObject textObject;
    private TextMesh textMesh;
    
public void Initialize(Vector3 startPos, Vector3 endPos, PreviewActionType actionType, Color color)
    {
        if (isInitialized)
            ResetVisual();
        
        transform.position = startPos;
        
        CreateArrow(startPos, endPos, color);
        CreateLabel(startPos, endPos, actionType);
        
        isInitialized = true;
    }
    
    private void CreateArrow(Vector3 startPos, Vector3 endPos, Color color)
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.15f;
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
        arrowHead.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        
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
        
        Vector3 midPoint = (startPos + endPos) / 2f + Vector3.up * 1.5f;
        textObject.transform.position = midPoint;
        
        textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = GetActionText(actionType);
        textMesh.fontSize = 50;
        textMesh.characterSize = 0.1f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
        
        var meshRenderer = textObject.GetComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("GUI/Text Shader"));
        meshRenderer.material.color = Color.white;
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
        if (textObject != null && Camera.main != null)
        {
            textObject.transform.LookAt(Camera.main.transform);
            textObject.transform.Rotate(0, 180, 0);
        }
    }
}
