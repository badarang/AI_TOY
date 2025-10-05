using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(StageData))]
public class StageDataEditor : OdinEditor
{
    // State for the visual grid editor
    private int _selectedWaveIndex = 0;
    private Vector2Int _contextMenuPos;

    // Cache for performance
    private Dictionary<UnitType, int> _unitScoreCache;
    private Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();

    // Style Constants
    private const int CELL_SIZE = 42;
    private const int CELL_PADDING = 5;
    private static readonly Color PLAYER_COLOR = new Color(0.2f, 0.8f, 0.2f, 1f);
    private static readonly Color ENEMY_COLOR = new Color(0.9f, 0.3f, 0.3f, 1f);
    private static readonly Color HOVER_COLOR = new Color(0.3f, 0.6f, 1f, 0.15f);

    protected override void OnEnable()
    {
        base.OnEnable();
        CalculateDifficulty((StageData)target);
    }

    public override void OnInspectorGUI()
    {
        // First, let Odin draw all the properties defined in StageData,
        // which automatically handles showing/hiding fields based on StageType.
        base.OnInspectorGUI();

        var data = (StageData)target;
        var battleTypes = new[] { StageType.Battle, StageType.EliteBattle, StageType.Boss };

        // Only show the custom grid editor and difficulty calculator for battle types
        if (battleTypes.Contains(data.stageType))
        {
            EditorGUILayout.Space(20);
            var headerStyle = new GUIStyle(EditorStyles.largeLabel) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("Battle Grid Visual Editor", headerStyle);
            EditorGUILayout.Space(5);

            // Wrap our custom GUI in a change check.
            // If any value is changed by our GUI, recalculate difficulty and save.
            EditorGUI.BeginChangeCheck();

            DrawWaveSelector();
            EditorGUILayout.Space(10);
            DrawMainGrid();

            if (EditorGUI.EndChangeCheck())
            {
                CalculateDifficulty(data);
            }
        }
    }

    #region Visual Grid Editor UI

    private void DrawWaveSelector()
    {
        var data = (StageData)target;
        EditorGUILayout.BeginVertical(CreateBoxStyle());
        
        if (data.waves == null) data.waves = new EnemyWave[0];
        if (_selectedWaveIndex >= data.waves.Length) _selectedWaveIndex = Mathf.Max(0, data.waves.Length - 1);

        string[] waveLabels = data.waves.Select((w, i) => string.IsNullOrEmpty(w.waveName) ? $"Wave {i + 1}" : w.waveName).ToArray();
        _selectedWaveIndex = GUILayout.Toolbar(_selectedWaveIndex, waveLabels);

        EditorGUILayout.EndVertical();
    }

    private void DrawMainGrid()
    {
        var data = (StageData)target;
        float totalWidth = data.width * (CELL_SIZE + CELL_PADDING) + CELL_PADDING;
        float inspectorWidth = EditorGUIUtility.currentViewWidth - 36f;
        Rect bgRect = GUILayoutUtility.GetRect(inspectorWidth, totalWidth + 32f);
        EditorGUI.DrawRect(bgRect, EditorGUIUtility.isProSkin ? new Color(0.20f, 0.20f, 0.22f, 1f) : new Color(0.94f, 0.94f, 0.96f, 1f));
        RenderGrid(bgRect);
        DrawLegend();
    }

