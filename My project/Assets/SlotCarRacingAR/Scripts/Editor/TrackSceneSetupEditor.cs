using UnityEditor;
using UnityEngine;
using SlotCarRacingAR.Runtime.Features;

namespace SlotCarRacingAR.Editor
{
    /// <summary>
    /// Custom editor for TrackSceneSetup that allows placing waypoints
    /// directly on the 3D model in the scene view by clicking.
    /// Includes per-waypoint CurveDifficulty assignment and import/export
    /// from RacingLineData assets.
    /// </summary>
    [CustomEditor(typeof(TrackSceneSetup))]
    public sealed class TrackSceneSetupEditor : UnityEditor.Editor
    {
        private bool _isPlacing;
        private RacingLineData _importSource;
        private CurveDifficulty _brushDifficulty = CurveDifficulty.Straight;
        private Vector2 _scrollPos;
        private bool _showWaypointList = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TrackSceneSetup setup = (TrackSceneSetup)target;
            Transform pathParent = GetPathParent(setup);
            int wpCount = setup.WaypointCount;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Waypoints: {wpCount}", EditorStyles.boldLabel);

            EditorGUILayout.Space(4);

            // ── Placing controls ──
            Color origBg = GUI.backgroundColor;
            if (_isPlacing)
            {
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUILayout.Button("■ Stop Placing Waypoints", GUILayout.Height(28)))
                {
                    _isPlacing = false;
                    RemoveTempColliders(setup);
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.3f, 1f, 0.3f);
                if (GUILayout.Button("▶ Start Placing Waypoints (Click on track)", GUILayout.Height(28)))
                {
                    _isPlacing = true;
                    AddTempColliders(setup);
                }
            }
            GUI.backgroundColor = origBg;

            EditorGUILayout.Space(8);

