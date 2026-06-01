using System;
using System.Text;
using SlotCarRacingAR.Runtime.Features;
using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SlotCarRacingAR.Runtime.Debug
{
    [DisallowMultipleComponent]
    public sealed class ArDebugOverlay : MonoBehaviour
    {
        private readonly StringBuilder _buffer = new StringBuilder(768);

        [SerializeField] private MarkerDetectionEntryPoint _markerDetectionEntryPoint;
        [SerializeField] private TelemetryHooks _telemetryHooks;
        [SerializeField] private ARSession _arSession;
        [SerializeField] private ARCameraManager _arCameraManager;
        [SerializeField] private ARTrackedImageManager _trackedImageManager;
        [SerializeField] private ARCameraBackground _arCameraBackground;
        [SerializeField] private Camera _arCamera;
        [SerializeField] private ArSurfaceProbe _arSurfaceProbe;

        private ARCameraManager _subscribedCameraManager;
        private Canvas _overlayCanvas;
        private RectTransform _overlayPanel;
        private Text _overlayText;
        private string _runtimeBootstrapStatus = "not-started";
        private bool _isBound;
        private float _fpsAccumulator;
        private int _fpsFrameCount;
        private float _currentFps;
        private int _cameraFrameCount;
        private float _lastCameraFrameTime = -1f;
        private bool _hasPoseSample;
        private Vector3 _lastPosePosition;
        private Quaternion _lastPoseRotation;
        private float _lastPoseDeltaMeters;
        private float _lastPoseDeltaDegrees;
        private float _lastPoseMovementTime = -1f;

        public void Bind(
            MarkerDetectionEntryPoint markerDetectionEntryPoint,
            TelemetryHooks telemetryHooks,
            ARSession arSession,
            ARCameraManager arCameraManager,
            ARTrackedImageManager trackedImageManager,
            ARCameraBackground arCameraBackground,
            Camera arCamera,
            ArSurfaceProbe arSurfaceProbe)
        {
            _markerDetectionEntryPoint = markerDetectionEntryPoint;
            _telemetryHooks = telemetryHooks;
            _arSession = arSession;
            _arCameraManager = arCameraManager;
            _trackedImageManager = trackedImageManager;
            _arCameraBackground = arCameraBackground;
            _arCamera = arCamera;
            _arSurfaceProbe = arSurfaceProbe;
            _isBound = true;
            EnsureOverlayVisuals();
            ResubscribeCameraFrames();
        }

        public void SetRuntimeBootstrapStatus(string runtimeBootstrapStatus)
        {
            _runtimeBootstrapStatus = string.IsNullOrWhiteSpace(runtimeBootstrapStatus)
                ? "unknown"
                : runtimeBootstrapStatus;
        }

        private void Awake()
        {
        }

        private void OnEnable()
        {
            if (!_isBound)
            {
                return;
            }

            EnsureOverlayVisuals();
            ResubscribeCameraFrames();

            if (_overlayCanvas != null)
            {
                _overlayCanvas.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (_subscribedCameraManager != null)
            {
                _subscribedCameraManager.frameReceived -= OnCameraFrameReceived;
                _subscribedCameraManager = null;
            }

            if (_overlayCanvas != null)
            {
                _overlayCanvas.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (_overlayCanvas != null)
            {
                Destroy(_overlayCanvas.gameObject);
            }
        }

        private void Update()
        {
            if (!_isBound)
            {
                return;
            }

            EnsureOverlayVisuals();
            ResolveMissingReferences();
            UpdateFps();
            UpdatePoseState();

            if (_overlayText != null)
            {
                _overlayText.text = BuildOverlayText();
            }
        }

        private void ResolveMissingReferences()
        {
            _markerDetectionEntryPoint ??= GetComponentInChildren<MarkerDetectionEntryPoint>(true);
            _telemetryHooks ??= GetComponentInChildren<TelemetryHooks>(true);
            _arSession ??= GetComponentInChildren<ARSession>(true);
            _trackedImageManager ??= GetComponentInChildren<ARTrackedImageManager>(true);
            _arCameraManager ??= GetComponentInChildren<ARCameraManager>(true);
            _arCameraBackground ??= GetComponentInChildren<ARCameraBackground>(true);
            _arSurfaceProbe ??= GetComponent<ArSurfaceProbe>();

            if (_arCamera == null && _arCameraManager != null)
            {
                _arCamera = _arCameraManager.GetComponent<Camera>();
            }

            _arCamera ??= GetComponentInChildren<Camera>(true);
            ResubscribeCameraFrames();
        }

        private void ResubscribeCameraFrames()
        {
            if (_subscribedCameraManager == _arCameraManager)
            {
                return;
            }

            if (_subscribedCameraManager != null)
            {
                _subscribedCameraManager.frameReceived -= OnCameraFrameReceived;
            }

            _subscribedCameraManager = _arCameraManager;

            if (isActiveAndEnabled && _subscribedCameraManager != null)
            {
                _subscribedCameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs _) 
        {
            _cameraFrameCount++;
            _lastCameraFrameTime = Time.unscaledTime;
        }

        private void UpdateFps()
        {
            _fpsAccumulator += Time.unscaledDeltaTime;
            _fpsFrameCount++;

            if (_fpsAccumulator < 0.5f)
            {
                return;
            }

            _currentFps = _fpsFrameCount / _fpsAccumulator;
            _fpsAccumulator = 0f;
            _fpsFrameCount = 0;
        }

        private void UpdatePoseState()
        {
            if (_arCamera == null)
            {
                _hasPoseSample = false;
                _lastPoseDeltaMeters = 0f;
                _lastPoseDeltaDegrees = 0f;
                return;
            }

            Transform cameraTransform = _arCamera.transform;
            Vector3 currentPosition = cameraTransform.localPosition;
            Quaternion currentRotation = cameraTransform.localRotation;

            if (_hasPoseSample)
            {
                _lastPoseDeltaMeters = Vector3.Distance(currentPosition, _lastPosePosition);
                _lastPoseDeltaDegrees = Quaternion.Angle(currentRotation, _lastPoseRotation);

                if (_lastPoseDeltaMeters > 0.0005f || _lastPoseDeltaDegrees > 0.1f)
                {
                    _lastPoseMovementTime = Time.unscaledTime;
                }
            }

            _lastPosePosition = currentPosition;
            _lastPoseRotation = currentRotation;
            _hasPoseSample = true;
        }

        private string BuildOverlayText()
        {
            CountTrackedImages(out int trackedImageCount, out int currentlyTrackingCount);

            _buffer.Clear();
            _buffer.AppendLine("AR DEBUG OVERLAY");
            _buffer.Append("fps: ").Append(_currentFps.ToString("F1")).AppendLine();
            _buffer.Append("camera permission: ").AppendLine(GetCameraPermissionStatus());
            _buffer.Append("runtime bootstrap: ").AppendLine(_runtimeBootstrapStatus);
            _buffer.Append("ar session state: ").Append(ARSession.state).Append(" | reason: ").Append(ARSession.notTrackingReason).AppendLine();
            _buffer.Append("session component: ").Append(FormatPresence(_arSession)).Append(" | enabled: ").Append(FormatBool(_arSession != null && _arSession.enabled)).Append(" | subsystem: ").Append(FormatSubsystemPresence(_arSession != null && _arSession.subsystem != null)).Append(" | running: ").Append(FormatBool(IsSubsystemRunning(_arSession != null ? _arSession.subsystem : null))).AppendLine();
            _buffer.Append("camera manager: ").Append(FormatPresence(_arCameraManager)).Append(" | enabled: ").Append(FormatBool(_arCameraManager != null && _arCameraManager.enabled)).Append(" | subsystem: ").Append(FormatSubsystemPresence(_arCameraManager != null && _arCameraManager.subsystem != null)).Append(" | running: ").Append(FormatBool(IsSubsystemRunning(_arCameraManager != null ? _arCameraManager.subsystem : null))).AppendLine();
            _buffer.Append("camera subsystem permission: ").Append(FormatBool(_arCameraManager != null && _arCameraManager.permissionGranted)).Append(" | render mode: ").Append(_arCameraManager != null ? _arCameraManager.currentRenderingMode.ToString() : "n/a").AppendLine();
            _buffer.Append("camera background: ").Append(FormatPresence(_arCameraBackground)).Append(" | enabled: ").Append(FormatBool(_arCameraBackground != null && _arCameraBackground.enabled)).Append(" | rendering: ").Append(FormatBool(_arCameraBackground != null && _arCameraBackground.backgroundRenderingEnabled)).Append(" | mode: ").Append(_arCameraBackground != null ? _arCameraBackground.currentRenderingMode.ToString() : "n/a").AppendLine();
            _buffer.Append("camera frames: ").Append(_cameraFrameCount).Append(" | last frame: ").Append(FormatAge(_lastCameraFrameTime)).AppendLine();
            _buffer.Append("pose driver: ").AppendLine(GetPoseDriverStatus());
            _buffer.Append("camera local pos: ").Append(FormatVector3(_lastPosePosition)).Append(" | delta: ").Append(_lastPoseDeltaMeters.ToString("F4")).Append("m / ").Append(_lastPoseDeltaDegrees.ToString("F1")).Append("deg").AppendLine();
            _buffer.Append("last pose movement: ").AppendLine(FormatAge(_lastPoseMovementTime));
            _buffer.Append("track anchor: ").Append(_markerDetectionEntryPoint != null ? "present" : "missing").Append(" | detected: ").Append(_markerDetectionEntryPoint != null && _markerDetectionEntryPoint.HasAnchor ? "YES" : "no").Append(" | tracking: ").Append(_markerDetectionEntryPoint != null && _markerDetectionEntryPoint.IsTracking ? "true" : "false").Append(" | anchor: ").Append(_markerDetectionEntryPoint != null ? _markerDetectionEntryPoint.AnchorStatus : "n/a").Append(" | stable: ").Append(_markerDetectionEntryPoint != null ? _markerDetectionEntryPoint.StableFrames.ToString() : "n/a").AppendLine();
            _buffer.Append("reference library: ").Append(_trackedImageManager != null && _trackedImageManager.referenceLibrary != null ? _trackedImageManager.referenceLibrary.count.ToString() : "missing").Append(" | tracked images: ").Append(trackedImageCount).Append(" total / ").Append(currentlyTrackingCount).Append(" active").AppendLine();
            _buffer.Append("surface probe: ").Append(_arSurfaceProbe != null ? (_arSurfaceProbe.HasPlacement ? "locked" : "searching") : "missing").Append(" | planes: ").Append(_arSurfaceProbe != null ? _arSurfaceProbe.PlaneCount.ToString() : "n/a").Append(" total / ").Append(_arSurfaceProbe != null ? _arSurfaceProbe.TrackingPlaneCount.ToString() : "n/a").Append(" tracking").Append(" | last hit: ");
            if (_arSurfaceProbe != null && _arSurfaceProbe.HasPlacement)
            {
                _buffer.Append(_arSurfaceProbe.LastHitDistanceMeters.ToString("F2")).Append("m @ ").Append(FormatVector3(_arSurfaceProbe.LastHitPosition)).Append(" | age: ").Append(_arSurfaceProbe.LastHitAgeSeconds >= 0f ? _arSurfaceProbe.LastHitAgeSeconds.ToString("F2") + "s" : "n/a");
            }
            else
            {
                _buffer.Append("none");
            }

            _buffer.AppendLine();
            _buffer.Append("track size: ").Append(_markerDetectionEntryPoint != null ? _markerDetectionEntryPoint.TrackWidthMeters.ToString("F2") : "n/a").Append("m x ").Append(_markerDetectionEntryPoint != null ? _markerDetectionEntryPoint.TrackLengthMeters.ToString("F2") : "n/a").Append("m");
            if (_markerDetectionEntryPoint != null && !_markerDetectionEntryPoint.HasAnchor)
            {
                _buffer.Append(" | waiting for track-anchor marker");
            }

            _buffer.AppendLine();
            _buffer.Append("tracking losses: ").Append(_telemetryHooks != null ? _telemetryHooks.TrackingLossCount.ToString() : "n/a").AppendLine();

            // --- CAR & CURVE DIAGNOSTICS ---
            CarPlaceholder car = _markerDetectionEntryPoint != null ? _markerDetectionEntryPoint.Car : null;
            _buffer.Append("--- CAR ---").AppendLine();
            if (car != null)
            {
                _buffer.Append("speed: ").Append(car.Speed.ToString("F3")).Append(" / max ").Append(car.MaxSpeed.ToString("F3")).Append(" m/s").AppendLine();
                _buffer.Append("safe: ").Append(car.CurrentSafeSpeed.ToString("F3")).Append(" m/s | angle: ").Append(car.CurrentCurvatureAngle.ToString("F1")).Append("°").AppendLine();
                _buffer.Append("difficulty: ").Append(car.CurrentDifficulty.ToString()).Append(" | state: ").Append(car.StateLabel).AppendLine();
                _buffer.Append("progress: ").Append(car.TrackProgress.ToString("F3")).Append(" | lap: ").Append(car.LapCount).AppendLine();
                _buffer.Append("curves: ").Append(car.TrackCurvePercentage.ToString("F1")).Append("% | length: ").Append(car.TrackTotalLength.ToString("F2")).AppendLine();
                if (_markerDetectionEntryPoint != null)
                    _buffer.Append("detection: ").Append(_markerDetectionEntryPoint.CurveDetectionMode).AppendLine();
            }
            else
            {
                _buffer.Append("car: not bound").AppendLine();
            }
            _buffer.Append("-----------").AppendLine();

            _buffer.Append("--- 3D MODEL ---").AppendLine();
            _buffer.Append(TrackModelLoader.DiagnosticLog).AppendLine();
            _buffer.Append("----------------").AppendLine();
            _buffer.Append("graphics api: ").Append(SystemInfo.graphicsDeviceType).AppendLine();
            _buffer.Append("screen: ").Append(Screen.width).Append('x').Append(Screen.height).Append(" | orientation: ").Append(Screen.orientation);

            return _buffer.ToString();
        }

        private string GetPoseDriverStatus()
        {
            if (_arCamera == null)
            {
                return "missing camera";
            }

            Component[] components = _arCamera.GetComponents<Component>();
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;
                if (typeName.EndsWith("PoseDriver", StringComparison.Ordinal))
                {
                    return typeName;
                }
            }

            return "missing";
        }

        private void CountTrackedImages(out int totalCount, out int activelyTrackingCount)
        {
            totalCount = 0;
            activelyTrackingCount = 0;

            if (_trackedImageManager == null)
            {
                return;
            }

            foreach (ARTrackedImage trackedImage in _trackedImageManager.trackables)
            {
                totalCount++;
                if (trackedImage.trackingState == TrackingState.Tracking)
                {
                    activelyTrackingCount++;
                }
            }
        }

        private string GetCameraPermissionStatus()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string androidPermission = UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera)
                ? "granted"
                : "missing";
            string cameraSubsystemPermission = _arCameraManager != null && _arCameraManager.permissionGranted
                ? "granted"
                : "missing";
            return androidPermission + " | subsystem: " + cameraSubsystemPermission;
#else
            return "editor-or-not-android";
#endif
        }

        private void EnsureOverlayVisuals()
        {
            if (_overlayCanvas == null)
            {
                GameObject canvasObject = new GameObject("ArDebugOverlayCanvas");
                canvasObject.transform.SetParent(transform, false);

                _overlayCanvas = canvasObject.AddComponent<Canvas>();
                _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _overlayCanvas.sortingOrder = short.MaxValue;
                _overlayCanvas.pixelPerfect = false;

                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;

                GameObject panelObject = new GameObject("Panel");
                panelObject.transform.SetParent(canvasObject.transform, false);

                _overlayPanel = panelObject.AddComponent<RectTransform>();
                _overlayPanel.anchorMin = new Vector2(0f, 1f);
                _overlayPanel.anchorMax = new Vector2(0f, 1f);
                _overlayPanel.pivot = new Vector2(0f, 1f);

                Image background = panelObject.AddComponent<Image>();
                background.color = new Color(0f, 0f, 0f, 0.72f);
                background.raycastTarget = false;

                GameObject textObject = new GameObject("Text");
                textObject.transform.SetParent(panelObject.transform, false);

                RectTransform textRect = textObject.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(12f, 10f);
                textRect.offsetMax = new Vector2(-12f, -10f);

                _overlayText = textObject.AddComponent<Text>();
                _overlayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _overlayText.color = Color.white;
                _overlayText.alignment = TextAnchor.UpperLeft;
                _overlayText.horizontalOverflow = HorizontalWrapMode.Overflow;
                _overlayText.verticalOverflow = VerticalWrapMode.Overflow;
                _overlayText.raycastTarget = false;
                _overlayText.text = "AR DEBUG OVERLAY";
            }

            if (_overlayPanel != null)
            {
                _overlayPanel.anchoredPosition = new Vector2(16f, -16f);
                _overlayPanel.sizeDelta = new Vector2(
                    Mathf.Min(Screen.width - 32f, 860f),
                    Mathf.Min(Screen.height * 0.52f, 420f));
            }

            if (_overlayText != null)
            {
                _overlayText.fontSize = Mathf.Clamp(Screen.height / 42, 18, 28);
            }
        }

        private static string FormatAge(float timestamp)
        {
            if (timestamp < 0f)
            {
                return "never";
            }

            return (Time.unscaledTime - timestamp).ToString("F2") + "s ago";
        }

        private static string FormatPresence(Component component)
        {
            return component != null ? "present" : "missing";
        }

        private static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string FormatSubsystemPresence(bool hasSubsystem)
        {
            return hasSubsystem ? "ready" : "missing";
        }

        private static bool IsSubsystemRunning(object subsystem)
        {
            if (subsystem == null)
            {
                return false;
            }

            System.Reflection.PropertyInfo runningProperty = subsystem.GetType().GetProperty("running");
            object value = runningProperty?.GetValue(subsystem);
            return value is bool boolValue && boolValue;
        }

        private static string FormatVector3(Vector3 value)
        {
            return "(" + value.x.ToString("F3") + ", " + value.y.ToString("F3") + ", " + value.z.ToString("F3") + ")";
        }
    }
}
