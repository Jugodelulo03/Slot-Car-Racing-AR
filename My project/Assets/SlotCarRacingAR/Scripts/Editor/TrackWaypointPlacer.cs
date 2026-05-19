using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SlotCarRacingAR.Runtime.Features;

namespace SlotCarRacingAR.Editor
{
    /// <summary>
    /// Editor window for placing racing-line waypoints on a 3D track model.
    /// Each waypoint can be assigned a CurveDifficulty so curve zones are
    /// defined manually instead of relying on automatic curvature detection.
    ///
    /// Workflow:
    ///  1. Drag the CIRCUIT.glb prefab into the scene
    ///  2. Open Window → Slot Car Racing → Waypoint Placer
    ///  3. Assign the track model in the window
    ///  4. Click "Start Placing" then click on the track surface to add waypoints
    ///  5. Assign curve difficulty per waypoint (default = Straight)
    ///  6. Click "Export" to save as RacingLineData ScriptableObject
    /// </summary>
    public sealed class TrackWaypointPlacer : EditorWindow
    {
        private GameObject _trackModel;
        private readonly List<Vector3> _waypoints = new();
        private readonly List<CurveDifficulty> _waypointDifficulties = new();
        private bool _isPlacing;
        private Vector2 _scrollPos;
        private int _selectedIndex = -1;
        private float _gizmoSize = 0.3f;
        private readonly List<MeshCollider> _tempColliders = new();
        private CurveDifficulty _brushDifficulty = CurveDifficulty.Straight;

        [MenuItem("Window/Slot Car Racing/Waypoint Placer")]
        private static void ShowWindow()
        {
            var window = GetWindow<TrackWaypointPlacer>("Waypoint Placer");
            window.minSize = new Vector2(300, 400);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            StopPlacing();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Track Waypoint Placer", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _trackModel = (GameObject)EditorGUILayout.ObjectField(
                "Track Model (Scene)", _trackModel, typeof(GameObject), true);

            _gizmoSize = EditorGUILayout.Slider("Gizmo Size", _gizmoSize, 0.01f, 2f);

            EditorGUILayout.Space(8);

            // Placing controls
            EditorGUI.BeginDisabledGroup(_trackModel == null);
            Color origBg = GUI.backgroundColor;

