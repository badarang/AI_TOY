using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class GridManager : MonoBehaviour
{
    public int width { get; private set; }
    public int height { get; private set; }

    private List<GameObject> gridLines = new List<GameObject>();
    private Vector2Int? hoveredCell = null;
    private Vector2Int? selectedCell = null;
    private UnitBase selectedUnit = null;

    public LayerMask unitLayer;
    public Material highlightMat;
    private GameObject highlightQuad;
    private Material defaultHighlightMat;

    // New Input System 관련 변수들
    private PlayerInputActions inputActions;
    private Camera mainCamera;

    void Awake()
    {
        // PlayerInputActions 인스턴스 생성
        inputActions = new PlayerInputActions();
        mainCamera = Camera.main;
    }

    void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Click.performed += OnClickPerformed;
            inputActions.Enable();
        }
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Click.performed -= OnClickPerformed;
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
    void Update()
    {
        // 카메라가 없으면 실행하지 않음
        if (mainCamera == null)
        {
            Debug.LogWarning("GridManager: mainCamera is null!");
            return;
        }

        Vector2 pointValue = GetPointValue();
        
        // 월드 좌표로 직접 Ray 생성
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(pointValue.x, pointValue.y, mainCamera.nearClipPlane));
        Ray ray = new Ray(mainCamera.transform.position, (worldPos - mainCamera.transform.position).normalized);

        DebugPrinter.DebugColor(DebugType.Input, $"{pointValue.x}, {pointValue.y}");

        // 디버깅용 Ray 그리기 (Scene 뷰에서 확인 가능)
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        // 단일 Raycast로 통일 (거리 제한 없음)
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name} at {hit.point}");
            
            Vector3 point = hit.point;
            int x = Mathf.FloorToInt(point.x);
            int z = Mathf.FloorToInt(point.z);
            
            // 격자 범위 내에 있는지 확인
            if (x >= 0 && x < width && z >= 0 && z < height)
            {
                // 새로운 셀에 호버했을 때만 하이라이트 업데이트
                if (!hoveredCell.HasValue || hoveredCell.Value != new Vector2Int(x, z))
                {
                    hoveredCell = new Vector2Int(x, z);
                    ShowHighlight(x, z);
                }
            }
            else
            {
                if (hoveredCell.HasValue)
                {
                    hoveredCell = null;
                    HideHighlight();
                }
            }
        }
        else
        {
            // Raycast가 실패한 경우 하이라이트 숨기기
            if (hoveredCell.HasValue)
            {
                hoveredCell = null;
                HideHighlight();
            }
        }
    }

    // 클릭 이벤트 처리
    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        DebugPrinter.DebugColor(DebugType.Input, $"{inputActions.Gameplay.Point.ReadValue<Vector2>().x}, {inputActions.Gameplay.Point.ReadValue<Vector2>().y} Clicked");
        if (hoveredCell.HasValue)
        {
            TrySelectUnitAtCell(hoveredCell.Value.x, hoveredCell.Value.y);
        }
    }

    public void GenerateGrid(StageData stageData)
    {
        width = stageData.width;
        height = stageData.height;
        ClearGridLines();
        hoveredCell = null;
        selectedCell = null;
        selectedUnit = null;
        if (highlightQuad != null) Destroy(highlightQuad);

        // 카메라 위치를 고려하여 격자 Y 위치 조정
        float gridY = 0.01f; // 격자 높이
        
        // 세로선
        for (int x = 0; x <= width; x++)
        {
            CreateGridLine(new Vector3(x, gridY, 0), new Vector3(x, gridY, height));
        }
        // 가로선
        for (int z = 0; z <= height; z++)
        {
            CreateGridLine(new Vector3(0, gridY, z), new Vector3(width, gridY, z));
        }
        
        Debug.Log($"Grid generated at Y={gridY}, size: {width}x{height}");
    }


    // 크로스 플랫폼 입력 위치 가져오기
    private Vector2 GetPointValue()
    {
        if (inputActions != null)
        {
            Vector2 pointValue = inputActions.Gameplay.Point.ReadValue<Vector2>();
            if (pointValue != Vector2.zero)
            {
                return pointValue;
            }
        }

        Vector2 fallbackPosition = Input.mousePosition;
        return fallbackPosition;
    }

    void ShowHighlight(int x, int z)
    {
        if (highlightQuad == null)
        {
            highlightQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlightQuad.transform.localScale = new Vector3(1, 1, 1);
            highlightQuad.transform.rotation = Quaternion.Euler(90, 0, 0);
            highlightQuad.GetComponent<Collider>().enabled = false;
            defaultHighlightMat = highlightQuad.GetComponent<Renderer>().material;
            
            // 디버깅: 하이라이트 쿼드 생성 확인
            Debug.Log("Highlight quad created");
        }
        
        // 격자 높이에 맞춰 하이라이트 위치 조정
        float gridY = 0.01f;
        highlightQuad.transform.position = new Vector3(x + 0.5f, gridY + 0.01f, z + 0.5f);
        highlightQuad.SetActive(true);
        
        var rend = highlightQuad.GetComponent<Renderer>();
        if (highlightMat != null)
        {
            rend.material = highlightMat;
            Debug.Log($"Using custom highlight material at position ({x}, {z})");
        }
        else
        {
            // 기본 파란색 반투명 재질 생성
            Material defaultMat = new Material(Shader.Find("Standard"));
            defaultMat.color = new Color(0.3f, 0.6f, 1f, 0.3f);
            defaultMat.SetFloat("_Mode", 3); // Transparent mode
            defaultMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            defaultMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            defaultMat.SetInt("_ZWrite", 0);
            defaultMat.DisableKeyword("_ALPHATEST_ON");
            defaultMat.EnableKeyword("_ALPHABLEND_ON");
            defaultMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            defaultMat.renderQueue = 3000;
            
            rend.material = defaultMat;
            Debug.Log($"Using default blue highlight material at position ({x}, {z})");
        }
    }

    void HideHighlight()
    {
        if (highlightQuad != null)
            highlightQuad.SetActive(false);
    }

    void TrySelectUnitAtCell(int x, int z)
    {
        // 격자 높이에 맞춰 유닛 검색 위치 조정
        float gridY = 0.01f;
        Vector3 cellCenter = new Vector3(x + 0.5f, gridY + 0.5f, z + 0.5f);
        Collider[] hits = Physics.OverlapSphere(cellCenter, 0.3f, unitLayer);
        if (hits.Length > 0)
        {
            Debug.Log($"Cell ({x}, {z}) has {hits.Length} units.");
            UnitBase unit = hits[0].GetComponent<UnitBase>();
            if (unit != null && !(unit is EnemyUnit))
            {
                if (selectedUnit != null)
                    selectedUnit.Deselect();
                selectedUnit = unit;
                selectedUnit.Select();
                selectedCell = new Vector2Int(x, z);
            }
            else
            {
                // EnemyUnit이거나 Unit이 아니면 선택 해제
                if (selectedUnit != null)
                    selectedUnit.Deselect();
                selectedUnit = null;
                selectedCell = null;
            }
        }
        else
        {
            if (selectedUnit != null)
                selectedUnit.Deselect();
            selectedUnit = null;
            selectedCell = null;
        }
    }

    void CreateGridLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.parent = this.transform;
        var lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = Color.gray;
        lr.useWorldSpace = true;
        gridLines.Add(lineObj);
    }

    void ClearGridLines()
    {
        foreach (var go in gridLines)
        {
            if (go != null) Destroy(go);
        }
        gridLines.Clear();
    }

    public bool IsMovable(Vector2Int pos)
    {
        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(new Vector3(x, 0, 0), new Vector3(x, 0, height));
        }
        for (int z = 0; z <= height; z++)
        {
            Gizmos.DrawLine(new Vector3(0, 0, z), new Vector3(width, 0, z));
        }
    }
} 