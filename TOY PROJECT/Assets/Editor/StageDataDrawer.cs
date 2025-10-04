// 최종 수정 버전: 모든 헬퍼 메서드의 반환문 오류를 해결하고, 아이콘 렌더링 로직을 복원했습니다.
using System;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(StageData))]
public class StageDataEditor : OdinEditor
{
    // State
    private int selectedWaveIndex = 0;
    private Vector2Int contextMenuPos;
    private Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();
    private Vector2 scrollPosition;
    private bool showAdvancedOptions = false;
    private bool showGridSettings = true;
    private bool showUnitSettings = true;

    // Style Constants
    private const int CELL_SIZE = 42;
    private const int CELL_PADDING = 5;
    private const int ICON_SIZE = 32;
    private static readonly Color PLAYER_COLOR = new Color(0.2f, 0.8f, 0.2f, 1f);
    private static readonly Color ENEMY_COLOR = new Color(0.9f, 0.3f, 0.3f, 1f);
    private static readonly Color HOVER_COLOR = new Color(0.3f, 0.6f, 1f, 0.15f);

    public override void OnInspectorGUI()
    {
        StageData data = (StageData)target;
        serializedObject.Update();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        GUILayout.Space(10);

        DrawGridSettings(data);
        GUILayout.Space(15);

        DrawWaveSelector(data);
        GUILayout.Space(15);

        DrawMainGrid(data);
        GUILayout.Space(15);

        DrawUnitSettings(data);

        DrawAdvancedOptions(data);

        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
        serializedObject.ApplyModifiedProperties();
    }

