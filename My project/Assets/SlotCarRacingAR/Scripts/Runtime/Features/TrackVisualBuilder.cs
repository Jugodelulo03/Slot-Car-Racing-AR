using UnityEngine;

namespace SlotCarRacingAR.Runtime.Features
{
    /// <summary>
    /// Renders an OvalTrackDefinition as a styled slot-car track:
    ///  - Wide black surface (road)
    ///  - White border lines on each edge
    ///  - Red/white alternating curb strips on sharp curves
    /// Created as a child of the ARAnchor so it moves with the tracked surface.
    /// </summary>
    public sealed class TrackVisualBuilder : MonoBehaviour
    {
        private LineRenderer _surfaceRenderer;
        private LineRenderer _innerBorderRenderer;
        private LineRenderer _outerBorderRenderer;
        private GameObject _curbContainer;
        private OvalTrackDefinition _trackDefinition;

        private const float LaneHalfWidth = 0.018f;  // 1.8cm each side = 3.6cm road
        private const float BorderWidth = 0.003f;     // 3mm white border
        private const float CurbWidth = 0.006f;       // 6mm curb strip
        private const float CurbSegmentLength = 0.012f; // 1.2cm per curb tile

        public void Build(OvalTrackDefinition trackDefinition)
        {
            _trackDefinition = trackDefinition;
            CreateSurface(trackDefinition);
            CreateBorders(trackDefinition);
            CreateCurbs(trackDefinition);
        }

        public void Rebuild(OvalTrackDefinition trackDefinition)
        {
            _trackDefinition = trackDefinition;

            if (_surfaceRenderer != null)
                UpdateLinePoints(_surfaceRenderer, trackDefinition, 0f);
            if (_innerBorderRenderer != null)
                UpdateLinePoints(_innerBorderRenderer, trackDefinition, -(LaneHalfWidth + BorderWidth * 0.5f));
            if (_outerBorderRenderer != null)
                UpdateLinePoints(_outerBorderRenderer, trackDefinition, LaneHalfWidth + BorderWidth * 0.5f);

            // Rebuild curbs from scratch (geometry changes)
            if (_curbContainer != null) Destroy(_curbContainer);
            CreateCurbs(trackDefinition);
        }

        private void CreateSurface(OvalTrackDefinition trackDef)
        {
            GameObject obj = new GameObject("TrackSurface");
            obj.transform.SetParent(transform, false);

            _surfaceRenderer = obj.AddComponent<LineRenderer>();
            ConfigureLine(_surfaceRenderer, new Color(0.08f, 0.08f, 0.08f, 0.95f), LaneHalfWidth * 2f);
            UpdateLinePoints(_surfaceRenderer, trackDef, 0f);
        }

        private void CreateBorders(OvalTrackDefinition trackDef)
        {
            // Inner white border
            GameObject innerObj = new GameObject("TrackInnerBorder");
            innerObj.transform.SetParent(transform, false);
            _innerBorderRenderer = innerObj.AddComponent<LineRenderer>();
            ConfigureLine(_innerBorderRenderer, new Color(1f, 1f, 1f, 0.9f), BorderWidth);
            UpdateLinePoints(_innerBorderRenderer, trackDef, -(LaneHalfWidth + BorderWidth * 0.5f));

            // Outer white border
            GameObject outerObj = new GameObject("TrackOuterBorder");
            outerObj.transform.SetParent(transform, false);
            _outerBorderRenderer = outerObj.AddComponent<LineRenderer>();
            ConfigureLine(_outerBorderRenderer, new Color(1f, 1f, 1f, 0.9f), BorderWidth);
            UpdateLinePoints(_outerBorderRenderer, trackDef, LaneHalfWidth + BorderWidth * 0.5f);
        }

        private void CreateCurbs(OvalTrackDefinition trackDef)
        {
            _curbContainer = new GameObject("Curbs");
            _curbContainer.transform.SetParent(transform, false);

            // Walk the track and place curb tiles where IsCurveAtProgress is true
            float totalLen = trackDef.TotalLength;
            if (totalLen <= 0f) return;

            float step = CurbSegmentLength;
            int maxSegments = Mathf.CeilToInt(totalLen / step) + 1;
            bool inCurve = false;
            int curbIndex = 0;

            // Collect curve runs — each run gets inner+outer curb strips
            float runStart = -1f;

            for (int i = 0; i <= maxSegments; i++)
            {
                float dist = i * step;
                float progress = dist / totalLen;
                if (progress > 1f) progress = 1f;

                bool curved = trackDef.IsCurveAtProgress(progress);

                if (curved && !inCurve)
                {
                    runStart = progress;
                    inCurve = true;
                }
                else if ((!curved || i == maxSegments) && inCurve)
                {
                    // End of curve run — create curb strip
                    float runEnd = progress;
                    CreateCurbStrip(trackDef, runStart, runEnd, curbIndex);
                    curbIndex++;
                    inCurve = false;
                }
            }
        }

