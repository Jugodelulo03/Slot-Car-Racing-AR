using UnityEditor;
using UnityEngine;
using SlotCarRacingAR.Runtime.Features;

namespace SlotCarRacingAR.Editor
{
    /// <summary>
    /// Custom editor for TrackSceneSetup that allows placing waypoints
    /// directly on the 3D model in the scene view by clicking.
    /// </summary>
    [CustomEditor(typeof(TrackSceneSetup))]
    public sealed class TrackSceneSetupEditor : UnityEditor.Editor
    {
        private bool _isPlacing;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TrackSceneSetup setup = (TrackSceneSetup)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Waypoints: {setup.WaypointCount}", EditorStyles.boldLabel);

            EditorGUILayout.Space(4);

            // Get or create Path child
            Transform pathParent = GetPathParent(setup);

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

            EditorGUILayout.Space(4);

            if (pathParent != null && pathParent.childCount > 0)
            {
                if (GUILayout.Button("Clear All Waypoints"))
                {
                    if (EditorUtility.DisplayDialog("Clear", "Remove all waypoints?", "Yes", "Cancel"))
                    {
                        Undo.RecordObject(pathParent.gameObject, "Clear Waypoints");
                        while (pathParent.childCount > 0)
                            Undo.DestroyObjectImmediate(pathParent.GetChild(0).gameObject);
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
                    e.Use();
                }
            }

            // Right-click to undo last
            if (e.type == EventType.MouseDown && e.button == 1 && !e.alt && pathParent.childCount > 0)
            {
                Undo.DestroyObjectImmediate(pathParent.GetChild(pathParent.childCount - 1).gameObject);
                e.Use();
            }

            SceneView.currentDrawingSceneView?.Repaint();
        }

        private Transform GetPathParent(TrackSceneSetup setup)
        {
            // Use serialized field via reflection
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
                // Only remove if it was added by us (no saved collider)
                if (!PrefabUtility.IsPartOfPrefabInstance(mc))
                    Undo.DestroyObjectImmediate(mc);
            }
        }
    }
}