    // --- UI Drawing Methods ---

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(CreateBoxStyle());
        var headerStyle = new GUIStyle(EditorStyles.largeLabel) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black } };
        GUILayout.Label("🎮 Stage Data Editor", headerStyle);
        var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 };
        GUILayout.Label("Grid-based level editor for Unity", subtitleStyle);
        EditorGUILayout.EndVertical();
    }

    private void DrawGridSettings(StageData data)
    {
        showGridSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGridSettings, "🗺️ Grid Settings");
        if (showGridSettings)
        {
            EditorGUILayout.BeginVertical(CreateBoxStyle());
            EditorGUILayout.PropertyField(serializedObject.FindProperty("width"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
            EditorGUILayout.Space(5);
            var infoStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 10, normal = { textColor = Color.gray } };
            GUILayout.Label($"Total Cells: {data.width * data.height} | Right-click on grid to add/remove units", infoStyle);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawWaveSelector(StageData data)
    {
        EditorGUILayout.BeginVertical(CreateBoxStyle());
        var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        GUILayout.Label("🌊 Wave Editor", headerStyle);
        GUILayout.Space(5);

        if (data.waves == null) data.waves = new EnemyWave[0];

        string[] waveLabels = data.waves.Select((w, i) => string.IsNullOrEmpty(w.waveName) ? $"Wave {i + 1}" : w.waveName).ToArray();
        selectedWaveIndex = GUILayout.Toolbar(selectedWaveIndex, waveLabels);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("＋ Add Wave"))
        {
            Undo.RecordObject(data, "Add Wave");
            var list = data.waves.ToList();
            list.Add(new EnemyWave { waveName = $"Wave {list.Count + 1}", enemySpawns = new EnemySpawnData[0] });
            data.waves = list.ToArray();
            selectedWaveIndex = data.waves.Length - 1;
            EditorUtility.SetDirty(data);
        }
        if (data.waves.Length > 0 && GUILayout.Button("－ Remove Current Wave"))
        {
            if (EditorUtility.DisplayDialog("Remove Wave", $"Are you sure you want to remove \"{waveLabels[selectedWaveIndex]}\"?", "Yes", "Cancel"))
            {
                Undo.RecordObject(data, "Remove Wave");
                var list = data.waves.ToList();
                list.RemoveAt(selectedWaveIndex);
                data.waves = list.ToArray();
                selectedWaveIndex = Mathf.Max(0, selectedWaveIndex - 1);
                EditorUtility.SetDirty(data);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawMainGrid(StageData data)
    {
        float totalWidth = data.width * (CELL_SIZE + CELL_PADDING) + CELL_PADDING;
        float totalHeight = data.height * (CELL_SIZE + CELL_PADDING) + CELL_PADDING;
        float inspectorWidth = EditorGUIUtility.currentViewWidth - 36f;
        float gridStartX = Mathf.Max((inspectorWidth - totalWidth) / 2f, 0f);
        Rect bgRect = GUILayoutUtility.GetRect(inspectorWidth, totalHeight + 32f);
        EditorGUI.DrawRect(bgRect, EditorGUIUtility.isProSkin ? new Color(0.20f, 0.20f, 0.22f, 1f) : new Color(0.94f, 0.94f, 0.96f, 1f));
        Rect gridRect = new Rect(bgRect.x + gridStartX, bgRect.y + 12f, totalWidth, totalHeight);
        EditorGUI.DrawRect(gridRect, EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 1f) : new Color(0.97f, 0.97f, 0.99f, 1f));
        RenderGrid(data, gridRect);
        DrawLegend();
    }

    private void RenderGrid(StageData data, Rect gridRect)
    {
        float gridStartX = gridRect.x + CELL_PADDING;
        float gridStartY = gridRect.y + CELL_PADDING;
        Event e = Event.current;
        Vector2Int hoveredCell = GetHoveredCell(data, gridStartX, gridStartY, e.mousePosition);

        bool isWaveValid = data.waves != null && data.waves.Length > 0 && selectedWaveIndex < data.waves.Length;
        EnemySpawnData[] currentEnemies = isWaveValid ? data.waves[selectedWaveIndex].enemySpawns : new EnemySpawnData[0];

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                Vector2Int pos = new Vector2Int(x, data.height - y - 1);
                bool isPlayer = data.playerSpawn == pos;
                var enemy = currentEnemies.FirstOrDefault(es => es.spawnPos == pos);
                var obstacle = data.obstacleSpawns.FirstOrDefault(os => os.spawnPos == pos);
                bool isHovered = hoveredCell == new Vector2Int(x, y);

                float px = gridStartX + x * (CELL_SIZE + CELL_PADDING);
                float py = gridStartY + y * (CELL_SIZE + CELL_PADDING);
                Rect cellRect = new Rect(px, py, CELL_SIZE, CELL_SIZE);

                Color cellBg = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 1f) : new Color(0.97f, 0.97f, 0.99f, 1f);
                if (isPlayer) cellBg = Color.Lerp(cellBg, PLAYER_COLOR, 0.10f);
                else if (enemy != null) cellBg = Color.Lerp(cellBg, ENEMY_COLOR, 0.10f);
                else if (obstacle != null) cellBg = Color.Lerp(cellBg, new Color(0.5f, 0.3f, 0.1f, 1f), 0.20f);
                EditorGUI.DrawRect(cellRect, cellBg);
                if (isHovered) EditorGUI.DrawRect(cellRect, HOVER_COLOR);
                DrawCellBorder(cellRect, isPlayer, enemy != null);
                RenderUnit(cellRect, isPlayer, enemy, obstacle, data.playerType);

                HandleContextMenu(data, cellRect, pos, isPlayer, enemy, obstacle, e);
            }
        }
        DrawGridLines(gridRect, data, gridStartX, gridStartY, EditorGUIUtility.isProSkin ? new Color(0.35f, 0.35f, 0.35f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f));
    }

    private void DrawUnitSettings(StageData data)
    {
        showUnitSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showUnitSettings, "⚔️ Unit & Wave Settings");
        if (showUnitSettings)
        {
            EditorGUILayout.BeginVertical(CreateBoxStyle());
            DrawPlayerSettings(data);
            GUILayout.Space(10);
            DrawEnemySettings(data);
            GUILayout.Space(10);
            DrawObstacleSettings(data);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawEnemySettings(StageData data)
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUILayout.Label("👹 Enemy Configuration (Current Wave)", headerStyle);

        if (data.waves == null || data.waves.Length == 0 || selectedWaveIndex >= data.waves.Length)
        {
            EditorGUILayout.HelpBox("No wave selected or available. Please add a wave first.", MessageType.Info);
            return;
        }

        var waveProp = serializedObject.FindProperty("waves").GetArrayElementAtIndex(selectedWaveIndex);
        EditorGUILayout.PropertyField(waveProp.FindPropertyRelative("waveName"));
        EditorGUILayout.PropertyField(waveProp.FindPropertyRelative("enemySpawns"), true);

        var currentEnemies = data.waves[selectedWaveIndex].enemySpawns;
        if (currentEnemies != null && currentEnemies.Length > 0)
        {
            var statsStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 10 };
            var enemyTypes = currentEnemies.GroupBy(e => e.enemyType).Select(g => $"{g.Key}: {g.Count()}");
            GUILayout.Label($"Total Enemies in Wave: {currentEnemies.Length} ({string.Join(", ", enemyTypes)})", statsStyle);
        }
    }

    private void HandleContextMenu(StageData data, Rect cellRect, Vector2Int pos, bool isPlayer, EnemySpawnData enemy, ObstacleSpawnData obstacle, Event e)
    {
        if (e.type == EventType.ContextClick && cellRect.Contains(e.mousePosition))
        {
            contextMenuPos = pos;
            GenericMenu menu = new GenericMenu();

            bool isWaveValid = data.waves != null && data.waves.Length > 0 && selectedWaveIndex < data.waves.Length;

            // Player Menu
            if (!isPlayer && enemy == null && obstacle == null) menu.AddItem(new GUIContent("👤 Set Player Spawn"), false, () => { Undo.RecordObject(data, "Set Player Spawn"); data.playerSpawn = contextMenuPos; EditorUtility.SetDirty(data); });
            else menu.AddDisabledItem(new GUIContent("👤 Occupied"));
            menu.AddSeparator("");

            // Enemy Menu
            if (enemy == null && !isPlayer && obstacle == null)
            {
                if (!isWaveValid) { menu.AddDisabledItem(new GUIContent("👹 Add Enemy (No Wave)")); }
                else
                {
                    var enemyTypes = System.Enum.GetValues(typeof(UnitType)).Cast<UnitType>().Where(t => t.ToString().StartsWith("Enemy_")).ToArray();
                    foreach (var enemyType in enemyTypes)
                    {
                        var type = enemyType;
                        menu.AddItem(new GUIContent($"👹 Add {type} to Wave"), false, () =>
                        {
                            var waveProp = serializedObject.FindProperty("waves").GetArrayElementAtIndex(selectedWaveIndex).FindPropertyRelative("enemySpawns");
                            waveProp.InsertArrayElementAtIndex(waveProp.arraySize);
                            var newEnemyProp = waveProp.GetArrayElementAtIndex(waveProp.arraySize - 1);
                            newEnemyProp.FindPropertyRelative("enemyType").enumValueIndex = (int)(object)type;
                            newEnemyProp.FindPropertyRelative("spawnPos").vector2IntValue = contextMenuPos;
                            serializedObject.ApplyModifiedProperties();
                        });
                    }
                }
            }
            else if (enemy != null)
            {
                menu.AddItem(new GUIContent($"🗑️ Remove {enemy.enemyType} from Wave"), false, () =>
                {
                    var waveProp = serializedObject.FindProperty("waves").GetArrayElementAtIndex(selectedWaveIndex).FindPropertyRelative("enemySpawns");
                    int indexToRemove = Array.FindIndex(data.waves[selectedWaveIndex].enemySpawns, en => en.spawnPos == contextMenuPos);
                    if (indexToRemove != -1) { waveProp.DeleteArrayElementAtIndex(indexToRemove); }
                    serializedObject.ApplyModifiedProperties();
                });
            }
            else { menu.AddDisabledItem(new GUIContent("👹 Occupied")); }

            // Obstacle Menu
            var obsIdx = Array.FindIndex(data.obstacleSpawns, o => o.spawnPos == contextMenuPos);
            if (obsIdx < 0 && !isPlayer && enemy == null)
            {
                var guids = AssetDatabase.FindAssets("t:ObstacleData");
                if (guids.Length > 0)
                {
                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var obsData = AssetDatabase.LoadAssetAtPath<ObstacleData>(path);
                        if (obsData == null) continue;
                        menu.AddItem(new GUIContent($"🌲 Add Obstacle/{obsData.unitMeta.nameKey}"), false, () =>
                        {
                            Undo.RecordObject(data, $"Add Obstacle {obsData.unitMeta.nameKey}");
                            var list = data.obstacleSpawns.ToList();
                            list.Add(new ObstacleSpawnData { obstacleData = obsData, spawnPos = contextMenuPos });
                            data.obstacleSpawns = list.ToArray();
                            EditorUtility.SetDirty(data);
                        });
                    }
                }
                else { menu.AddDisabledItem(new GUIContent("🌲 No ObstacleData found")); }
            }
            else if (obsIdx >= 0)
            {
                var obsName = data.obstacleSpawns[obsIdx].obstacleData != null ? data.obstacleSpawns[obsIdx].obstacleData.unitMeta.nameKey : "Obstacle";
                menu.AddItem(new GUIContent($"🗑️ Remove Obstacle ({obsName})"), false, () =>
                {
                    Undo.RecordObject(data, "Remove Obstacle");
                    data.obstacleSpawns = data.obstacleSpawns.Where((o, i) => i != obsIdx).ToArray();
                    EditorUtility.SetDirty(data);
                });
            }
            else { menu.AddDisabledItem(new GUIContent("🌲 Occupied")); }

            menu.ShowAsContext();
            e.Use();
        }
    }

    // --- Helper methods with original bodies restored ---

    private void DrawPlayerSettings(StageData data)
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUILayout.Label("👤 Player Configuration", headerStyle);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playerType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playerSpawn"));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawObstacleSettings(StageData data)
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUILayout.Label("🌲 Obstacle Configuration", headerStyle);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleSpawns"), true);
    }

    private void DrawAdvancedOptions(StageData data)
    {
        showAdvancedOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showAdvancedOptions, "⚙️ Advanced Options");
        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical(CreateBoxStyle());
            if (GUILayout.Button("🧹 Clear All Enemies in Current Wave"))
            {
                if (EditorUtility.DisplayDialog("Clear Enemies", "Are you sure you want to remove all enemies from the current wave?", "Yes", "Cancel"))
                {
                    if (data.waves != null && selectedWaveIndex < data.waves.Length)
                    {
                        Undo.RecordObject(data, "Clear Enemies in Wave");
                        data.waves[selectedWaveIndex].enemySpawns = new EnemySpawnData[0];
                        EditorUtility.SetDirty(data);
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
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

    private void DrawGridLines(Rect gridRect, StageData data, float startX, float startY, Color gridLineCol)
    {
        Handles.BeginGUI();
        Handles.color = gridLineCol;
        for (int x = 0; x <= data.width; x++) { float px = startX + x * (CELL_SIZE + CELL_PADDING) - CELL_PADDING / 2f; Handles.DrawLine(new Vector3(px, gridRect.y), new Vector3(px, gridRect.yMax)); }
        for (int y = 0; y <= data.height; y++) { float py = startY + y * (CELL_SIZE + CELL_PADDING) - CELL_PADDING / 2f; Handles.DrawLine(new Vector3(gridRect.x, py), new Vector3(gridRect.xMax, py)); }
        Handles.EndGUI();
    }

    private Vector2Int GetHoveredCell(StageData data, float startX, float startY, Vector2 mousePos)
    {
        for (int y = 0; y < data.height; y++) { for (int x = 0; x < data.width; x++) { float px = startX + x * (CELL_SIZE + CELL_PADDING); float py = startY + y * (CELL_SIZE + CELL_PADDING); Rect cell = new Rect(px, py, CELL_SIZE, CELL_SIZE); if (cell.Contains(mousePos)) { return new Vector2Int(x, y); } } }
        return new Vector2Int(-1, -1);
    }

    private void RenderUnit(Rect cellRect, bool isPlayer, EnemySpawnData enemy, ObstacleSpawnData obstacle, UnitType playerType)
    {
        if (isPlayer) RenderPlayerUnit(cellRect, playerType);
        else if (enemy != null) RenderEnemyUnit(cellRect, enemy.enemyType);
        else if (obstacle != null && obstacle.obstacleData != null) RenderObstacleUnit(cellRect, obstacle.obstacleData);
    }

    private void RenderPlayerUnit(Rect cellRect, UnitType playerType)
    {
        string typeName = playerType.ToString();
        Texture2D icon = GetUnitIcon(typeName);
        if (icon)
        {
            var iconRect = new Rect(cellRect.x + (cellRect.width - ICON_SIZE) / 2, cellRect.y + (cellRect.height - ICON_SIZE) / 2, ICON_SIZE, ICON_SIZE);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
        }
        else
        {
            DrawUnitLabel(cellRect, GetShortName(typeName), PLAYER_COLOR, "👤");
        }
    }

    private void RenderEnemyUnit(Rect cellRect, UnitType enemyType)
    {
        string typeName = enemyType.ToString();
        Texture2D icon = GetUnitIcon(typeName);
        if (icon)
        {
            var iconRect = new Rect(cellRect.x + (cellRect.width - ICON_SIZE) / 2, cellRect.y + (cellRect.height - ICON_SIZE) / 2, ICON_SIZE, ICON_SIZE);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
        }
        else
        {
            DrawUnitLabel(cellRect, GetShortName(typeName), ENEMY_COLOR, "👹");
        }
    }

    private void RenderObstacleUnit(Rect cellRect, ObstacleData obstacleData)
    {
        var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
        GUI.Label(cellRect, "🌲", style);
    }

    private void DrawUnitLabel(Rect cellRect, string shortName, Color color, string emoji)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14, normal = { textColor = color } };
        GUI.Label(cellRect, $"{emoji}\n{shortName}", style);
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

    private GUIStyle CreateBoxStyle()
    {
        return new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 8, 8), margin = new RectOffset(0, 0, 2, 2) };
    }

    private string GetShortName(string type)
    {
        if (string.IsNullOrEmpty(type)) return "?";
        int idx = type.IndexOf('_');
        return idx >= 0 ? type.Substring(idx + 1) : type;
    }

    private Texture2D GetUnitIcon(string type) 
    {
        if (string.IsNullOrEmpty(type)) return null;
        if (iconCache.TryGetValue(type, out var tex))
            return tex;
        tex = Resources.Load<Texture2D>($"UnitIcons/{type}");
        iconCache[type] = tex;
        return tex;
    }
}