        private void CreateCurbStrip(OvalTrackDefinition trackDef, float startProgress, float endProgress, int index)
        {
            float totalLen = trackDef.TotalLength;
            float startDist = startProgress * totalLen;
            float endDist = endProgress * totalLen;
            if (endDist <= startDist) endDist += totalLen;

            float runLength = endDist - startDist;
            int tileCount = Mathf.Max(1, Mathf.RoundToInt(runLength / CurbSegmentLength));

            // Place outer curb
            float outerOffset = LaneHalfWidth + BorderWidth + CurbWidth * 0.5f;
            PlaceCurbTiles(trackDef, startProgress, endProgress, tileCount, outerOffset, $"OuterCurb_{index}");

            // Place inner curb
            float innerOffset = -(LaneHalfWidth + BorderWidth + CurbWidth * 0.5f);
            PlaceCurbTiles(trackDef, startProgress, endProgress, tileCount, innerOffset, $"InnerCurb_{index}");
        }

        private void PlaceCurbTiles(OvalTrackDefinition trackDef, float startProg, float endProg,
            int tileCount, float lateralOffset, string name)
        {
            // Build a line renderer with alternating red/white via gradient
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(_curbContainer.transform, false);

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = false;
            lr.startWidth = CurbWidth;
            lr.endWidth = CurbWidth;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.allowOcclusionWhenDynamic = false;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 1;
            lr.material = new Material(Shader.Find("Sprites/Default"));

            // Points along the curve
            int pointCount = tileCount + 1;
            Vector3[] points = new Vector3[pointCount];

            float progRange = endProg - startProg;
            if (progRange < 0f) progRange += 1f;

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / tileCount;
                float prog = Mathf.Repeat(startProg + t * progRange, 1f);
                Vector3 center = trackDef.GetPositionAtProgress(prog);
                Vector3 fwd = trackDef.GetForwardAtProgress(prog);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                points[i] = center + right * lateralOffset;
            }

            lr.positionCount = pointCount;
            lr.SetPositions(points);

            // Build alternating red/white gradient
            Gradient grad = new Gradient();
            int colorKeys = Mathf.Min(tileCount * 2, 8); // Gradient max 8 keys
            if (colorKeys < 2) colorKeys = 2;

            GradientColorKey[] keys = new GradientColorKey[colorKeys];
            GradientAlphaKey[] alphaKeys = { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };

            for (int i = 0; i < colorKeys; i++)
            {
                float time = (float)i / (colorKeys - 1);
                keys[i] = new GradientColorKey(i % 2 == 0 ? Color.red : Color.white, time);
            }

            grad.SetKeys(keys, alphaKeys);
            lr.colorGradient = grad;
        }

        private static void ConfigureLine(LineRenderer lr, Color color, float width)
        {
            lr.useWorldSpace = false;
            lr.loop = false;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.allowOcclusionWhenDynamic = false;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 2;

            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
        }

        private static void UpdateLinePoints(LineRenderer lr, OvalTrackDefinition trackDef, float lateralOffset)
        {
            Vector3[] centerPoints = trackDef.GetClosedLoopPoints();

            if (Mathf.Abs(lateralOffset) < 0.001f)
            {
                lr.positionCount = centerPoints.Length;
                lr.SetPositions(centerPoints);
                return;
            }

            Vector3[] offsetPoints = new Vector3[centerPoints.Length];
            for (int i = 0; i < centerPoints.Length; i++)
            {
                int prev = (i - 1 + centerPoints.Length) % centerPoints.Length;
                int next = (i + 1) % centerPoints.Length;
                Vector3 forward = (centerPoints[next] - centerPoints[prev]).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                offsetPoints[i] = centerPoints[i] + right * lateralOffset;
            }

            lr.positionCount = offsetPoints.Length;
            lr.SetPositions(offsetPoints);
        }
    }
}
