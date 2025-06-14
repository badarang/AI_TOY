using System;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(StageData))]
public class StageDataEditor : OdinEditor
{
    private Vector2Int contextMenuPos; // 우클릭 위치 저장
    private Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();
    private Vector2 scrollPosition;
    private bool showAdvancedOptions = false;
    private bool showGridSettings = true;
    private bool showUnitSettings = true;

    // 스타일 상수
    private const int CELL_SIZE = 42;
    private const int CELL_PADDING = 5;
    private const int ICON_SIZE = 32;

    // 색상 팔레트
    private static readonly Color PLAYER_COLOR = new Color(0.2f, 0.8f, 0.2f, 1f);
    private static readonly Color ENEMY_COLOR = new Color(0.9f, 0.3f, 0.3f, 1f);
    private static readonly Color HOVER_COLOR = new Color(0.3f, 0.6f, 1f, 0.15f);
    private static readonly Color SELECTED_COLOR = new Color(0.3f, 0.6f, 1f, 0.25f);

    public override void OnInspectorGUI()
    {
        StageData data = (StageData)target;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        GUILayout.Space(10);

        DrawGridSettings(data);
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
    }

    private new void DrawHeader()
    {
        EditorGUILayout.BeginVertical(CreateBoxStyle());

        var headerStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        GUILayout.Label("🎮 Stage Data Editor", headerStyle);

        var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize = 11
        };
        GUILayout.Label("Grid-based level editor for Unity", subtitleStyle);

