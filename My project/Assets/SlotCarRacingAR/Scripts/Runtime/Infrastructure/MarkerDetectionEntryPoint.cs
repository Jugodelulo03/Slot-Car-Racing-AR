using System;
using SlotCarRacingAR.Runtime.Debug;
using SlotCarRacingAR.Runtime.Features;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SlotCarRacingAR.Runtime.Infrastructure
{
    /// <summary>
    /// Detects a single AR marker, accumulates pose samples for accuracy,
    /// then creates an ARAnchor and parents the track placeholder to it.
    /// Unity's transform hierarchy propagates all anchor pose updates automatically.
    ///
    /// Multiplayer-ready: the anchor orientation is derived from the physical marker,
    /// so all devices detecting the same marker get the same canonical orientation.
    /// A future Cloud Anchor save/resolve step shares this pose across devices.
    /// </summary>
    public sealed class MarkerDetectionEntryPoint : MonoBehaviour
    {
        private const string AnchorMarkerName = "track-anchor";
        private const int RequiredSamples = 8;

        [SerializeField] private ARTrackedImageManager _trackedImageManager;
        [SerializeField] private bool _enableEditorFallback = true;

        [Header("Track Scale")]
        [SerializeField] [Range(0.10f, 1.0f)] private float _trackScale = 0.25f;

        [Header("3D Track Model")]
        [Tooltip("Optional: racing line data exported from the Waypoint Placer editor tool.")]
        [SerializeField] private RacingLineData _racingLineData;
        [Tooltip("Optional: 3D track model prefab (GLB/FBX). Loaded under the anchor.")]
        [SerializeField] private GameObject _trackModelPrefab;

        [Header("Scene Track (Recommended)")]
        [Tooltip("Drag the TrackGroup from the scene. Contains the 3D model + path waypoints. Overrides the fields above.")]
        [SerializeField] private TrackSceneSetup _trackSceneSetup;

        [Header("Debug Visualization")]
        [Tooltip("Optional: visualizes track points colored by difficulty at runtime.")]
        [SerializeField] private TrackDebugVisualizer _trackDebugVisualizer;

        [Header("Height Correction")]
        [Tooltip("Vertical offset in meters above the anchor plane.")]
        [SerializeField] [Range(-0.05f, 0.10f)] private float _heightOffsetMeters = 0.015f;

        private ARAnchorManager _anchorManager;
        private TrackPlaceholder _trackPlaceholder;
        private CarPlaceholder _carPlaceholder;
        private TelemetryHooks _telemetryHooks;

        // Original parents so we can un-parent on reset
        private Transform _originalTrackParent;
        private Transform _originalCarParent;

        // Oval track
        private OvalTrackDefinition _ovalTrack;
        private TrackVisualBuilder _trackVisualBuilder;
        private TrackModelLoader _trackModelLoader;

        // Scene-setup cached measurements (at native scale)
        private float _sceneSetupNativeMaxExtent;
        private Vector3 _sceneSetupNativeCenter;

        // Anchor state
        private ARAnchor _worldAnchor;
        private bool _isAnchored;
        private bool _anchorCreationInProgress;

        // Sampling state (accumulate before creating anchor)
        private int _sampleCount;
        private Vector3 _positionAccum;
        private Vector3 _forwardAccum;

        // Stability cubes (children of anchor)
        private GameObject _anchorVisual;

        public bool IsTracking { get; private set; }
        public bool HasAnchor => _isAnchored;
        public float TrackScale => _trackScale;
        public float TrackWidthMeters => _trackScale * OvalTrackDefinition.DesignBoundingWidth;
        public float TrackLengthMeters => _trackScale * OvalTrackDefinition.DesignBoundingHeight;
        public float HeightOffsetMeters => _heightOffsetMeters;
        public string AnchorStatus { get; private set; } = "waiting";
        public int StableFrames => _sampleCount;

        // Multiplayer: expose the canonical pose derived from the marker
        // All devices seeing the same physical marker will compute the same orientation
        public Quaternion AnchorRotation => _worldAnchor != null
            ? _worldAnchor.transform.rotation
            : Quaternion.identity;
        public Vector3 AnchorPosition => _worldAnchor != null
            ? _worldAnchor.transform.position
            : Vector3.zero;

        // Backward-compatible properties
        public int TrackedMarkerCount => _isAnchored ? 1 : 0;
        public bool HasMarkerRectangle => _isAnchored;
        public float RectangleWidthMeters => TrackWidthMeters;
        public float RectangleLengthMeters => TrackLengthMeters;
        public CarPlaceholder Car => _carPlaceholder;

        /// <summary>Describes which curve detection path was used (shown in debug UI).</summary>
        public string CurveDetectionMode { get; private set; } = "not built";

        public void Bind(
            TrackPlaceholder trackPlaceholder,
            CarPlaceholder carPlaceholder,
            TelemetryHooks telemetryHooks,
            ARTrackedImageManager trackedImageManager)
        {
            _trackPlaceholder = trackPlaceholder;
            _carPlaceholder = carPlaceholder;
            _telemetryHooks = telemetryHooks;
            _trackedImageManager = trackedImageManager != null
                ? trackedImageManager
                : GetComponent<ARTrackedImageManager>();

            if (_trackPlaceholder != null)
            {
                _originalTrackParent = _trackPlaceholder.transform.parent;
            }

            if (_carPlaceholder != null)
            {
                _originalCarParent = _carPlaceholder.transform.parent;
            }

            ApplyTrackingState(IsTracking);
        }

        public void BindAnchorManager(ARAnchorManager anchorManager)
        {
            _anchorManager = anchorManager;
        }

        public void SetTrackScale(float scale)
        {
            _trackScale = Mathf.Clamp(scale, 0.10f, 1.0f);

            if (_isAnchored)
            {
                ApplyLocalLayout();
            }
        }

        public void SetHeightOffset(float offsetMeters)
        {
            _heightOffsetMeters = Mathf.Clamp(offsetMeters, -0.05f, 0.10f);

            if (_isAnchored)
            {
                // ═══ PATH A: TrackSceneSetup ═══
                if (_trackSceneSetup != null && _sceneSetupNativeMaxExtent > 0.001f)
                {
                    ApplySceneSetupTransform();
                    RebuildTrackFromSceneWaypoints();
                    if (_carPlaceholder != null)
                    {
                        float scaleFactor = _trackScale / _sceneSetupNativeMaxExtent;
                        _carPlaceholder.ApplyTrackScaleFactor(scaleFactor);
                        if (_ovalTrack != null)
                            _carPlaceholder.BindTrack(_ovalTrack);
                    }
                    return;
                }

                // ═══ PATH B: Legacy ═══
                if (_trackPlaceholder != null)
                {
                    Vector3 localPos = _trackPlaceholder.transform.localPosition;
                    localPos.y = _heightOffsetMeters;
                    _trackPlaceholder.transform.localPosition = localPos;
                }

                // Rebuild oval track at new height
                if (_ovalTrack != null)
                {
                    // Rescale model first, then derive racing line scale
                    if (_trackModelLoader != null)
                        _trackModelLoader.Rescale(_trackScale, _heightOffsetMeters);

                    var (scaleX, scaleZ) = DeriveRacingLineScales();
                    float height = GetRacingLineHeight();

                    if (_racingLineData != null && _racingLineData.Waypoints.Length >= 3)
                        _ovalTrack = new OvalTrackDefinition(_racingLineData, scaleX, height);
                    else
                        _ovalTrack = new OvalTrackDefinition(scaleX, scaleZ, height);
                    if (_trackVisualBuilder != null) _trackVisualBuilder.Rebuild(_ovalTrack);
                    if (_carPlaceholder != null) _carPlaceholder.BindTrack(_ovalTrack);
                }
            }
        }

        /// <summary>
        /// Destroys the current anchor, un-parents the track, and restarts scanning.
        /// </summary>
        public void ResetAnchor()
        {
            // Un-parent track placeholder back to its original parent
            if (_trackPlaceholder != null)
            {
                _trackPlaceholder.transform.SetParent(_originalTrackParent, false);
                _trackPlaceholder.transform.localPosition = Vector3.zero;
                _trackPlaceholder.transform.localRotation = Quaternion.identity;
                _trackPlaceholder.transform.localScale = Vector3.one;
            }

            // Un-parent car back to its original parent
            if (_carPlaceholder != null)
            {
                _carPlaceholder.transform.SetParent(_originalCarParent, false);
                _carPlaceholder.transform.localPosition = Vector3.zero;
                _carPlaceholder.transform.localRotation = Quaternion.identity;
                _carPlaceholder.gameObject.SetActive(false);
            }

            // Clean up track visual and model (will be destroyed with anchor, but clear references)
            _trackVisualBuilder = null;
            _ovalTrack = null;
            if (_trackModelLoader != null)
            {
                _trackModelLoader.Unload();
                _trackModelLoader = null;
            }

            // Destroy anchor GameObject (cubes + visual + track line are children, destroyed with it)
            if (_worldAnchor != null)
            {
                if (_anchorManager != null)
                {
                    _anchorManager.TryRemoveAnchor(_worldAnchor);
                }
                Destroy(_worldAnchor.gameObject);
                _worldAnchor = null;
            }

            _anchorVisual = null;
            _isAnchored = false;
            _anchorCreationInProgress = false;
            _sampleCount = 0;
            _positionAccum = Vector3.zero;
            _forwardAccum = Vector3.zero;
            AnchorStatus = "waiting";
            OnTrackingLost();

            if (_trackedImageManager != null)
            {
                _trackedImageManager.enabled = true;
            }

            UnityEngine.Debug.Log("[MarkerDetection] Reset — scanning for marker again.");
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!_enableEditorFallback) return;

            if (_trackedImageManager == null || _trackedImageManager.referenceLibrary == null
                || _trackedImageManager.subsystem == null)
            {
                UnityEngine.Debug.Log("[MarkerDetection] Editor fallback.");
                _isAnchored = true;
                AnchorStatus = "editor-fallback";
                if (_trackPlaceholder != null)
                {
                    _trackPlaceholder.ApplyLocalLayout(TrackWidthMeters, TrackLengthMeters);
                }
                OnMarkerDetected();
            }
#endif
        }

        private void OnEnable()
        {
            if (_trackedImageManager != null)
            {
                _trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
            }

            UnityEngine.Debug.Log("[MarkerDetection] Enabled — looking for track-anchor marker.");
        }

        private void OnDisable()
        {
            if (_trackedImageManager != null)
            {
                _trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (_trackedImageManager == null || _trackedImageManager.subsystem == null) return;
#endif

            // Once anchored, Unity's transform hierarchy handles everything
            if (_isAnchored || _anchorCreationInProgress) return;

            ScanForMarkerImage();
        }

        private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
        {
            if (_isAnchored || _anchorCreationInProgress) return;
            ScanForMarkerImage();
        }

        private void ScanForMarkerImage()
        {
            if (_trackedImageManager == null) return;

            foreach (ARTrackedImage trackedImage in _trackedImageManager.trackables)
            {
                if (trackedImage == null) continue;
                if (trackedImage.trackingState != TrackingState.Tracking) continue;

                string imageName = trackedImage.referenceImage.name;
                if (!string.Equals(imageName, AnchorMarkerName, StringComparison.Ordinal)) continue;

                AccumulateSample(trackedImage.transform.position, trackedImage.transform.rotation);
                return;
            }
        }

        private void AccumulateSample(Vector3 position, Quaternion rotation)
        {
            // Flatten rotation to horizontal
            Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

            _positionAccum += position;
            _forwardAccum += forward.normalized;
            _sampleCount++;

            AnchorStatus = $"sampling {_sampleCount}/{RequiredSamples}";
            UnityEngine.Debug.Log($"[MarkerDetection] Sample {_sampleCount}/{RequiredSamples} at {position}");

            if (_sampleCount >= RequiredSamples)
            {
                Vector3 avgPos = _positionAccum / _sampleCount;
                Vector3 avgFwd = _forwardAccum.normalized;
                Quaternion avgRot = avgFwd.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(avgFwd, Vector3.up)
                    : Quaternion.identity;

                Pose anchorPose = new Pose(avgPos, avgRot);
                CreateAnchorAndParent(anchorPose);
            }
        }

        private async void CreateAnchorAndParent(Pose anchorPose)
        {
            if (_anchorCreationInProgress || _isAnchored) return;
            _anchorCreationInProgress = true;
            AnchorStatus = "creating-anchor";

            UnityEngine.Debug.Log($"[MarkerDetection] Creating ARAnchor at {anchorPose.position} " +
                                  $"rotation {anchorPose.rotation.eulerAngles}...");

            // Try ARAnchorManager first (proper SLAM-locked anchor)
            if (_anchorManager != null && _anchorManager.subsystem != null)
            {
                try
                {
                    var result = await _anchorManager.TryAddAnchorAsync(anchorPose);
                    if (result.status.IsSuccess())
                    {
                        _worldAnchor = result.value;
                        AnchorStatus = "ANCHORED";
                        UnityEngine.Debug.Log("[MarkerDetection] ARAnchor created via ARAnchorManager.");
                        ParentTrackToAnchor();
                        _anchorCreationInProgress = false;
                        return;
                    }

                    UnityEngine.Debug.LogWarning($"[MarkerDetection] TryAddAnchorAsync failed: {result.status}. Using fallback.");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[MarkerDetection] Anchor exception: {ex.Message}. Using fallback.");
                }
            }

            // Fallback: create GameObject with ARAnchor component
            GameObject anchorObj = new GameObject("TrackWorldAnchor");
            anchorObj.transform.SetPositionAndRotation(anchorPose.position, anchorPose.rotation);
            _worldAnchor = anchorObj.AddComponent<ARAnchor>();
            AnchorStatus = "ANCHORED-fallback";
            UnityEngine.Debug.Log("[MarkerDetection] Fallback ARAnchor created.");

            ParentTrackToAnchor();
            _anchorCreationInProgress = false;
        }

        /// <summary>
        /// Parents the track placeholder (and all its children) under the ARAnchor.
        /// Sets local position/rotation/scale so the track sits at the anchor origin.
        /// Unity's transform hierarchy will then propagate all anchor pose corrections
        /// automatically — no per-frame position copying needed.
        ///
        /// For multiplayer: all devices that resolve the same Cloud Anchor will get
        /// the same world-space pose, so the parented track will have identical
        /// orientation on every device.
        /// </summary>
        private void ParentTrackToAnchor()
        {
            if (_worldAnchor == null) return;

            // Parent the track placeholder under the anchor (hide if 3D model or scene setup is used)
            if (_trackPlaceholder != null)
            {
                _trackPlaceholder.transform.SetParent(_worldAnchor.transform, false);
                _trackPlaceholder.transform.localPosition = new Vector3(0f, _heightOffsetMeters, 0f);
                _trackPlaceholder.transform.localRotation = Quaternion.identity;

                if (_trackModelPrefab != null || _trackSceneSetup != null)
                {
                    _trackPlaceholder.gameObject.SetActive(false);
                }
                else
                {
                    _trackPlaceholder.ApplyLocalLayout(TrackWidthMeters, TrackLengthMeters);
                }
            }

            // Build the oval track and visual
            BuildOvalTrack();

            // Parent the car under the anchor and bind to track
            ParentCarToAnchor();

            // Anchor visual disabled — track scene setup provides its own visuals

            _isAnchored = true;
            OnMarkerDetected();

            // Disable image tracking — no longer needed
            if (_trackedImageManager != null)
            {
                _trackedImageManager.enabled = false;
                UnityEngine.Debug.Log("[MarkerDetection] Image tracking disabled — track parented to anchor.");
            }

            UnityEngine.Debug.Log($"[MarkerDetection] Track parented to ARAnchor. " +
                                  $"Orientation: {_worldAnchor.transform.rotation.eulerAngles} " +
                                  $"(canonical for all devices detecting this marker).");
        }

        private void BuildOvalTrack()
        {
            if (_worldAnchor == null) return;

            // ═══ PATH A: TrackSceneSetup (model + waypoints in scene) ═══
            if (_trackSceneSetup != null)
            {
                BuildFromSceneSetup();
                return;
            }

            // ═══ PATH B: Legacy (separate model prefab + racing line data) ═══

            // ── 1. Load 3D model FIRST (if assigned) so we know its rendered size ──
            if (_trackModelPrefab != null)
            {
                GameObject loaderObj = new GameObject("TrackModelLoader");
                loaderObj.transform.SetParent(_worldAnchor.transform, false);
                _trackModelLoader = loaderObj.AddComponent<TrackModelLoader>();

                float modelTargetSize = _trackScale;
                _trackModelLoader.Load(_trackModelPrefab, _worldAnchor.transform, modelTargetSize, _heightOffsetMeters);

                UnityEngine.Debug.Log($"[MarkerDetection] 3D model loaded: '{_trackModelPrefab.name}', " +
                                      $"renderedSize={_trackModelLoader.RenderedBoundsSize:F4}");
            }

            // ── 2. Build racing line, scaled to match the model ──
            var (scaleX, scaleZ) = DeriveRacingLineScales();
            float height = GetRacingLineHeight();

            if (_racingLineData != null && _racingLineData.Waypoints.Length >= 3)
            {
                _ovalTrack = new OvalTrackDefinition(_racingLineData, scaleX, height);
                UnityEngine.Debug.Log($"[MarkerDetection] Track from RacingLineData: {_ovalTrack.TotalLength:F2}m, " +
                                      $"{_ovalTrack.WaypointCount} waypoints, scale={scaleX:F4}.");
            }
            else
            {
                _ovalTrack = new OvalTrackDefinition(scaleX, scaleZ, height);
                UnityEngine.Debug.Log($"[MarkerDetection] Track from built-in design: {_ovalTrack.TotalLength:F2}m, " +
                                      $"{_ovalTrack.WaypointCount} waypoints, scaleX={scaleX:F4} scaleZ={scaleZ:F4}.");
            }

            // ── 3. Create visual builder (only if no 3D model) ──
            if (_trackModelPrefab == null)
            {
                GameObject visualObj = new GameObject("TrackVisual");
                visualObj.transform.SetParent(_worldAnchor.transform, false);
                _trackVisualBuilder = visualObj.AddComponent<TrackVisualBuilder>();
                _trackVisualBuilder.Build(_ovalTrack);
            }
        }

        /// <summary>
        /// Simple path: parent the scene track group under anchor, measure its native
        /// bounds, then scale so that _trackScale = desired real-world size in meters.
        /// Centers the model on the anchor and reads waypoints in anchor-local space.
        /// </summary>
        private void BuildFromSceneSetup()
        {
            Transform trackRoot = _trackSceneSetup.transform;

            // Parent under anchor at native scale to measure bounds
            trackRoot.SetParent(_worldAnchor.transform, false);
            trackRoot.localPosition = Vector3.zero;
            trackRoot.localRotation = Quaternion.identity;
            trackRoot.localScale = Vector3.one;

            // Measure native bounds
            Bounds nativeBounds = ComputeRendererBounds(trackRoot.gameObject);
            _sceneSetupNativeMaxExtent = Mathf.Max(nativeBounds.size.x, nativeBounds.size.y, nativeBounds.size.z);
            _sceneSetupNativeCenter = _worldAnchor.transform.InverseTransformPoint(nativeBounds.center);

            if (_sceneSetupNativeMaxExtent < 0.001f)
            {
                UnityEngine.Debug.LogError("[MarkerDetection] TrackSceneSetup has zero bounds!");
                _sceneSetupNativeMaxExtent = 1f;
            }

            UnityEngine.Debug.Log($"[MarkerDetection] SceneSetup native bounds: size={nativeBounds.size:F2}, " +
                                  $"center={_sceneSetupNativeCenter:F2}, maxExtent={_sceneSetupNativeMaxExtent:F2}");

            // Apply scale + centering
            ApplySceneSetupTransform();

            // Read waypoints in anchor-local space (after final transform is applied)
            RebuildTrackFromSceneWaypoints();
        }

        /// <summary>
        /// Applies scale and centering to the scene setup transform.
        /// scaleFactor = _trackScale / nativeMaxExtent, centered on anchor.
        /// </summary>
        private void ApplySceneSetupTransform()
        {
            float scaleFactor = _trackScale / _sceneSetupNativeMaxExtent;
            Transform trackRoot = _trackSceneSetup.transform;

            trackRoot.localScale = Vector3.one * scaleFactor;
            // Center: shift so the native bounds center lands on the anchor origin
            trackRoot.localPosition = new Vector3(
                -_sceneSetupNativeCenter.x * scaleFactor,
                _heightOffsetMeters,
                -_sceneSetupNativeCenter.z * scaleFactor);

            UnityEngine.Debug.Log($"[MarkerDetection] SceneSetup applied: scaleFactor={scaleFactor:F6}, " +
                                  $"localScale={trackRoot.localScale:F6}, localPos={trackRoot.localPosition:F4}");
        }

        /// <summary>
        /// Reads waypoint world positions (after transform is set), converts to anchor-local space,
        /// and creates the OvalTrackDefinition.
        /// </summary>
        private void RebuildTrackFromSceneWaypoints()
        {
            Vector3[] localWaypoints = _trackSceneSetup.GetAnchorSpaceWaypoints(_worldAnchor.transform);

            if (localWaypoints.Length < 3)
            {
                UnityEngine.Debug.LogError("[MarkerDetection] TrackSceneSetup has < 3 waypoints!");
                return;
            }

            // Priority: TrackSceneSetup manual difficulties > RacingLineData manual difficulties > auto-detect
            if (_trackSceneSetup.HasManualCurveData)
            {
                _ovalTrack = new OvalTrackDefinition(localWaypoints, _trackSceneSetup.WaypointDifficulties);
                CurveDetectionMode = $"MANUAL (SceneSetup, {localWaypoints.Length} wp)";
            }
            else if (_racingLineData != null && _racingLineData.HasManualCurveData
                && _racingLineData.WaypointDifficulties.Length == localWaypoints.Length)
            {
                _ovalTrack = new OvalTrackDefinition(localWaypoints, _racingLineData.WaypointDifficulties);
                CurveDetectionMode = $"MANUAL (RacingLine, {localWaypoints.Length} wp)";
            }
            else
            {
                _ovalTrack = new OvalTrackDefinition(localWaypoints);
                CurveDetectionMode = $"AUTO (diff.len={_trackSceneSetup.WaypointDifficulties?.Length ?? 0}, wp={localWaypoints.Length})";
            }
            UnityEngine.Debug.Log($"[MarkerDetection] Curve detection: {CurveDetectionMode}");

            UnityEngine.Debug.Log($"[MarkerDetection] Track from SceneSetup: {_ovalTrack.TotalLength:F2}m, " +
                                  $"{_ovalTrack.WaypointCount} waypoints, scaleFactor={_trackScale / _sceneSetupNativeMaxExtent:F6}.");

            // Show debug visualization if available
            if (_trackDebugVisualizer != null && _worldAnchor != null)
            {
                _trackDebugVisualizer.ShowTrack(_ovalTrack, _worldAnchor.transform);
            }
        }

        /// <summary>
        /// Computes world-space AABB of all renderers under a GameObject.
        /// </summary>
        private static Bounds ComputeRendererBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(obj.transform.position, Vector3.zero);

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private void ParentCarToAnchor()
        {
            if (_worldAnchor == null || _carPlaceholder == null) return;

            _carPlaceholder.transform.SetParent(_worldAnchor.transform, false);
            _carPlaceholder.transform.localPosition = Vector3.zero;
            _carPlaceholder.transform.localRotation = Quaternion.identity;

            // Apply the same scale factor to the car that was applied to the track
            if (_trackSceneSetup != null && _sceneSetupNativeMaxExtent > 0.001f)
            {
                float scaleFactor = _trackScale / _sceneSetupNativeMaxExtent;
                _carPlaceholder.ApplyTrackScaleFactor(scaleFactor);
            }

            if (_ovalTrack != null)
            {
                _carPlaceholder.BindTrack(_ovalTrack);
            }

            _carPlaceholder.gameObject.SetActive(true);
            UnityEngine.Debug.Log("[MarkerDetection] Car parented to anchor and bound to oval track.");
        }

        /// <summary>
        /// Updates local scale when track dimensions change.
        /// Position stays at anchor origin — no world-space math needed.
        /// </summary>
        private void ApplyLocalLayout()
        {
            // ═══ PATH A: TrackSceneSetup ═══
            if (_trackSceneSetup != null && _sceneSetupNativeMaxExtent > 0.001f)
            {
                ApplySceneSetupTransform();
                RebuildTrackFromSceneWaypoints();
                if (_carPlaceholder != null)
                {
                    float scaleFactor = _trackScale / _sceneSetupNativeMaxExtent;
                    _carPlaceholder.ApplyTrackScaleFactor(scaleFactor);
                    if (_ovalTrack != null)
                        _carPlaceholder.BindTrack(_ovalTrack);
                }
                return;
            }

            // ═══ PATH B: Legacy ═══
            if (_trackPlaceholder != null)
            {
                _trackPlaceholder.ApplyLocalLayout(TrackWidthMeters, TrackLengthMeters);
            }

            // Rescale 3D model FIRST so we know its rendered size
            if (_trackModelLoader != null)
            {
                _trackModelLoader.Rescale(_trackScale, _heightOffsetMeters);
            }

            // Rebuild oval track with scale derived from model (or _trackScale if no model)
            if (_ovalTrack != null)
            {
                var (scaleX, scaleZ) = DeriveRacingLineScales();
                float height = GetRacingLineHeight();

                if (_racingLineData != null && _racingLineData.Waypoints.Length >= 3)
                    _ovalTrack = new OvalTrackDefinition(_racingLineData, scaleX, height);
                else
                    _ovalTrack = new OvalTrackDefinition(scaleX, scaleZ, height);

                if (_trackVisualBuilder != null)
                {
                    _trackVisualBuilder.Rebuild(_ovalTrack);
                }
                if (_carPlaceholder != null)
                {
                    _carPlaceholder.BindTrack(_ovalTrack);
                }
            }
        }

        /// <summary>
        /// Returns separate X and Z scale factors for OvalTrackDefinition
        /// so the racing line matches the 3D model's rendered XZ size.
        /// Falls back to uniform _trackScale when no model is loaded.
        /// </summary>
        private (float scaleX, float scaleZ) DeriveRacingLineScales()
        {
            if (_trackModelLoader == null)
                return (_trackScale, _trackScale);

            Vector3 rs = _trackModelLoader.RenderedBoundsSize;
            if (rs.x < 0.0001f || rs.z < 0.0001f)
                return (_trackScale, _trackScale);

            if (_racingLineData != null && _racingLineData.Waypoints.Length >= 3)
            {
                // Normalised waypoints: uniform scale is fine
                float modelMaxXZ = Mathf.Max(rs.x, rs.z);
                return (modelMaxXZ, modelMaxXZ);
            }
            else
            {
                // DesignPoints: stretch X and Z independently to match model
                float scaleX = rs.x / OvalTrackDefinition.DesignBoundingWidth;
                float scaleZ = rs.z / OvalTrackDefinition.DesignBoundingHeight;
                return (scaleX, scaleZ);
            }
        }

        /// <summary>
        /// Returns the Y height for the racing line waypoints.
        /// When a 3D model is loaded, uses the model's top surface Y so the car drives on top.
        /// Otherwise falls back to the user's height offset.
        /// </summary>
        private float GetRacingLineHeight()
        {
            if (_trackModelLoader != null)
                return _trackModelLoader.SurfaceY;
            return _heightOffsetMeters;
        }

        public void OnMarkerDetected()
        {
            if (IsTracking) return;
            IsTracking = true;
            ApplyTrackingState(true);
            UnityEngine.Debug.Log("[MarkerDetection] Track anchored — SLAM tracking active.");
        }

        public void OnTrackingLost()
        {
            if (!IsTracking) return;
            IsTracking = false;
            ApplyTrackingState(false);
            _telemetryHooks?.OnTrackingLossDetected();
            UnityEngine.Debug.LogWarning("[MarkerDetection] Tracking lost.");
        }

        private void ApplyTrackingState(bool isTracked)
        {
            _trackPlaceholder?.SetTrackingState(isTracked);

            if (_carPlaceholder != null)
            {
                _carPlaceholder.gameObject.SetActive(isTracked);
            }
        }

        // ── Visual helpers (all children of anchor) ──────────────────

        private void CreateAnchorVisualUnderAnchor()
        {
            if (_worldAnchor == null) return;

            _anchorVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _anchorVisual.name = "AnchorVisual";
            _anchorVisual.transform.SetParent(_worldAnchor.transform, false);
            _anchorVisual.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            _anchorVisual.transform.localScale = Vector3.one * 0.04f;

            Collider col = _anchorVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = _anchorVisual.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(1f, 0.85f, 0f, 0.98f);
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
        }
    }
}
