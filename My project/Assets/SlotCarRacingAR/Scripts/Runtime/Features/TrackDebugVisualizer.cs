using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>
    /// Runtime debug visualizer that shows track waypoints colored by their
    /// CurveDifficulty. Attach to any GameObject — call ShowTrack() after
    /// the OvalTrackDefinition is created.
    /// Toggle on/off with the _enabled field or via SetVisible().
    /// </summary>
    public sealed class TrackDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization")]
        [Tooltip("Show debug points at runtime.")]
        [SerializeField] private bool _showPoints = false;
        [Tooltip("Allow these debug points in device builds. Keep disabled for normal gameplay.")]
        [SerializeField] private bool _showInPlayerBuilds = false;
        [Tooltip("Size of each debug sphere.")]
        [SerializeField] private float _pointSize = 0.002f;
        [Tooltip("Only show every Nth point (1 = all points).")]
        [SerializeField] [Range(1, 10)] private int _skipFactor = 1;

        // Colors per difficulty
        private static readonly Color ColorStraight = new Color(0.2f, 0.9f, 0.2f, 0.8f); // green
        private static readonly Color ColorGentle   = new Color(0.9f, 0.9f, 0.2f, 0.8f); // yellow
        private static readonly Color ColorMedium   = new Color(1.0f, 0.6f, 0.0f, 0.8f); // orange
        private static readonly Color ColorSharp    = new Color(1.0f, 0.2f, 0.2f, 0.8f); // red
        private static readonly Color ColorHairpin  = new Color(0.8f, 0.0f, 0.8f, 0.8f); // magenta

        private GameObject _pointsParent;
        private Transform _trackAnchor;

        /// <summary>
        /// Creates colored debug spheres at each track waypoint position.
        /// Call after OvalTrackDefinition is built and the track transform is positioned.
        /// </summary>
        /// <param name="track">The track definition with waypoints and difficulties.</param>
        /// <param name="anchor">The transform that serves as parent (anchor or track root).</param>
        public void ShowTrack(OvalTrackDefinition track, Transform anchor)
        {
            _trackAnchor = anchor;
            ClearPoints();

            if (!ShouldShowPoints() || track == null) return;

            Vector3[] waypoints = track.GetAllWaypoints();
            CurveDifficulty[] difficulties = track.GetAllDifficulties();

            if (waypoints == null || difficulties == null) return;

            _pointsParent = new GameObject("[DEBUG] TrackPoints");
            _pointsParent.transform.SetParent(anchor, false);

            // Create a shared unlit material base
            Shader unlitShader = Shader.Find("Sprites/Default");

            int shown = 0;
            for (int i = 0; i < waypoints.Length; i += _skipFactor)
            {
                Color color = GetDifficultyColor(difficulties[i]);

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = $"P{i}_{difficulties[i]}";
                sphere.transform.SetParent(_pointsParent.transform, false);
                sphere.transform.localPosition = waypoints[i];
                sphere.transform.localScale = Vector3.one * _pointSize;

                // Remove collider to avoid physics overhead
                var col = sphere.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Unlit colored material
                var renderer = sphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var mat = new Material(unlitShader != null ? unlitShader : Shader.Find("Universal Render Pipeline/Unlit"));
                    mat.color = color;
                    renderer.material = mat;
                }
                shown++;
            }

            UnityEngine.Debug.Log($"[TrackDebugVisualizer] Showing {shown} debug points (skip={_skipFactor}, total={waypoints.Length})");
        }

        /// <summary>Toggle visibility of debug points.</summary>
        public void SetVisible(bool visible)
        {
            _showPoints = visible;
            if (_pointsParent != null)
                _pointsParent.SetActive(ShouldShowPoints());
        }

        /// <summary>Destroy all debug point objects.</summary>
        public void ClearPoints()
        {
            if (_pointsParent != null)
            {
                Destroy(_pointsParent);
                _pointsParent = null;
            }
        }

        private void OnDestroy()
        {
            ClearPoints();
        }

        private bool ShouldShowPoints()
        {
#if !UNITY_EDITOR
            if (!_showInPlayerBuilds)
            {
                return false;
            }
#endif

            return _showPoints;
        }

        private static Color GetDifficultyColor(CurveDifficulty difficulty)
        {
            switch (difficulty)
            {
                case CurveDifficulty.Gentle:  return ColorGentle;
                case CurveDifficulty.Medium:  return ColorMedium;
                case CurveDifficulty.Sharp:   return ColorSharp;
                case CurveDifficulty.Hairpin: return ColorHairpin;
                default:                      return ColorStraight;
            }
        }
    }
}
