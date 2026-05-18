using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>
    /// ScriptableObject that stores the racing-line waypoints traced over a 3D track model.
    /// Points are in normalised design space (centred at origin) — scaled at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "RacingLine", menuName = "SlotCarRacing/Racing Line Data")]
    public sealed class RacingLineData : ScriptableObject
    {
        [Tooltip("Control points of the racing line in normalised coordinates (model space).")]
        public Vector3[] Waypoints = System.Array.Empty<Vector3>();

        [Tooltip("Bounding size of the model when waypoints were captured (used for normalisation).")]
        public Vector3 OriginalModelSize = Vector3.one;
    }
}
