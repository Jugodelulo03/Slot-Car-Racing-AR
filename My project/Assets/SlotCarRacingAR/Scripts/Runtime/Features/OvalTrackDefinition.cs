using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>Curve difficulty levels based on curvature angle.</summary>
    public enum CurveDifficulty
    {
        Straight = 0,   // No curvature — full speed
        Gentle = 1,     // Curva suave — near-max speed OK
        Medium = 2,     // Curva media — ease off throttle
        Sharp = 3,      // Curva cerrada — must brake before entering
        Hairpin = 4     // Curva muy fuerte — heavy braking required
    }

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

        /// <summary>Angle thresholds for each difficulty tier (smoothed local curvature degrees).</summary>
        private const float GentleThresholdDeg = 8.0f;
        private const float MediumThresholdDeg = 18.0f;
        private const float SharpThresholdDeg = 30.0f;
        private const float HairpinThresholdDeg = 50.0f;
        /// <summary>Smoothing window as fraction of total points for averaging local curvature.</summary>
        private const float SmoothingWindowFraction = 0.03f;
        private const int MinSmoothingWindow = 3;

        private readonly Vector3[] _waypoints;
        private readonly CurveDifficulty[] _curveDifficulty;
        private readonly float[] _curvatureAngles; // raw angles for diagnostics
        private readonly float _totalLength;
        private readonly float[] _cumulativeLengths;
        private readonly float _boundingWidth;
        private readonly float _boundingLength;

        public int WaypointCount => _waypoints.Length;
        public float TotalLength => _totalLength;
        public float BoundingWidth => _boundingWidth;

        /// <summary>Returns all interpolated waypoint positions (read-only reference).</summary>
        public Vector3[] GetAllWaypoints() => _waypoints;
        /// <summary>Returns per-waypoint difficulty classification (read-only reference).</summary>
        public CurveDifficulty[] GetAllDifficulties() => _curveDifficulty;

        /// <summary>Percentage of track points that are NOT Straight (0-100).</summary>
        public float CurvePercentage
        {
            get
            {
                if (_curveDifficulty == null || _curveDifficulty.Length == 0) return 0f;
                int count = 0;
                for (int i = 0; i < _curveDifficulty.Length; i++)
                    if (_curveDifficulty[i] != CurveDifficulty.Straight) count++;
                return 100f * count / _curveDifficulty.Length;
            }
        }

        /// <summary>Returns counts per difficulty level for diagnostics.</summary>
        public void GetDifficultyCounts(out int straight, out int gentle, out int medium, out int sharp, out int hairpin)
        {
            straight = gentle = medium = sharp = hairpin = 0;
            if (_curveDifficulty == null) return;
            for (int i = 0; i < _curveDifficulty.Length; i++)
            {
                switch (_curveDifficulty[i])
                {
                    case CurveDifficulty.Straight: straight++; break;
                    case CurveDifficulty.Gentle: gentle++; break;
                    case CurveDifficulty.Medium: medium++; break;
                    case CurveDifficulty.Sharp: sharp++; break;
                    case CurveDifficulty.Hairpin: hairpin++; break;
                }
            }
        }
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

            // Classify curves by difficulty
            _curvatureAngles = ComputeCurvatureAngles(_waypoints, total);
            _curveDifficulty = ClassifyCurves(_curvatureAngles, total);

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

            // Classify curves by difficulty
            _curvatureAngles = ComputeCurvatureAngles(_waypoints, total);
            _curveDifficulty = ClassifyCurves(_curvatureAngles, total);

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
        /// Creates the track from raw pre-positioned waypoints with manual difficulty data.
        /// Used with TrackSceneSetup + RacingLineData that has WaypointDifficulties.
        /// </summary>
        public OvalTrackDefinition(Vector3[] rawWaypoints, CurveDifficulty[] manualDifficulties, int resolutionPerSegment = 5)
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

            // Use manual difficulties
            if (manualDifficulties != null && manualDifficulties.Length == ctrlCount)
            {
                _curveDifficulty = PropagateManualDifficulties(manualDifficulties, ctrlCount, resolutionPerSegment, total);
                _curvatureAngles = new float[total];
            }
            else
            {
                _curvatureAngles = ComputeCurvatureAngles(_waypoints, total);
                _curveDifficulty = ClassifyCurves(_curvatureAngles, total);
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
        /// If the RacingLineData has manual curve data, it is propagated to interpolated
        /// points instead of computing automatic curvature detection.
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

            // Use manual curve data if available; otherwise fall back to auto-detection
            if (racingLine.HasManualCurveData)
            {
                _curveDifficulty = PropagateManualDifficulties(racingLine.WaypointDifficulties, ctrlCount, resolutionPerSegment, total);
                _curvatureAngles = new float[total]; // zeros — not needed with manual data
                UnityEngine.Debug.Log($"[OvalTrack] Using MANUAL curve data from RacingLineData ({ctrlCount} control points → {total} interpolated).");
            }
            else
            {
                _curvatureAngles = ComputeCurvatureAngles(_waypoints, total);
                _curveDifficulty = ClassifyCurves(_curvatureAngles, total);
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
            int idx = GetWaypointIndexAtProgress(progress);
            return _curveDifficulty[idx] != CurveDifficulty.Straight;
        }

        /// <summary>Returns the curve difficulty at the given progress.</summary>
        public CurveDifficulty GetDifficultyAtProgress(float progress)
        {
            int idx = GetWaypointIndexAtProgress(progress);
            return _curveDifficulty[idx];
        }

        /// <summary>Returns the curvature angle (degrees) at the given progress for diagnostics.</summary>
        public float CurvatureAngleAtProgress(float progress)
        {
            int idx = GetWaypointIndexAtProgress(progress);
            return _curvatureAngles[idx];
        }

        /// <summary>
        /// Resolves progress (0..1) to the waypoint index using arc-length,
        /// matching the same segment the car is physically on.
        /// </summary>
        private int GetWaypointIndexAtProgress(float progress)
        {
            progress = Mathf.Repeat(progress, 1f);
            float dist = progress * _totalLength;
            int count = _waypoints.Length;

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                float segEnd = next == 0 ? _totalLength : _cumulativeLengths[next];

                if (dist < segEnd)
                    return i;
            }

            return 0;
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

        /// <summary>
        /// Computes curvature using LOCAL tangent change, then smooths with a moving average.
        /// Step 1: At each point, compute the tangent (direction to next point).
        /// Step 2: Measure the angle between consecutive tangents (= local curvature per step).
        /// Step 3: Smooth with a moving average window to eliminate noise while preserving real curves.
        /// Result: straight sections → ~0°, gentle curves → small angles, hairpins → large angles.
        /// </summary>
        private static float[] ComputeCurvatureAngles(Vector3[] waypoints, int total)
        {
            // Step 1: Compute tangent at each point
            Vector3[] tangents = new Vector3[total];
            for (int i = 0; i < total; i++)
            {
                int next = (i + 1) % total;
                Vector3 dir = waypoints[next] - waypoints[i];
                tangents[i] = dir.sqrMagnitude > 0.00001f ? dir.normalized : Vector3.forward;
            }

            // Step 2: Local curvature = angle between consecutive tangents
            float[] rawAngles = new float[total];
            for (int i = 0; i < total; i++)
            {
                int prev = (i - 1 + total) % total;
                rawAngles[i] = Vector3.Angle(tangents[prev], tangents[i]);
            }

            // Step 3: Smooth with moving average
            int smoothWindow = Mathf.Max(MinSmoothingWindow, Mathf.RoundToInt(total * SmoothingWindowFraction));
            int halfWindow = smoothWindow / 2;
            float[] smoothed = new float[total];

            // Compute running sum for efficiency
            for (int i = 0; i < total; i++)
            {
                float sum = 0f;
                for (int j = -halfWindow; j <= halfWindow; j++)
                {
                    int idx = (i + j + total) % total;
                    sum += rawAngles[idx];
                }
                smoothed[i] = sum / (halfWindow * 2 + 1);
            }

            return smoothed;
        }

        /// <summary>
        /// Classifies each waypoint into a CurveDifficulty tier based on its curvature angle.
        /// </summary>
        private static CurveDifficulty[] ClassifyCurves(float[] angles, int total)
        {
            CurveDifficulty[] difficulty = new CurveDifficulty[total];
            int[] counts = new int[5];

            for (int i = 0; i < total; i++)
            {
                float a = angles[i];
                if (a >= HairpinThresholdDeg)
                    difficulty[i] = CurveDifficulty.Hairpin;
                else if (a >= SharpThresholdDeg)
                    difficulty[i] = CurveDifficulty.Sharp;
                else if (a >= MediumThresholdDeg)
                    difficulty[i] = CurveDifficulty.Medium;
                else if (a >= GentleThresholdDeg)
                    difficulty[i] = CurveDifficulty.Gentle;
                else
                    difficulty[i] = CurveDifficulty.Straight;

                counts[(int)difficulty[i]]++;
            }

            UnityEngine.Debug.Log($"[OvalTrack] ClassifyCurves: Straight={counts[0]} Gentle={counts[1]} " +
                                  $"Medium={counts[2]} Sharp={counts[3]} Hairpin={counts[4]} (total={total}, smoothWindow={Mathf.Max(MinSmoothingWindow, Mathf.RoundToInt(total * SmoothingWindowFraction))})");
            return difficulty;
        }

        /// <summary>
        /// Propagates per-control-point manual difficulties to all interpolated waypoints.
        /// Each control point's difficulty is CENTERED on its position — spanning half the
        /// resolution backward and half forward. This prevents the difficulty zone from
        /// appearing shifted ahead of the actual curve geometry.
        /// When two zones overlap, the higher difficulty wins (max).
        /// </summary>
        private static CurveDifficulty[] PropagateManualDifficulties(
            CurveDifficulty[] controlDifficulties, int ctrlCount, int resolutionPerSegment, int total)
        {
            CurveDifficulty[] result = new CurveDifficulty[total];
            int[] counts = new int[5];

            int halfSpan = resolutionPerSegment / 2;

            for (int seg = 0; seg < ctrlCount; seg++)
            {
                CurveDifficulty d = seg < controlDifficulties.Length
                    ? controlDifficulties[seg]
                    : CurveDifficulty.Straight;

                // Center point of this control point in interpolated space
                int center = seg * resolutionPerSegment;

                for (int offset = -halfSpan; offset < resolutionPerSegment - halfSpan; offset++)
                {
                    int idx = (center + offset + total) % total;
                    // Higher difficulty wins when zones overlap
                    if (d > result[idx])
                        result[idx] = d;
                }
            }

            for (int i = 0; i < total; i++)
                counts[(int)result[i]]++;

            UnityEngine.Debug.Log($"[OvalTrack] Manual difficulties (centered): Straight={counts[0]} Gentle={counts[1]} " +
                                  $"Medium={counts[2]} Sharp={counts[3]} Hairpin={counts[4]} (total={total})");
            return result;
        }
    }
}