    private void RenderGrid(Rect gridRect)
    {
        var data = (StageData)target;
        float gridStartX = (gridRect.width - (data.width * (CELL_SIZE + CELL_PADDING) + CELL_PADDING)) / 2f;
        float gridStartY = 12f;
        Event e = Event.current;
        Vector2Int hoveredCell = GetHoveredCell(gridRect.x + gridStartX, gridRect.y + gridStartY, e.mousePosition);

        bool isWaveValid = data.waves != null && data.waves.Length > 0 && _selectedWaveIndex < data.waves.Length;
        var wave = isWaveValid ? data.waves[_selectedWaveIndex] : null;
        if (wave != null && wave.enemySpawns == null) wave.enemySpawns = new EnemySpawnData[0];
        
        EnemySpawnData[] currentEnemies = (wave != null && wave.enemySpawns != null) ? wave.enemySpawns : new EnemySpawnData[0];

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                Vector2Int pos = new Vector2Int(x, data.height - y - 1);
                bool isPlayer = data.playerSpawn == pos;
                var enemy = currentEnemies.FirstOrDefault(es => es.spawnPos == pos);
                var obstacle = data.obstacleSpawns?.FirstOrDefault(os => os.spawnPos == pos);
                bool isHovered = hoveredCell == new Vector2Int(x, y);

                float px = gridRect.x + gridStartX + x * (CELL_SIZE + CELL_PADDING) + CELL_PADDING / 2f;
                float py = gridRect.y + gridStartY + y * (CELL_SIZE + CELL_PADDING) + CELL_PADDING / 2f;
                Rect cellRect = new Rect(px, py, CELL_SIZE, CELL_SIZE);

                DrawCellBackground(cellRect, isPlayer, enemy != null, obstacle != null, isHovered);
                DrawCellBorder(cellRect, isPlayer, enemy != null);
                RenderUnit(cellRect, isPlayer, enemy, obstacle, data.playerType);

                HandleContextMenu(cellRect, pos, isPlayer, enemy, obstacle, e);
            }
        }
    }

    private void HandleContextMenu(Rect cellRect, Vector2Int pos, bool isPlayer, EnemySpawnData enemy, ObstacleSpawnData obstacle, Event e)
    {
        if (e.type != EventType.ContextClick || !cellRect.Contains(e.mousePosition)) return;

        _contextMenuPos = pos;
        GenericMenu menu = new GenericMenu();
        var data = (StageData)target;
        bool isWaveValid = data.waves != null && data.waves.Length > 0 && _selectedWaveIndex < data.waves.Length;

        // Player spawn logic
        if (!isPlayer && enemy == null && obstacle == null) menu.AddItem(new GUIContent("👤 Set Player Spawn"), false, () => { serializedObject.FindProperty("playerSpawn").vector2IntValue = _contextMenuPos; serializedObject.ApplyModifiedProperties(); });
        else menu.AddDisabledItem(new GUIContent("👤 Cell Occupied"));

        menu.AddSeparator("");

        // Enemy spawn logic
        if (enemy == null && !isPlayer && obstacle == null)
        {
            if (!isWaveValid) { menu.AddDisabledItem(new GUIContent("👹 Add Enemy (No Wave)")); }
            else
            {
                var enemyTypes = System.Enum.GetValues(typeof(UnitType)).Cast<UnitType>().Where(t => t.ToString().StartsWith("Enemy_")).ToArray();
                foreach (var enemyType in enemyTypes)
                {
                    menu.AddItem(new GUIContent($"👹 Add {enemyType} to Wave"), false, () => AddEnemyToWave(enemyType));
                }
            }
        }
        else if (enemy != null)
        {
            menu.AddItem(new GUIContent($"🗑️ Remove {enemy.enemyType}"), false, RemoveEnemyFromWave);
        }
        else { menu.AddDisabledItem(new GUIContent("👹 Cell Occupied")); }

        menu.ShowAsContext();
        e.Use();
    }

    #endregion

    #region Data Modification

    private void AddEnemyToWave(UnitType type)
    {
        var waveProp = serializedObject.FindProperty("waves").GetArrayElementAtIndex(_selectedWaveIndex);
        var enemySpawnsProp = waveProp.FindPropertyRelative("enemySpawns");
        if (!enemySpawnsProp.isArray) enemySpawnsProp.arraySize = 0;

        enemySpawnsProp.InsertArrayElementAtIndex(enemySpawnsProp.arraySize);
        var newEnemyProp = enemySpawnsProp.GetArrayElementAtIndex(enemySpawnsProp.arraySize - 1);
        newEnemyProp.FindPropertyRelative("enemyType").enumValueIndex = (int)(object)type;
        newEnemyProp.FindPropertyRelative("spawnPos").vector2IntValue = _contextMenuPos;
        
        serializedObject.ApplyModifiedProperties();
    }

    private void RemoveEnemyFromWave()
    {
        var waveProp = serializedObject.FindProperty("waves").GetArrayElementAtIndex(_selectedWaveIndex);
        var enemySpawnsProp = waveProp.FindPropertyRelative("enemySpawns");
        if (!enemySpawnsProp.isArray) return;

        for (int i = 0; i < enemySpawnsProp.arraySize; i++)
        {
            if (enemySpawnsProp.GetArrayElementAtIndex(i).FindPropertyRelative("spawnPos").vector2IntValue == _contextMenuPos)
            {
                enemySpawnsProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return;
            }
        }
    }

    #endregion

    #region Difficulty Calculation

    private void CalculateDifficulty(StageData data)
    {
        if (_unitScoreCache == null) CacheUnitDataScores();

        float totalDifficulty = 0;
        if (data.waves != null)
        {
            for (int i = 0; i < data.waves.Length; i++)
            {
                var wave = data.waves[i];
                if (wave.enemySpawns == null) continue;

                float waveMultiplier = 1f + (i * 0.2f); // Wave 1: 1.0x, Wave 2: 1.2x, etc.

                foreach (var enemySpawn in wave.enemySpawns)
                {
                    if (_unitScoreCache.TryGetValue(enemySpawn.enemyType, out int score))
                    {
                        totalDifficulty += score * waveMultiplier;
                    }
                }
            }
        }

        var difficultyProp = serializedObject.FindProperty("difficulty");
        difficultyProp.intValue = Mathf.RoundToInt(totalDifficulty);
        serializedObject.ApplyModifiedProperties();
    }

    private void CacheUnitDataScores()
    {
        _unitScoreCache = new Dictionary<UnitType, int>();
        string[] guids = AssetDatabase.FindAssets("t:UnitData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnitData unitData = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (unitData != null && !_unitScoreCache.ContainsKey(unitData.unitType))
            {
                _unitScoreCache.Add(unitData.unitType, unitData.difficultyScore);
            }
        }
    }

    #endregion

    #region Rendering Helpers

    private Vector2Int GetHoveredCell(float startX, float startY, Vector2 mousePos)
    {
        var data = (StageData)target;
        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                Rect cell = new Rect(startX + x * (CELL_SIZE + CELL_PADDING), startY + y * (CELL_SIZE + CELL_PADDING), CELL_SIZE, CELL_SIZE);
                if (cell.Contains(mousePos)) return new Vector2Int(x, y);
            }
        }
        return new Vector2Int(-1, -1);
    }

    private void DrawCellBackground(Rect cellRect, bool isPlayer, bool hasEnemy, bool hasObstacle, bool isHovered)
    {
        Color cellBg = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 1f) : new Color(0.97f, 0.97f, 0.99f, 1f);
        if (isPlayer) cellBg = Color.Lerp(cellBg, PLAYER_COLOR, 0.10f);
        else if (hasEnemy) cellBg = Color.Lerp(cellBg, ENEMY_COLOR, 0.10f);
        else if (hasObstacle) cellBg = Color.Lerp(cellBg, new Color(0.5f, 0.3f, 0.1f, 1f), 0.20f);
        EditorGUI.DrawRect(cellRect, cellBg);
        if (isHovered) EditorGUI.DrawRect(cellRect, HOVER_COLOR);
    }

    private void RenderUnit(Rect cellRect, bool isPlayer, EnemySpawnData enemy, ObstacleSpawnData obstacle, UnitType playerType)
    {
        if (isPlayer) DrawUnitLabel(cellRect, GetShortName(playerType.ToString()), PLAYER_COLOR, "👤");
        else if (enemy != null) DrawUnitLabel(cellRect, GetShortName(enemy.enemyType.ToString()), ENEMY_COLOR, "👹");
        else if (obstacle != null) GUI.Label(new Rect(cellRect.x, cellRect.y + 2, cellRect.width, cellRect.height - 4), "🌲", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
    }

    private void DrawUnitLabel(Rect cellRect, string shortName, Color color, string emoji)
    {
        var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = color } };
        var emojiStyle = new GUIStyle(style) { fontSize = 16 };
        GUI.Label(new Rect(cellRect.x, cellRect.y + 2, cellRect.width, cellRect.height / 2), emoji, emojiStyle);
        GUI.Label(new Rect(cellRect.x, cellRect.y + cellRect.height / 2 - 2, cellRect.width, cellRect.height / 2), shortName, style);
    }

    private void DrawCellBorder(Rect cellRect, bool isPlayer, bool hasEnemy)
    {
        Color finalBorderCol = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0.7f, 0.7f, 0.7f, 1f);
        if (isPlayer) finalBorderCol = PLAYER_COLOR;
        else if (hasEnemy) finalBorderCol = ENEMY_COLOR;
        Handles.BeginGUI();
        Handles.color = finalBorderCol;
        Handles.DrawSolidRectangleWithOutline(new Vector3[] { new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.xMax, cellRect.y), new Vector3(cellRect.xMax, cellRect.yMax), new Vector3(cellRect.x, cellRect.yMax), }, Color.clear, finalBorderCol);
        Handles.EndGUI();
    }

    private void DrawLegend()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(12, 12), PLAYER_COLOR);
        GUILayout.Label("Player", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(15);
        EditorGUILayout.BeginHorizontal();
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(12, 12), ENEMY_COLOR);
        GUILayout.Label("Enemy", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private GUIStyle CreateBoxStyle() { return new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 8, 8), margin = new RectOffset(0, 0, 2, 2) }; }
    private string GetShortName(string type) { if (string.IsNullOrEmpty(type)) return "?"; int idx = type.IndexOf('_'); return idx >= 0 && type.Length >= idx + 3 ? type.Substring(idx + 1, 2) : (type.Length >= 2 ? type.Substring(0, 2) : type); }

    #endregion
}