            // ── Import from RacingLineData ──
            EditorGUILayout.LabelField("Import / Export", EditorStyles.miniBoldLabel);
            _importSource = (RacingLineData)EditorGUILayout.ObjectField(
                "RacingLineData Asset", _importSource, typeof(RacingLineData), false);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(_importSource == null);
            if (GUILayout.Button("Import Difficulties from Asset"))
            {
                ImportDifficultiesFromAsset(setup, pathParent);
            }
            if (GUILayout.Button("Import Waypoints + Difficulties"))
            {
                ImportFullFromAsset(setup, pathParent);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(wpCount < 3);
            if (GUILayout.Button("Export Difficulties to RacingLineData Asset"))
            {
                ExportDifficultiesToAsset(setup);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);

            // ── Curve Difficulty Brush ──
            EditorGUILayout.LabelField("Curve Difficulty Brush", EditorStyles.miniBoldLabel);
            _brushDifficulty = (CurveDifficulty)EditorGUILayout.EnumPopup("Brush", _brushDifficulty);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(wpCount == 0);
            if (GUILayout.Button("Paint All → Brush"))
            {
                PaintAllDifficulties(setup, _brushDifficulty);
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Sync Array Size"))
            {
                SyncDifficultyArraySize(setup);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // ── Per-waypoint difficulty list ──
            if (wpCount > 0 && pathParent != null)
            {
                _showWaypointList = EditorGUILayout.Foldout(_showWaypointList, $"Waypoint Difficulties ({wpCount})", true);
                if (_showWaypointList)
                {
                    SerializedProperty diffProp = serializedObject.FindProperty("_waypointDifficulties");
                    SyncSerializedArraySize(diffProp, wpCount);

                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(300));
                    for (int i = 0; i < wpCount; i++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        // Index button — click to focus in scene
                        if (GUILayout.Button($"{i}", GUILayout.Width(30)))
                        {
                            Transform wp = pathParent.GetChild(i);
                            SceneView.lastActiveSceneView?.LookAt(wp.position);
                            Selection.activeTransform = wp;
                        }

                        // Difficulty dropdown with color
                        if (i < diffProp.arraySize)
                        {
                            SerializedProperty elem = diffProp.GetArrayElementAtIndex(i);
                            CurveDifficulty current = (CurveDifficulty)elem.enumValueIndex;
                            GUI.backgroundColor = GetDifficultyColor(current);
                            CurveDifficulty newVal = (CurveDifficulty)EditorGUILayout.EnumPopup(current, GUILayout.Width(80));
                            GUI.backgroundColor = origBg;
                            if (newVal != current)
                                elem.enumValueIndex = (int)newVal;
                        }

                        // Waypoint name
                        if (pathParent.childCount > i)
                        {
                            EditorGUILayout.LabelField(pathParent.GetChild(i).name, GUILayout.ExpandWidth(true));
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space(4);

            // ── Utility ──
            if (pathParent != null && pathParent.childCount > 0)
            {
                if (GUILayout.Button("Clear All Waypoints"))
                {
                    if (EditorUtility.DisplayDialog("Clear", "Remove all waypoints?", "Yes", "Cancel"))
                    {
                        Undo.RecordObject(pathParent.gameObject, "Clear Waypoints");
                        while (pathParent.childCount > 0)
                            Undo.DestroyObjectImmediate(pathParent.GetChild(0).gameObject);
                        ClearDifficulties(setup);
                    }
                }
            }

            if (GUILayout.Button("Create Path Child (if missing)"))
            {
                EnsurePathParent(setup);
            }
        }

        private void OnSceneGUI()
        {
            if (!_isPlacing) return;

            TrackSceneSetup setup = (TrackSceneSetup)target;
            Transform pathParent = GetPathParent(setup);
            if (pathParent == null) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            Event e = Event.current;

            // Left-click to add waypoint
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    GameObject wp = new GameObject($"WP_{pathParent.childCount:D3}");
                    Undo.RegisterCreatedObjectUndo(wp, "Add Waypoint");
                    wp.transform.SetParent(pathParent, true);
                    wp.transform.position = hit.point;

                    // Expand difficulty array
                    AppendDifficulty(setup, _brushDifficulty);

                    e.Use();
                }
            }

            // Right-click to undo last
            if (e.type == EventType.MouseDown && e.button == 1 && !e.alt && pathParent.childCount > 0)
            {
                Undo.DestroyObjectImmediate(pathParent.GetChild(pathParent.childCount - 1).gameObject);
                TrimLastDifficulty(setup);
                e.Use();
            }

            SceneView.currentDrawingSceneView?.Repaint();
        }

        // ── Import / Export ──

        private void ImportDifficultiesFromAsset(TrackSceneSetup setup, Transform pathParent)
        {
            if (_importSource == null || !_importSource.HasManualCurveData) 
            {
                EditorUtility.DisplayDialog("Import", "El asset no tiene datos de dificultad manual.", "OK");
                return;
            }

            int wpCount = pathParent != null ? pathParent.childCount : 0;
            CurveDifficulty[] source = _importSource.WaypointDifficulties;

            serializedObject.Update();
            SerializedProperty diffProp = serializedObject.FindProperty("_waypointDifficulties");
            int count = Mathf.Min(source.Length, wpCount > 0 ? wpCount : source.Length);
            diffProp.arraySize = count;
            for (int i = 0; i < count; i++)
                diffProp.GetArrayElementAtIndex(i).enumValueIndex = (int)source[i];
            serializedObject.ApplyModifiedProperties();

            Debug.Log($"[TrackSceneSetup] Imported {count} difficulties from {_importSource.name}.");
        }

        private void ImportFullFromAsset(TrackSceneSetup setup, Transform pathParent)
        {
            if (_importSource == null || _importSource.Waypoints.Length < 3)
            {
                EditorUtility.DisplayDialog("Import", "El asset no tiene suficientes waypoints (mínimo 3).", "OK");
                return;
            }

            if (pathParent == null)
            {
                EnsurePathParent(setup);
                pathParent = GetPathParent(setup);
                if (pathParent == null) return;
            }

            // Clear existing
            while (pathParent.childCount > 0)
                Undo.DestroyObjectImmediate(pathParent.GetChild(0).gameObject);

            // Get model bounds for denormalization
            Renderer[] renderers = setup.GetComponentsInChildren<Renderer>();
            Vector3 modelCenter = Vector3.zero;
            float maxExtent = 1f;
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                modelCenter = b.center;
                maxExtent = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (maxExtent < 0.001f) maxExtent = 1f;
            }

            // Create waypoints from normalised positions
            Vector3[] srcWp = _importSource.Waypoints;
            for (int i = 0; i < srcWp.Length; i++)
            {
                GameObject wp = new GameObject($"WP_{i:D3}");
                Undo.RegisterCreatedObjectUndo(wp, "Import Waypoint");
                wp.transform.SetParent(pathParent, true);
                wp.transform.position = modelCenter + srcWp[i] * maxExtent;
            }

            // Import difficulties
            serializedObject.Update();
            SerializedProperty diffProp = serializedObject.FindProperty("_waypointDifficulties");
            diffProp.arraySize = srcWp.Length;

            if (_importSource.HasManualCurveData)
            {
                for (int i = 0; i < srcWp.Length; i++)
                    diffProp.GetArrayElementAtIndex(i).enumValueIndex = (int)_importSource.WaypointDifficulties[i];
            }
            else
            {
                for (int i = 0; i < srcWp.Length; i++)
                    diffProp.GetArrayElementAtIndex(i).enumValueIndex = (int)CurveDifficulty.Straight;
            }
            serializedObject.ApplyModifiedProperties();

            Debug.Log($"[TrackSceneSetup] Imported {srcWp.Length} waypoints + difficulties from {_importSource.name}.");
        }

        private void ExportDifficultiesToAsset(TrackSceneSetup setup)
        {
            // If an import source is assigned, update it in place
            RacingLineData targetAsset = _importSource;

            if (targetAsset == null)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Racing Line", "RacingLine", "asset",
                    "Choose where to save the racing line data.");
                if (string.IsNullOrEmpty(path)) return;

                targetAsset = ScriptableObject.CreateInstance<RacingLineData>();
                AssetDatabase.CreateAsset(targetAsset, path);
            }

            Transform pathParent = GetPathParent(setup);
            int count = pathParent != null ? pathParent.childCount : 0;

            // Compute normalised waypoints
            Renderer[] renderers = setup.GetComponentsInChildren<Renderer>();
            Vector3 modelCenter = Vector3.zero;
            float maxExtent = 1f;
            Vector3 modelSize = Vector3.one;
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                modelCenter = b.center;
                modelSize = b.size;
                maxExtent = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (maxExtent < 0.001f) maxExtent = 1f;
            }