        EditorGUILayout.EndVertical();
    }

    private void DrawGridSettings(StageData data)
    {
        // Foldout Header 그룹 시작
        showGridSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGridSettings, "🗺️ Grid Settings");
        if (showGridSettings)
        {
            EditorGUILayout.BeginVertical(CreateBoxStyle());

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Map Size", EditorStyles.boldLabel, GUILayout.Width(70));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Width:", GUILayout.Width(45));
            data.width = EditorGUILayout.IntSlider(data.width, 3, 20, GUILayout.Width(150));
            GUILayout.Label($"({data.width})", EditorStyles.miniLabel, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Height:", GUILayout.Width(45));
            data.height = EditorGUILayout.IntSlider(data.height, 3, 20, GUILayout.Width(150));
            GUILayout.Label($"({data.height})", EditorStyles.miniLabel, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            var infoStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 10,
                normal = { textColor = Color.gray }
            };
            GUILayout.Label($"Total Cells: {data.width * data.height} | Right-click on grid to add/remove units", infoStyle);

            EditorGUILayout.EndVertical();
        }
        // Foldout Header 그룹 종료
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawMainGrid(StageData data)
    {
        var gridStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 10, 10)
        };
        EditorGUILayout.BeginVertical(gridStyle);

        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        GUILayout.Label("🎯 Stage Grid", titleStyle);

        GUILayout.Space(8);

        // 계산: 그리드 전체 사이즈
        float totalWidth = data.width * (CELL_SIZE + CELL_PADDING) + CELL_PADDING;
        float totalHeight = data.height * (CELL_SIZE + CELL_PADDING) + CELL_PADDING;
        float inspectorWidth = EditorGUIUtility.currentViewWidth - 36f; // 오딘/유니티 내부 여백 감안
        float gridStartX = Mathf.Max((inspectorWidth - totalWidth) / 2f, 0f);
        float gridStartY = 12f;

        // 전체 박스 영역
        Rect bgRect = GUILayoutUtility.GetRect(inspectorWidth, totalHeight + 32f);
        EditorGUI.DrawRect(bgRect, EditorGUIUtility.isProSkin ? new Color(0.20f, 0.20f, 0.22f, 1f) : new Color(0.94f, 0.94f, 0.96f, 1f));

        // 그리드 영역만 별도 배경
        Rect gridRect = new Rect(bgRect.x + gridStartX, bgRect.y + gridStartY, totalWidth, totalHeight);
        EditorGUI.DrawRect(gridRect, EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 1f) : new Color(0.97f, 0.97f, 0.99f, 1f));

        // 실제 그리드 렌더링
        RenderGrid(data, gridRect);

        DrawLegend();
        EditorGUILayout.EndVertical();
    }

    private void RenderGrid(StageData data, Rect gridRect)
    {
        Color borderCol = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0.7f, 0.7f, 0.7f, 1f);
        Color gridLineCol = EditorGUIUtility.isProSkin ? new Color(0.35f, 0.35f, 0.35f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f);

        float gridStartX = gridRect.x + CELL_PADDING;
        float gridStartY = gridRect.y + CELL_PADDING;
        Event e = Event.current;

        Vector2Int hoveredCell = GetHoveredCell(data, gridStartX, gridStartY, e.mousePosition);

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                Vector2Int pos = new Vector2Int(x, data.height - y - 1);
                bool isPlayer = data.playerSpawn == pos;
                var enemy = data.enemySpawns.FirstOrDefault(es => es.spawnPos == pos);
                var obstacle = data.obstacleSpawns.FirstOrDefault(os => os.spawnPos == pos);
                bool isHovered = hoveredCell == new Vector2Int(x, y);

                float px = gridStartX + x * (CELL_SIZE + CELL_PADDING);
                float py = gridStartY + y * (CELL_SIZE + CELL_PADDING);
                Rect cellRect = new Rect(px, py, CELL_SIZE, CELL_SIZE);

                Color cellBg = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 1f) : new Color(0.97f, 0.97f, 0.99f, 1f);
                if (isPlayer) cellBg = Color.Lerp(cellBg, PLAYER_COLOR, 0.10f);
                else if (enemy != null) cellBg = Color.Lerp(cellBg, ENEMY_COLOR, 0.10f);
                else if (obstacle != null) cellBg = Color.Lerp(cellBg, new Color(0.5f, 0.3f, 0.1f, 1f), 0.20f); // 장애물 색상

                EditorGUI.DrawRect(cellRect, cellBg);
                if (isHovered)
                    EditorGUI.DrawRect(cellRect, HOVER_COLOR);

                DrawCellBorder(cellRect, borderCol, isPlayer, enemy != null);
                RenderUnit(cellRect, isPlayer, enemy, obstacle, data.playerType);

                if (isHovered)
                {
                    var coordStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 8,
                        normal = { textColor = Color.gray }
                    };
                    var coordRect = new Rect(cellRect.x + 2, cellRect.y + 2, 20, 10);
                    GUI.Label(coordRect, $"{pos.x},{pos.y}", coordStyle);
                }

                HandleContextMenu(data, cellRect, pos, isPlayer, enemy, obstacle, e);
            }
        }

        DrawGridLines(gridRect, data, gridStartX, gridStartY, gridLineCol);
    }

    private void DrawCellBorder(Rect cellRect, Color borderCol, bool isPlayer, bool hasEnemy)
    {
        Color finalBorderCol = borderCol;

        if (isPlayer)
        {
            finalBorderCol = PLAYER_COLOR;
        }
        else if (hasEnemy)
        {
            finalBorderCol = ENEMY_COLOR;
        }

        Handles.BeginGUI();
        Handles.color = finalBorderCol;
        Handles.DrawSolidRectangleWithOutline(
            new Vector3[] {
                new Vector3(cellRect.x, cellRect.y),
                new Vector3(cellRect.xMax, cellRect.y),
                new Vector3(cellRect.xMax, cellRect.yMax),
                new Vector3(cellRect.x, cellRect.yMax),
            },
            Color.clear,
            finalBorderCol
        );
        Handles.EndGUI();
    }

    private void DrawGridLines(Rect gridRect, StageData data, float startX, float startY, Color gridLineCol)
    {
        Handles.BeginGUI();
        Handles.color = gridLineCol;

        for (int x = 0; x <= data.width; x++)
        {
            float px = startX + x * (CELL_SIZE + CELL_PADDING) - CELL_PADDING / 2;
            Handles.DrawLine(
                new Vector3(px, gridRect.y),
                new Vector3(px, gridRect.yMax)
            );
        }
        for (int y = 0; y <= data.height; y++)
        {
            float py = startY + y * (CELL_SIZE + CELL_PADDING) - CELL_PADDING / 2;
            Handles.DrawLine(
                new Vector3(gridRect.x, py),
                new Vector3(gridRect.xMax, py)
            );
        }

        Handles.EndGUI();
    }

    private Vector2Int GetHoveredCell(StageData data, float startX, float startY, Vector2 mousePos)
    {
        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                float px = startX + x * (CELL_SIZE + CELL_PADDING);
                float py = startY + y * (CELL_SIZE + CELL_PADDING);
                Rect cell = new Rect(px, py, CELL_SIZE, CELL_SIZE);
                if (cell.Contains(mousePos))
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    private void RenderUnit(Rect cellRect, bool isPlayer, EnemySpawnData enemy, ObstacleSpawnData obstacle, UnitType playerType)
    {
        if (isPlayer)
        {
            RenderPlayerUnit(cellRect, playerType);
        }
        else if (enemy != null)
        {
            RenderEnemyUnit(cellRect, enemy.enemyType);
        }
        else if (obstacle != null && obstacle.obstacleData != null)
        {
            RenderObstacleUnit(cellRect, obstacle.obstacleData);
        }
    }

    private void RenderPlayerUnit(Rect cellRect, UnitType playerType)
    {
        string typeName = playerType.ToString();
        Texture2D icon = GetUnitIcon(typeName);

        if (icon)
        {
            var iconRect = new Rect(
                cellRect.x + (cellRect.width - ICON_SIZE) / 2,
                cellRect.y + (cellRect.height - ICON_SIZE) / 2,
                ICON_SIZE, ICON_SIZE
            );

            DrawIconBackground(iconRect, PLAYER_COLOR);
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
            var iconRect = new Rect(
                cellRect.x + (cellRect.width - ICON_SIZE) / 2,
                cellRect.y + (cellRect.height - ICON_SIZE) / 2,
                ICON_SIZE, ICON_SIZE
            );

            DrawIconBackground(iconRect, ENEMY_COLOR);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
        }
        else
        {
            DrawUnitLabel(cellRect, GetShortName(typeName), ENEMY_COLOR, "👹");
        }
    }

    private void RenderObstacleUnit(Rect cellRect, ObstacleData obstacleData)
    {
        // 장애물 아이콘 또는 이모지 표시
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.5f, 0.3f, 0.1f, 1f) }
        };
        var emojiRect = new Rect(cellRect.x, cellRect.y + 2, cellRect.width, cellRect.height - 4);
        GUI.Label(emojiRect, "🌲", style);
        // 장애물 이름도 작게 표시
        var nameStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.LowerCenter,
            fontSize = 8,
            normal = { textColor = new Color(0.3f, 0.2f, 0.1f, 1f) }
        };
        var nameRect = new Rect(cellRect.x, cellRect.yMax - 12, cellRect.width, 12);
        GUI.Label(nameRect, obstacleData.obstacleName, nameStyle);
    }

    private void DrawIconBackground(Rect iconRect, Color color)
    {
        var bgRect = new Rect(iconRect.x - 2, iconRect.y - 2, iconRect.width + 4, iconRect.height + 4);
        EditorGUI.DrawRect(bgRect, Color.Lerp(color, Color.white, 0.8f));

        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawSolidArc(bgRect.center, Vector3.forward, Vector3.up, 360f, bgRect.width / 2);
        Handles.EndGUI();
    }

    private void DrawUnitLabel(Rect cellRect, string shortName, Color color, string emoji)
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = color }
        };

        var emojiStyle = new GUIStyle(style) { fontSize = 16 };
        var emojiRect = new Rect(cellRect.x, cellRect.y + 2, cellRect.width, cellRect.height / 2);
        GUI.Label(emojiRect, emoji, emojiStyle);

        var textRect = new Rect(cellRect.x, cellRect.y + cellRect.height / 2 - 2, cellRect.width, cellRect.height / 2);
        GUI.Label(textRect, shortName, style);
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

    private void DrawUnitSettings(StageData data)
    {
        // Foldout Header 그룹 시작 (여기만 사용)
        showUnitSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showUnitSettings, "⚔️ Unit Settings");
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

    private void DrawPlayerSettings(StageData data)
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUILayout.Label("👤 Player Configuration", headerStyle);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Type:", GUILayout.Width(50));
        data.playerType = (UnitType)EditorGUILayout.EnumPopup(data.playerType, GUILayout.Width(150));

        GUILayout.Space(20);
        GUILayout.Label("Spawn Position:", GUILayout.Width(100));
        data.playerSpawn = EditorGUILayout.Vector2IntField(GUIContent.none, data.playerSpawn, GUILayout.Width(80));

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEnemySettings(StageData data)
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUILayout.Label("👹 Enemy Configuration", headerStyle);

        serializedObject.Update();
        var enemySpawnsProp = serializedObject.FindProperty("enemySpawns");
        EditorGUILayout.PropertyField(enemySpawnsProp, new GUIContent("Enemy Spawns"), true);
        serializedObject.ApplyModifiedProperties();

        if (data.enemySpawns != null && data.enemySpawns.Length > 0)
        {
            EditorGUILayout.Space(5);
            var statsStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 10 };
            var enemyTypes = data.enemySpawns.GroupBy(e => e.enemyType).Select(g => $"{g.Key}: {g.Count()}");
            GUILayout.Label($"Total Enemies: {data.enemySpawns.Length} ({string.Join(", ", enemyTypes)})", statsStyle);
        }
    }

    private void DrawObstacleSettings(StageData data)
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUILayout.Label("🌲 Obstacle Configuration", headerStyle);

        serializedObject.Update();
        var obstacleSpawnsProp = serializedObject.FindProperty("obstacleSpawns");
        EditorGUILayout.PropertyField(obstacleSpawnsProp, new GUIContent("Obstacle Spawns"), true);
        serializedObject.ApplyModifiedProperties();

        if (data.obstacleSpawns != null && data.obstacleSpawns.Length > 0)
        {
            EditorGUILayout.Space(5);
            var statsStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 10 };
            GUILayout.Label($"Total Obstacles: {data.obstacleSpawns.Length}", statsStyle);
        }
    }

    private void DrawAdvancedOptions(StageData data)
    {
        // Foldout Header 그룹 시작
        showAdvancedOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showAdvancedOptions, "⚙️ Advanced Options");
        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical(CreateBoxStyle());

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🧹 Clear All Enemies", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear All Enemies", "Are you sure you want to remove all enemies?", "Yes", "Cancel"))
                {
                    Undo.RecordObject(data, "Clear All Enemies");
                    data.enemySpawns = new EnemySpawnData[0];
                    EditorUtility.SetDirty(data);
                }
            }

            if (GUILayout.Button("🎯 Center Player", GUILayout.Height(25)))
            {
                Undo.RecordObject(data, "Center Player");
                data.playerSpawn = new Vector2Int(data.width / 2, data.height / 2);
                EditorUtility.SetDirty(data);
            }

            if (GUILayout.Button("🔄 Randomize Enemies", GUILayout.Height(25)))
            {
                RandomizeEnemies(data);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        // Foldout Header 그룹 종료
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void RandomizeEnemies(StageData data)
    {
        if (EditorUtility.DisplayDialog("Randomize Enemies", "This will replace all current enemies with random ones. Continue?", "Yes", "Cancel"))
        {
            Undo.RecordObject(data, "Randomize Enemies");

            var enemyTypes = System.Enum.GetValues(typeof(UnitType)).Cast<UnitType>()
                .Where(t => t.ToString().StartsWith("Enemy_")).ToArray();

            var newEnemies = new List<EnemySpawnData>();
            int enemyCount = UnityEngine.Random.Range(1, Mathf.Min(5, data.width * data.height / 4));

            for (int i = 0; i < enemyCount; i++)
            {
                Vector2Int pos;
                do
                {
                    pos = new Vector2Int(UnityEngine.Random.Range(0, data.width), UnityEngine.Random.Range(0, data.height));
                } while (pos == data.playerSpawn || newEnemies.Any(e => e.spawnPos == pos));

                newEnemies.Add(new EnemySpawnData
                {
                    enemyType = enemyTypes[UnityEngine.Random.Range(0, enemyTypes.Length)],
                    spawnPos = pos
                });
            }

            data.enemySpawns = newEnemies.ToArray();
            EditorUtility.SetDirty(data);
        }
    }

    private void HandleContextMenu(StageData data, Rect cellRect, Vector2Int pos, bool isPlayer, EnemySpawnData enemy, ObstacleSpawnData obstacle, Event e)
    {
        if (e.type == EventType.ContextClick && cellRect.Contains(e.mousePosition))
        {
            contextMenuPos = pos;
            GenericMenu menu = new GenericMenu();

            // 플레이어
            if (!isPlayer && !data.enemySpawns.Any(es => es.spawnPos == pos) && !data.obstacleSpawns.Any(os => os.spawnPos == pos))
            {
                menu.AddItem(new GUIContent("👤 Add Player Here"), false, () =>
                {
                    Undo.RecordObject(data, "Set Player Spawn");
                    data.playerSpawn = contextMenuPos;
                    EditorUtility.SetDirty(data);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("👤 Player Already Here or Occupied"));
            }

            menu.AddSeparator("");

            // 적
            if (enemy == null && !isPlayer && !data.obstacleSpawns.Any(os => os.spawnPos == pos))
            {
                var enemyTypes = System.Enum.GetValues(typeof(UnitType)).Cast<UnitType>()
                    .Where(t => t.ToString().StartsWith("Enemy_")).ToArray();

                foreach (var enemyType in enemyTypes)
                {
                    var type = enemyType;
                    menu.AddItem(new GUIContent($"👹 Add {type}"), false, () =>
                    {
                        Undo.RecordObject(data, $"Add {type}");
                        var list = data.enemySpawns.ToList();
                        list.Add(new EnemySpawnData { enemyType = type, spawnPos = contextMenuPos });
                        data.enemySpawns = list.ToArray();
                        EditorUtility.SetDirty(data);
                    });
                }
            }
            else if (enemy != null)
            {
                menu.AddItem(new GUIContent($"🗑️ Remove {enemy.enemyType}"), false, () =>
                {
                    Undo.RecordObject(data, "Remove Enemy");
                    data.enemySpawns = data.enemySpawns.Where(e => e.spawnPos != contextMenuPos).ToArray();
                    EditorUtility.SetDirty(data);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("👹 Enemy Slot Occupied"));
            }

            // 장애물
            var obsIdx = Array.FindIndex(data.obstacleSpawns, o => o.spawnPos == contextMenuPos);
            if (obsIdx < 0 && !isPlayer && !data.enemySpawns.Any(es => es.spawnPos == pos))
            {
                // 현재 위치에 장애물이 없으면 추가 메뉴
                var guids = AssetDatabase.FindAssets("t:ObstacleData");
                if (guids.Length > 0)
                {
                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var obsData = AssetDatabase.LoadAssetAtPath<ObstacleData>(path);
                        if (obsData == null) continue;
                        menu.AddItem(new GUIContent($"🌲 Add Obstacle/{obsData.obstacleName}"), false, () =>
                        {
                            Undo.RecordObject(data, $"Add Obstacle {obsData.obstacleName}");
                            var list = data.obstacleSpawns.ToList();
                            list.Add(new ObstacleSpawnData { obstacleData = obsData, spawnPos = contextMenuPos });
                            data.obstacleSpawns = list.ToArray();
                            EditorUtility.SetDirty(data);
                        });
                    }
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("🌲 No ObstacleData found"));
                }
            }
            else if (obsIdx >= 0)
            {
                // 이미 장애물이 있으면 삭제 메뉴
                var obsName = data.obstacleSpawns[obsIdx].obstacleData != null ? data.obstacleSpawns[obsIdx].obstacleData.obstacleName : "Obstacle";
                menu.AddItem(new GUIContent($"🗑️ Remove Obstacle ({obsName})"), false, () =>
                {
                    Undo.RecordObject(data, "Remove Obstacle");
                    data.obstacleSpawns = data.obstacleSpawns.Where((o, i) => i != obsIdx).ToArray();
                    EditorUtility.SetDirty(data);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("🌲 Obstacle Slot Occupied"));
            }

            menu.ShowAsContext();
            e.Use();
        }
    }

    private GUIStyle CreateBoxStyle()
    {
        var style = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(8, 8, 8, 8),
            margin = new RectOffset(0, 0, 2, 2)
        };
        return style;
    }

    private string GetShortName(string type)
    {
        if (string.IsNullOrEmpty(type)) return "?";
        int idx = type.IndexOf('_');
        if (idx >= 0 && type.Length >= idx + 3)
            return type.Substring(idx + 1, 2);
        else if (type.Length >= 2)
            return type.Substring(0, 2);
        return type.Length > 0 ? type.Substring(0, 1) : "?";
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