            if (_isPlacing)
            {
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUILayout.Button("■ Stop Placing", GUILayout.Height(30)))
                    StopPlacing();
            }
            else
            {
                GUI.backgroundColor = new Color(0.3f, 1f, 0.3f);
                if (GUILayout.Button("▶ Start Placing (Click on track)", GUILayout.Height(30)))
                    StartPlacing();
            }
            GUI.backgroundColor = origBg;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);

            // ── Curve Difficulty Brush ──
            EditorGUILayout.LabelField("Curve Difficulty Brush", EditorStyles.miniBoldLabel);
            _brushDifficulty = (CurveDifficulty)EditorGUILayout.EnumPopup("New Waypoint Difficulty", _brushDifficulty);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Paint Selected → Brush"))
            {
                if (_selectedIndex >= 0 && _selectedIndex < _waypointDifficulties.Count)
                    _waypointDifficulties[_selectedIndex] = _brushDifficulty;
            }
            if (GUILayout.Button("Paint All → Brush"))
            {
                for (int i = 0; i < _waypointDifficulties.Count; i++)
                    _waypointDifficulties[i] = _brushDifficulty;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Waypoints: {_waypoints.Count}", EditorStyles.miniLabel);

            // Waypoint list
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _waypoints.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                bool selected = _selectedIndex == i;
                if (selected) GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);

                if (GUILayout.Button($"{i}", GUILayout.Width(30)))
                {
                    _selectedIndex = i;
                    SceneView.lastActiveSceneView?.LookAt(_waypoints[i]);
                }

                GUI.backgroundColor = origBg;

                // Difficulty dropdown per waypoint (compact)
                CurveDifficulty diff = _waypointDifficulties[i];
                GUI.backgroundColor = GetDifficultyColor(diff);
                _waypointDifficulties[i] = (CurveDifficulty)EditorGUILayout.EnumPopup(diff, GUILayout.Width(70));
                GUI.backgroundColor = origBg;

                EditorGUILayout.Vector3Field("", _waypoints[i]);

                if (GUILayout.Button("▲", GUILayout.Width(24)) && i > 0)
                {
                    (_waypoints[i], _waypoints[i - 1]) = (_waypoints[i - 1], _waypoints[i]);
                    (_waypointDifficulties[i], _waypointDifficulties[i - 1]) = (_waypointDifficulties[i - 1], _waypointDifficulties[i]);
                    if (_selectedIndex == i) _selectedIndex--;
                }

                if (GUILayout.Button("▼", GUILayout.Width(24)) && i < _waypoints.Count - 1)
                {
                    (_waypoints[i], _waypoints[i + 1]) = (_waypoints[i + 1], _waypoints[i]);
                    (_waypointDifficulties[i], _waypointDifficulties[i + 1]) = (_waypointDifficulties[i + 1], _waypointDifficulties[i]);
                    if (_selectedIndex == i) _selectedIndex++;
                }

                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    _waypoints.RemoveAt(i);
                    _waypointDifficulties.RemoveAt(i);
                    if (_selectedIndex >= _waypoints.Count) _selectedIndex = _waypoints.Count - 1;
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);

            // Actions
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear All") && _waypoints.Count > 0)
            {
                if (EditorUtility.DisplayDialog("Clear Waypoints", "Remove all waypoints?", "Yes", "Cancel"))
                {
                    _waypoints.Clear();
                    _waypointDifficulties.Clear();
                }
            }

            EditorGUI.BeginDisabledGroup(_waypoints.Count < 3);
            if (GUILayout.Button("Export to Asset"))
                ExportToAsset();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Log Waypoints to Console"))
                LogWaypoints();
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

        private void StartPlacing()
        {
            if (_trackModel == null) return;

            // Add temporary MeshColliders to all meshes in the model so raycasts work
            RemoveTempColliders();

            MeshFilter[] meshFilters = _trackModel.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                // Skip if already has a collider
                if (mf.GetComponent<Collider>() != null) continue;

                MeshCollider mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                mc.sharedMesh = mf.sharedMesh;
                _tempColliders.Add(mc);
            }

            _isPlacing = true;
            Debug.Log($"[WaypointPlacer] Placing started. Added {_tempColliders.Count} temporary MeshColliders.");
        }

        private void StopPlacing()
        {
            _isPlacing = false;
            RemoveTempColliders();
        }

        private void RemoveTempColliders()
        {
            foreach (MeshCollider mc in _tempColliders)
            {
                if (mc != null) Undo.DestroyObjectImmediate(mc);
            }
            _tempColliders.Clear();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            // Draw existing waypoints colored by difficulty
            if (_waypoints.Count > 0)
            {
                for (int i = 0; i < _waypoints.Count; i++)
                {
                    float size = _gizmoSize;
                    if (i == _selectedIndex)
                    {
                        Handles.color = Color.cyan;
                        size *= 1.5f;
                    }
                    else if (i == 0)
                    {
                        Handles.color = Color.green;
                    }
                    else
                    {
                        CurveDifficulty diff = i < _waypointDifficulties.Count
                            ? _waypointDifficulties[i]
                            : CurveDifficulty.Straight;
                        Handles.color = GetDifficultyHandleColor(diff);
                    }

                    Handles.SphereHandleCap(0, _waypoints[i], Quaternion.identity, size, EventType.Repaint);
                    string label = i < _waypointDifficulties.Count && _waypointDifficulties[i] != CurveDifficulty.Straight
                        ? $" {i} [{_waypointDifficulties[i]}]"
                        : $" {i}";
                    Handles.Label(_waypoints[i] + Vector3.up * size, label,
                        new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white } });
                }

                // Draw line colored by difficulty
                for (int i = 0; i < _waypoints.Count; i++)
                {
                    int next = (i + 1) % _waypoints.Count;
                    CurveDifficulty diff = i < _waypointDifficulties.Count
                        ? _waypointDifficulties[i]
                        : CurveDifficulty.Straight;
                    Handles.color = GetDifficultyHandleColor(diff);
                    Handles.DrawLine(_waypoints[i], _waypoints[next], 2f);
                }
            }

            // Placing mode
            if (!_isPlacing) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    // Check if we hit the track model or its children
                    if (_trackModel != null && hit.transform.IsChildOf(_trackModel.transform) || hit.transform == _trackModel.transform)
                    {
                        Undo.RecordObject(this, "Add Waypoint");
                        _waypoints.Add(hit.point);
                        _waypointDifficulties.Add(_brushDifficulty);
                        _selectedIndex = _waypoints.Count - 1;
                        e.Use();
                        Repaint();
                        Debug.Log($"[WaypointPlacer] Added waypoint {_waypoints.Count - 1} at {hit.point} [{_brushDifficulty}]");
                    }
                    else
                    {
                        // Hit something else — still add if no track model filter
                        Undo.RecordObject(this, "Add Waypoint");
                        _waypoints.Add(hit.point);
                        _waypointDifficulties.Add(_brushDifficulty);
                        _selectedIndex = _waypoints.Count - 1;
                        e.Use();
                        Repaint();
                        Debug.Log($"[WaypointPlacer] Added waypoint {_waypoints.Count - 1} at {hit.point} (off-model) [{_brushDifficulty}]");
                    }
                }
            }

            // Right-click to undo last
            if (e.type == EventType.MouseDown && e.button == 1 && _waypoints.Count > 0 && !e.alt)
            {
                Undo.RecordObject(this, "Remove Last Waypoint");
                _waypoints.RemoveAt(_waypoints.Count - 1);
                _waypointDifficulties.RemoveAt(_waypointDifficulties.Count - 1);
                _selectedIndex = _waypoints.Count - 1;
                e.Use();
                Repaint();
            }

            sceneView.Repaint();
        }

        private static Color GetDifficultyHandleColor(CurveDifficulty d)
        {
            switch (d)
            {
                case CurveDifficulty.Gentle: return new Color(0.4f, 1f, 0.4f);
                case CurveDifficulty.Medium: return new Color(1f, 1f, 0.2f);
                case CurveDifficulty.Sharp: return new Color(1f, 0.5f, 0f);
                case CurveDifficulty.Hairpin: return new Color(1f, 0.15f, 0.15f);
                default: return Color.yellow; // Straight
            }
        }

        private void ExportToAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Racing Line", "RacingLine", "asset",
                "Choose where to save the racing line data.");

            if (string.IsNullOrEmpty(path)) return;

            // Compute normalised waypoints relative to model bounds
            Vector3 modelCenter = Vector3.zero;
            Vector3 modelSize = Vector3.one;

            if (_trackModel != null)
            {
                Renderer[] renderers = _trackModel.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        b.Encapsulate(renderers[i].bounds);
                    modelCenter = b.center;
                    modelSize = b.size;
                }
            }

            var asset = ScriptableObject.CreateInstance<SlotCarRacingAR.Runtime.Features.RacingLineData>();
            asset.OriginalModelSize = modelSize;

            // Store waypoints relative to model center, normalised by max extent
            float maxExtent = Mathf.Max(modelSize.x, modelSize.y, modelSize.z);
            if (maxExtent < 0.0001f) maxExtent = 1f;

            asset.Waypoints = new Vector3[_waypoints.Count];
            asset.WaypointDifficulties = new SlotCarRacingAR.Runtime.Features.CurveDifficulty[_waypoints.Count];
            for (int i = 0; i < _waypoints.Count; i++)
            {
                Vector3 relative = _waypoints[i] - modelCenter;
                asset.Waypoints[i] = relative / maxExtent;
                asset.WaypointDifficulties[i] = _waypointDifficulties[i];
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            Debug.Log($"[WaypointPlacer] Exported {_waypoints.Count} waypoints to {path}. " +
                      $"Model size: {modelSize}, normalised by {maxExtent:F3}");
        }

        private void LogWaypoints()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// {_waypoints.Count} waypoints (world space)");
            for (int i = 0; i < _waypoints.Count; i++)
            {
                Vector3 p = _waypoints[i];
                sb.AppendLine($"new Vector3({p.x:F4}f, {p.y:F4}f, {p.z:F4}f), // {i}");
            }
            Debug.Log(sb.ToString());
        }
    }
}
