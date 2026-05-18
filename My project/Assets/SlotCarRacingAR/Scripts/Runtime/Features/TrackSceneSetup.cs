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

                // Waypoint sphere
                Gizmos.color = i == 0 ? Color.green : Color.yellow;
                Gizmos.DrawSphere(wp.position, 0.3f);

                // Connection line
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                Gizmos.DrawLine(wp.position, wpNext.position);
            }
        }
    }
}
