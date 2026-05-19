using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>
    /// ScriptableObject that stores the racing-line waypoints traced over a 3D track model.
    /// Points are in normalised design space (centred at origin) — scaled at runtime.
    /// Each waypoint has a manually-assigned CurveDifficulty so curve detection is deterministic.
    /// </summary>
    [CreateAssetMenu(fileName = "RacingLine", menuName = "SlotCarRacing/Racing Line Data")]
    public sealed class RacingLineData : ScriptableObject
    {
        [Tooltip("Control points of the racing line in normalised coordinates (model space).")]
        public Vector3[] Waypoints = System.Array.Empty<Vector3>();

        [Tooltip("Manual curve difficulty per waypoint. Length must match Waypoints. " +
                 "Straight=0, Gentle=1, Medium=2, Sharp=3, Hairpin=4.")]
        public CurveDifficulty[] WaypointDifficulties = System.Array.Empty<CurveDifficulty>();

        [Tooltip("Bounding size of the model when waypoints were captured (used for normalisation).")]
        public Vector3 OriginalModelSize = Vector3.one;

        /// <summary>True when manual curve data exists and matches waypoint count.</summary>
        public bool HasManualCurveData =>
            WaypointDifficulties != null &&
            WaypointDifficulties.Length == Waypoints.Length &&
            WaypointDifficulties.Length > 0;
    }
}