            Undo.RecordObject(targetAsset, "Export Difficulties");
            targetAsset.OriginalModelSize = modelSize;
            targetAsset.Waypoints = new Vector3[count];
            targetAsset.WaypointDifficulties = new CurveDifficulty[count];

            SerializedProperty diffProp = serializedObject.FindProperty("_waypointDifficulties");

            for (int i = 0; i < count; i++)
            {
                Vector3 worldPos = pathParent.GetChild(i).position;
                targetAsset.Waypoints[i] = (worldPos - modelCenter) / maxExtent;
                targetAsset.WaypointDifficulties[i] = i < diffProp.arraySize
                    ? (CurveDifficulty)diffProp.GetArrayElementAtIndex(i).enumValueIndex
                    : CurveDifficulty.Straight;
            }

            EditorUtility.SetDirty(targetAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TrackSceneSetup] Exported {count} waypoints + difficulties to {AssetDatabase.GetAssetPath(targetAsset)}.");
        }

        // ── Helpers ──

        private void PaintAllDifficulties(TrackSceneSetup setup, CurveDifficulty difficulty)
        {
            serializedObject.Update();
            SerializedProperty diffProp = serializedObject.FindProperty("_waypointDifficulties");
            int count = setup.WaypointCount;
            diffProp.arraySize = count;
            for (int i = 0; i < count; i++)
                diffProp.GetArrayElementAtIndex(i).enumValueIndex = (int)difficulty;
            serializedObject.ApplyModifiedProperties();
        }

        private void SyncDifficultyArraySize(TrackSceneSetup setup)
        {
            serializedObject.Update();
            SerializedProperty diffProp = serializedObject.FindProperty("_waypointDifficulties");
            SyncSerializedArraySize(diffProp, setup.WaypointCount);
            serializedObject.ApplyModifiedProperties();
        }

        private void ClearDifficulties(TrackSceneSetup setup)
        {
            serializedObject.Update();
            SerializedProperty diffProp = serializedObject.FindProperty("_waypointDifficulties");
            diffProp.arraySize = 0;
            serializedObject.ApplyModifiedProperties();
        }

        private void AppendDifficulty(TrackSceneSetup setup, CurveDifficulty difficulty)
        {
            serializedObject.Update();
            SerializedProperty diffProp = serializedObject.FindProperty("_waypointDifficulties");
            int newSize = diffProp.arraySize + 1;
            diffProp.arraySize = newSize;
            diffProp.GetArrayElementAtIndex(newSize - 1).enumValueIndex = (int)difficulty;
            serializedObject.ApplyModifiedProperties();
        }

        private void TrimLastDifficulty(TrackSceneSetup setup)
        {
            serializedObject.Update();
            SerializedProperty diffProp = serializedObject.FindProperty("_waypointDifficulties");
            if (diffProp.arraySize > 0)
                diffProp.arraySize--;
            serializedObject.ApplyModifiedProperties();
        }

        private static void SyncSerializedArraySize(SerializedProperty arrayProp, int targetSize)
        {
            if (arrayProp.arraySize != targetSize)
                arrayProp.arraySize = targetSize;
        }

        private static Color GetDifficultyColor(CurveDifficulty d)
        {
            switch (d)
            {
                case CurveDifficulty.Gentle: return new Color(0.6f, 1f, 0.6f);
                case CurveDifficulty.Medium: return new Color(1f, 1f, 0.4f);
                case CurveDifficulty.Sharp: return new Color(1f, 0.6f, 0.3f);
                case CurveDifficulty.Hairpin: return new Color(1f, 0.3f, 0.3f);
                default: return Color.white;
            }
        }

        private Transform GetPathParent(TrackSceneSetup setup)
        {
            SerializedProperty prop = serializedObject.FindProperty("_pathParent");
            if (prop != null && prop.objectReferenceValue != null)
                return prop.objectReferenceValue as Transform;
            return null;
        }

        private void EnsurePathParent(TrackSceneSetup setup)
        {
            Transform existing = setup.transform.Find("Path");
            if (existing == null)
            {
                GameObject pathObj = new GameObject("Path");
                Undo.RegisterCreatedObjectUndo(pathObj, "Create Path");
                pathObj.transform.SetParent(setup.transform, false);
                pathObj.transform.localPosition = Vector3.zero;
                existing = pathObj.transform;
            }

            SerializedProperty prop = serializedObject.FindProperty("_pathParent");
            if (prop != null)
            {
                serializedObject.Update();
                prop.objectReferenceValue = existing;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void AddTempColliders(TrackSceneSetup setup)
        {
            MeshFilter[] meshFilters = setup.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponent<Collider>() != null) continue;
                MeshCollider mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        private void RemoveTempColliders(TrackSceneSetup setup)
        {
            MeshCollider[] colliders = setup.GetComponentsInChildren<MeshCollider>();
            foreach (MeshCollider mc in colliders)
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(mc))
                    Undo.DestroyObjectImmediate(mc);
            }
        }
    }
}
