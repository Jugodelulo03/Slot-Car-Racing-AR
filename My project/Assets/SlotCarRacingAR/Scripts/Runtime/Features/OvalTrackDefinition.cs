using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>
    /// Defines a slot-car racing circuit as a closed Catmull-Rom spline.
    /// The layout is modeled after a classic Scalextric track: long straight
    /// on the right, sweeping corner curves, and a tight S-curve chicane
    /// through the centre.  A single <c>scale</c> parameter controls the
    /// physical size while preserving the ~1:2 aspect ratio.
    /// Curve sections are detected automatically from spline curvature.
    /// </summary>
    public sealed class OvalTrackDefinition
    {
        // ── Design-space control points ──────────────────────────────
        // Normalised rectangle ~0.86 wide × 1.84 tall, centred at origin.
        private static readonly Vector2[] DesignPoints =
        {
            // Bottom straight (→)
            new(-0.30f, -0.92f),
            new(-0.05f, -0.92f),
            new( 0.20f, -0.92f),

            // Bottom-right sweeper (↗ ↑)
            new( 0.35f, -0.90f),
            new( 0.43f, -0.82f),
            new( 0.43f, -0.70f),

            // Right long straight (↑)
            new( 0.43f, -0.50f),
            new( 0.43f, -0.25f),
            new( 0.43f,  0.00f),
            new( 0.43f,  0.25f),
            new( 0.43f,  0.50f),
            new( 0.43f,  0.70f),

            // Top-right sweeper (↑ ←)
            new( 0.43f,  0.82f),
            new( 0.35f,  0.90f),
            new( 0.20f,  0.92f),

            // Top straight (←)
            new( 0.00f,  0.92f),
            new(-0.15f,  0.92f),

            // Top-left hairpin (← ↓)
            new(-0.30f,  0.88f),
            new(-0.38f,  0.78f),
            new(-0.38f,  0.62f),

            // Short descent
            new(-0.35f,  0.50f),

            // S-curve 1 (→)
            new(-0.22f,  0.42f),
            new(-0.05f,  0.36f),
            new( 0.12f,  0.28f),

            // S-curve 2 (←)
            new( 0.08f,  0.18f),
            new(-0.05f,  0.10f),
            new(-0.22f,  0.02f),

            // S-curve 3 (→)
            new(-0.18f, -0.08f),
            new(-0.02f, -0.16f),
            new( 0.12f, -0.24f),

            // Exit curve (↙)
            new( 0.05f, -0.35f),
            new(-0.10f, -0.48f),

            // Bottom-left sweeper (↓ ← ↓ →)
            new(-0.25f, -0.58f),
            new(-0.38f, -0.68f),
            new(-0.42f, -0.80f),
            new(-0.38f, -0.88f),
        };

        /// <summary>Approximate width of the design in normalised units.</summary>
        public const float DesignBoundingWidth = 0.86f;
        /// <summary>Approximate height of the design in normalised units.</summary>
        public const float DesignBoundingHeight = 1.84f;

        /// <summary>Angle change per waypoint above which it is flagged as a curve.</summary>
        private const float CurvatureThresholdDeg = 2.5f;

        private readonly Vector3[] _waypoints;
        private readonly bool[] _isCurve;
        private readonly float _totalLength;
        private readonly float[] _cumulativeLengths;
        private readonly float _boundingWidth;
        private readonly float _boundingLength;

        public int WaypointCount => _waypoints.Length;
        public float TotalLength => _totalLength;
        public float BoundingWidth => _boundingWidth;
        public float BoundingLength => _boundingLength;

        /// <summary>
        /// Creates the track at the given uniform scale.
        /// <c>scale = 0.25</c> → ≈ 22 cm × 46 cm on the table.
        /// </summary>
        public OvalTrackDefinition(float scale, float heightOffset, int resolutionPerSegment = 5)
            : this(scale, scale, heightOffset, resolutionPerSegment) { }

        /// <summary>
        /// Creates the track with independent X and Z scales.
        /// Used when the 3D model's aspect ratio differs from the design points.
        /// </summary>
        public OvalTrackDefinition(float scaleX, float scaleZ, float heightOffset, int resolutionPerSegment = 5)
        {
            int ctrlCount = DesignPoints.Length;
            int total = ctrlCount * resolutionPerSegment;
            float y = heightOffset;

            _waypoints = new Vector3[total];

            // Catmull-Rom interpolation over design points
            for (int seg = 0; seg < ctrlCount; seg++)
            {
                Vector2 a = DesignPoints[(seg - 1 + ctrlCount) % ctrlCount];
                Vector2 b = DesignPoints[seg];
                Vector2 c = DesignPoints[(seg + 1) % ctrlCount];
                Vector2 d = DesignPoints[(seg + 2) % ctrlCount];

                for (int s = 0; s < resolutionPerSegment; s++)
                {
                    float t = (float)s / resolutionPerSegment;
                    Vector2 pt = CatmullRom2D(a, b, c, d, t);
                    _waypoints[seg * resolutionPerSegment + s] =
                        new Vector3(pt.x * scaleX, y, pt.y * scaleZ);
                }
            }

            // Cumulative arc lengths
            _cumulativeLengths = new float[total];
            _cumulativeLengths[0] = 0f;
            float accum = 0f;
            for (int i = 1; i < total; i++)
            {
                accum += Vector3.Distance(_waypoints[i - 1], _waypoints[i]);
                _cumulativeLengths[i] = accum;
            }
            _totalLength = accum + Vector3.Distance(_waypoints[total - 1], _waypoints[0]);

            // Auto-detect curves from turning angle
            _isCurve = new bool[total];
            for (int i = 0; i < total; i++)
            {
                int prev = (i - 1 + total) % total;
                int next = (i + 1) % total;
                Vector3 v1 = (_waypoints[i] - _waypoints[prev]).normalized;
                Vector3 v2 = (_waypoints[next] - _waypoints[i]).normalized;
                if (v1.sqrMagnitude > 0.0001f && v2.sqrMagnitude > 0.0001f)
                    _isCurve[i] = Vector3.Angle(v1, v2) > CurvatureThresholdDeg;
            }

            // Bounding box
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < total; i++)
            {
                if (_waypoints[i].x < minX) minX = _waypoints[i].x;
                if (_waypoints[i].x > maxX) maxX = _waypoints[i].x;
                if (_waypoints[i].z < minZ) minZ = _waypoints[i].z;
                if (_waypoints[i].z > maxZ) maxZ = _waypoints[i].z;
            }
            _boundingWidth = maxX - minX;
            _boundingLength = maxZ - minZ;
        }

        /// <summary>
        /// Creates the track from raw pre-positioned waypoints (already in final local space).
        /// Used with TrackSceneSetup where waypoints are placed directly on the 3D model.
        /// No scaling is applied — the transform hierarchy handles it.
        /// </summary>
        public OvalTrackDefinition(Vector3[] rawWaypoints, int resolutionPerSegment = 5)
        {
            int ctrlCount = rawWaypoints.Length;
            int total = ctrlCount * resolutionPerSegment;

            _waypoints = new Vector3[total];

            for (int seg = 0; seg < ctrlCount; seg++)
            {
                Vector3 a = rawWaypoints[(seg - 1 + ctrlCount) % ctrlCount];
                Vector3 b = rawWaypoints[seg];
                Vector3 c = rawWaypoints[(seg + 1) % ctrlCount];
                Vector3 d = rawWaypoints[(seg + 2) % ctrlCount];

                for (int s = 0; s < resolutionPerSegment; s++)
                {
                    float t = (float)s / resolutionPerSegment;
                    _waypoints[seg * resolutionPerSegment + s] = CatmullRom3D(a, b, c, d, t);
                }
            }

            _cumulativeLengths = new float[total];
            _cumulativeLengths[0] = 0f;
            float accum = 0f;
            for (int i = 1; i < total; i++)
            {
                accum += Vector3.Distance(_waypoints[i - 1], _waypoints[i]);
                _cumulativeLengths[i] = accum;
            }
            _totalLength = accum + Vector3.Distance(_waypoints[total - 1], _waypoints[0]);

            _isCurve = new bool[total];
            for (int i = 0; i < total; i++)
            {
                int prev = (i - 1 + total) % total;
                int next = (i + 1) % total;
                Vector3 v1 = (_waypoints[i] - _waypoints[prev]).normalized;
                Vector3 v2 = (_waypoints[next] - _waypoints[i]).normalized;
                if (v1.sqrMagnitude > 0.0001f && v2.sqrMagnitude > 0.0001f)
                    _isCurve[i] = Vector3.Angle(v1, v2) > CurvatureThresholdDeg;
            }

            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < total; i++)
            {
                if (_waypoints[i].x < minX) minX = _waypoints[i].x;
                if (_waypoints[i].x > maxX) maxX = _waypoints[i].x;
                if (_waypoints[i].z < minZ) minZ = _waypoints[i].z;
                if (_waypoints[i].z > maxZ) maxZ = _waypoints[i].z;
            }
            _boundingWidth = maxX - minX;
            _boundingLength = maxZ - minZ;
        }

        /// <summary>
        /// Creates the track from a <see cref="RacingLineData"/> asset.
        /// The normalised waypoints are scaled by <paramref name="scale"/> and
        /// interpolated with Catmull-Rom for smooth car movement.
        /// </summary>
        public OvalTrackDefinition(RacingLineData racingLine, float scale, float heightOffset, int resolutionPerSegment = 5)
        {
            Vector3[] ctrl = racingLine.Waypoints;
            int ctrlCount = ctrl.Length;
            int total = ctrlCount * resolutionPerSegment;
            float y = heightOffset;

            _waypoints = new Vector3[total];

            for (int seg = 0; seg < ctrlCount; seg++)
            {
                Vector3 a = ctrl[(seg - 1 + ctrlCount) % ctrlCount];
                Vector3 b = ctrl[seg];
                Vector3 c = ctrl[(seg + 1) % ctrlCount];
                Vector3 d = ctrl[(seg + 2) % ctrlCount];

                for (int s = 0; s < resolutionPerSegment; s++)
                {
                    float t = (float)s / resolutionPerSegment;
                    Vector3 pt = CatmullRom3D(a, b, c, d, t);
                    _waypoints[seg * resolutionPerSegment + s] =
                        new Vector3(pt.x * scale, y, pt.z * scale);
                }
            }

            // Cumulative arc lengths
            _cumulativeLengths = new float[total];
            _cumulativeLengths[0] = 0f;
            float accum = 0f;
            for (int i = 1; i < total; i++)
            {
                accum += Vector3.Distance(_waypoints[i - 1], _waypoints[i]);
                _cumulativeLengths[i] = accum;
            }
            _totalLength = accum + Vector3.Distance(_waypoints[total - 1], _waypoints[0]);

            // Auto-detect curves from turning angle
            _isCurve = new bool[total];
            for (int i = 0; i < total; i++)
            {
                int prev = (i - 1 + total) % total;
                int next = (i + 1) % total;
                Vector3 v1 = (_waypoints[i] - _waypoints[prev]).normalized;
                Vector3 v2 = (_waypoints[next] - _waypoints[i]).normalized;
                if (v1.sqrMagnitude > 0.0001f && v2.sqrMagnitude > 0.0001f)
                    _isCurve[i] = Vector3.Angle(v1, v2) > CurvatureThresholdDeg;
            }

            // Bounding box
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < total; i++)
            {
                if (_waypoints[i].x < minX) minX = _waypoints[i].x;
                if (_waypoints[i].x > maxX) maxX = _waypoints[i].x;
                if (_waypoints[i].z < minZ) minZ = _waypoints[i].z;
                if (_waypoints[i].z > maxZ) maxZ = _waypoints[i].z;
            }
            _boundingWidth = maxX - minX;
            _boundingLength = maxZ - minZ;
        }

        public Vector3 GetPositionAtProgress(float progress)
        {
            progress = Mathf.Repeat(progress, 1f);
            return GetPositionAtDistance(progress * _totalLength);
        }

        public Vector3 GetForwardAtProgress(float progress)
        {
            const float delta = 0.001f;
            Vector3 a = GetPositionAtProgress(progress - delta);
            Vector3 b = GetPositionAtProgress(progress + delta);
            Vector3 fwd = (b - a).normalized;
            return fwd.sqrMagnitude > 0.001f ? fwd : Vector3.forward;
        }

        public bool IsCurveAtProgress(float progress)
        {
            progress = Mathf.Repeat(progress, 1f);
            int idx = Mathf.FloorToInt(progress * _waypoints.Length) % _waypoints.Length;
            return _isCurve[idx];
        }

        public Vector3[] GetClosedLoopPoints()
        {
            var closed = new Vector3[_waypoints.Length + 1];
            System.Array.Copy(_waypoints, closed, _waypoints.Length);
            closed[_waypoints.Length] = _waypoints[0];
            return closed;
        }

        private Vector3 GetPositionAtDistance(float dist)
        {
            dist = Mathf.Repeat(dist, _totalLength);
            int count = _waypoints.Length;

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                float segStart = _cumulativeLengths[i];
                float segEnd = next == 0 ? _totalLength : _cumulativeLengths[next];

                if (dist >= segStart && dist < segEnd)
                {
                    float segLen = segEnd - segStart;
                    float localT = segLen > 0.0001f ? (dist - segStart) / segLen : 0f;

                    int p0 = (i - 1 + count) % count;
                    int p3 = (next + 1) % count;
                    return CatmullRom3D(_waypoints[p0], _waypoints[i], _waypoints[next], _waypoints[p3], localT);
                }
            }

            return _waypoints[0];
        }

        private static Vector2 CatmullRom2D(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static Vector3 CatmullRom3D(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
