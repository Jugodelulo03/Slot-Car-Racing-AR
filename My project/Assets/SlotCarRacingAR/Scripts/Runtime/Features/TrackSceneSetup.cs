using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>
    /// Place this on a root GameObject in the Race scene that contains:
    ///   - The 3D track model (child)
    ///   - A "Path" child with empty GameObjects as waypoints
    ///
    /// At runtime, this whole group is parented under the AR anchor and
    /// scaled uniformly. The car reads waypoint positions from the children,
    /// which scale automatically with the parent transform.
    /// </summary>
    public sealed class TrackSceneSetup : MonoBehaviour
    {
        [Tooltip("Child transform whose children define the racing path waypoints (in order).")]
        [SerializeField] private Transform _pathParent;

        [Tooltip("Manual curve difficulty per waypoint. Matches Path children order.")]
        [SerializeField] private CurveDifficulty[] _waypointDifficulties = System.Array.Empty<CurveDifficulty>();

        /// <summary>Manual difficulties array (matches path child order). Null/empty = use auto-detection.</summary>
        public CurveDifficulty[] WaypointDifficulties => _waypointDifficulties;

        /// <summary>True when manual difficulties are assigned and match waypoint count.</summary>
        public bool HasManualCurveData =>
            _waypointDifficulties != null &&
            _pathParent != null &&
            _waypointDifficulties.Length == _pathParent.childCount &&
            _waypointDifficulties.Length > 0;

        /// <summary>
        /// Returns the waypoint positions in the local space of this transform.
        /// Since this object becomes a child of the anchor, these positions
        /// are already in anchor-local space after parenting.
        /// </summary>
        public Vector3[] GetLocalWaypoints()
        {
            if (_pathParent == null || _pathParent.childCount < 3)
            {
                UnityEngine.Debug.LogWarning("[TrackSceneSetup] Path parent missing or < 3 waypoints.");
                return System.Array.Empty<Vector3>();
            }

            int count = _pathParent.childCount;
            Vector3[] waypoints = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                // Position relative to this root (not pathParent)
                waypoints[i] = transform.InverseTransformPoint(_pathParent.GetChild(i).position);
            }

            return waypoints;
        }

        /// <summary>
        /// Returns waypoint positions converted to the anchor's local space.
        /// Call this AFTER the final localScale/localPosition have been set,
        /// so the world positions of the children reflect the final transform.
        /// </summary>
        public Vector3[] GetAnchorSpaceWaypoints(Transform anchorTransform)
        {
            if (_pathParent == null || _pathParent.childCount < 3)
            {
                UnityEngine.Debug.LogWarning("[TrackSceneSetup] Path parent missing or < 3 waypoints.");
                return System.Array.Empty<Vector3>();
            }

            int count = _pathParent.childCount;
            Vector3[] waypoints = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                waypoints[i] = anchorTransform.InverseTransformPoint(_pathParent.GetChild(i).position);
            }

            return waypoints;
        }

        /// <summary>
        /// Returns waypoint count for editor display.
        /// </summary>
        public int WaypointCount => _pathParent != null ? _pathParent.childCount : 0;

        private void OnDrawGizmos()
        {
            if (_pathParent == null) return;

            int count = _pathParent.childCount;
            if (count < 2) return;

            for (int i = 0; i < count; i++)
            {
                Transform wp = _pathParent.GetChild(i);
                int next = (i + 1) % count;
                Transform wpNext = _pathParent.GetChild(next);

                // Waypoint sphere colored by difficulty
                CurveDifficulty diff = (i < _waypointDifficulties.Length) ? _waypointDifficulties[i] : CurveDifficulty.Straight;
                Gizmos.color = i == 0 ? Color.green : GetDifficultyGizmoColor(diff);
                Gizmos.DrawSphere(wp.position, 0.3f);

                // Connection line colored by difficulty
                Gizmos.color = GetDifficultyGizmoColor(diff);
                Gizmos.DrawLine(wp.position, wpNext.position);
            }
        }

        private static Color GetDifficultyGizmoColor(CurveDifficulty d)
        {
            switch (d)
            {
                case CurveDifficulty.Gentle: return new Color(0.4f, 1f, 0.4f);
                case CurveDifficulty.Medium: return new Color(1f, 1f, 0.2f);
                case CurveDifficulty.Sharp: return new Color(1f, 0.5f, 0f);
                case CurveDifficulty.Hairpin: return new Color(1f, 0.15f, 0.15f);
                default: return Color.yellow;
            }
        }
    }
}
