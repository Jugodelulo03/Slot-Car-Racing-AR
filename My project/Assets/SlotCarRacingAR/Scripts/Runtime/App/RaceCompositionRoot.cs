using System;
using System.Collections;
using System.Reflection;
using SlotCarRacingAR.Runtime.Debug;
using SlotCarRacingAR.Runtime.Features;
using SlotCarRacingAR.Runtime.Infrastructure;
using SlotCarRacingAR.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SlotCarRacingAR.Runtime.App
{
    /// <summary>
    /// Race scene composition root. Owns active race runtime composition only.
    /// </summary>
    public sealed class RaceCompositionRoot : MonoBehaviour
    {
        private MarkerDetectionEntryPoint _markerDetectionEntryPoint;
        private TrackPlaceholder _trackPlaceholder;
        private CarPlaceholder _carPlaceholder;
        private AccelerationInputPlaceholder _accelerationInputPlaceholder;
        private TelemetryHooks _telemetryHooks;
        private ArDebugOverlay _arDebugOverlay;
        private ARSession _arSession;
        private ARInputManager _arInputManager;
        private ARCameraManager _arCameraManager;
        private ARTrackedImageManager _trackedImageManager;
        private ARCameraBackground _arCameraBackground;
        private ARPlaneManager _arPlaneManager;
        private ARRaycastManager _arRaycastManager;
        private ARAnchorManager _arAnchorManager;
        private Camera _arCamera;
        private ArSurfaceProbe _arSurfaceProbe;
        private SlotCarRacingAR.Runtime.UI.TrackSizePanel _trackSizePanel;
        private bool _arRuntimeBootstrapStarted;

        private void Awake()
        {
            EnsureInputSystemUiModule();
            CacheSceneReferences();
            WireSceneDependencies();
        }

        private static void EnsureInputSystemUiModule()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            }

            if (eventSystem == null)
            {
                return;
            }

            StandaloneInputModule legacyInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            InputSystemUIInputModule inputSystemUiModule = eventSystem.GetComponent<InputSystemUIInputModule>();

            if (inputSystemUiModule == null)
            {
                inputSystemUiModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputSystemUiModule.actionsAsset == null)
            {
                inputSystemUiModule.AssignDefaultActions();
            }

            if (legacyInputModule != null)
            {
                legacyInputModule.enabled = false;
                Destroy(legacyInputModule);
            }
        }

        private void Start()
        {
            InitializeRace();

#if UNITY_EDITOR
            EnableArComponents(false);
            ReportRuntimeBootstrapStatus("editor preview: AR runtime disabled");
#else
            StartCoroutine(EnsureArRuntimeReady());
#endif
        }

        private void CacheSceneReferences()
        {
            _markerDetectionEntryPoint = GetComponentInChildren<MarkerDetectionEntryPoint>(true);
            _trackPlaceholder = GetComponentInChildren<TrackPlaceholder>(true);
            _carPlaceholder = GetComponentInChildren<CarPlaceholder>(true);
            _accelerationInputPlaceholder = GetComponentInChildren<AccelerationInputPlaceholder>(true);
            _telemetryHooks = GetComponentInChildren<TelemetryHooks>(true);
            _arSession = GetComponentInChildren<ARSession>(true);
            _arInputManager = GetComponentInChildren<ARInputManager>(true);
            _arCameraManager = GetComponentInChildren<ARCameraManager>(true);
            _trackedImageManager = GetComponentInChildren<ARTrackedImageManager>(true);
            _arPlaneManager = GetComponentInChildren<ARPlaneManager>(true);
            _arRaycastManager = GetComponentInChildren<ARRaycastManager>(true);
            _arAnchorManager = GetComponentInChildren<ARAnchorManager>(true);
            _arCamera = _arCameraManager != null ? _arCameraManager.GetComponent<Camera>() : GetComponentInChildren<Camera>(true);
            if (_arCamera != null) _arCamera.nearClipPlane = 0.01f; // Allow close-up viewing of AR models
            _arCameraBackground = _arCamera != null ? _arCamera.GetComponent<ARCameraBackground>() : GetComponentInChildren<ARCameraBackground>(true);
            _arSurfaceProbe = GetComponent<ArSurfaceProbe>();
            _arDebugOverlay = GetComponent<ArDebugOverlay>();
        }

        private void WireSceneDependencies()
        {
            EnsureArCameraTrackedPoseDriver();
            EnsureSurfaceProbe();
            EnsureAnchorManager();
            _accelerationInputPlaceholder?.Bind(_carPlaceholder);
            _markerDetectionEntryPoint?.Bind(_trackPlaceholder, _carPlaceholder, _telemetryHooks, _trackedImageManager);
            _markerDetectionEntryPoint?.BindAnchorManager(_arAnchorManager);
            EnsureTrackSizePanel();
            EnsureArDebugOverlay();
        }

        private void EnsureSurfaceProbe()
        {
            GameObject arOriginObject = _trackedImageManager != null
                ? _trackedImageManager.gameObject
                : _arCameraManager != null && _arCameraManager.transform.parent != null
                    ? _arCameraManager.transform.parent.gameObject
                    : gameObject;

            if (_arPlaneManager == null)
            {
                _arPlaneManager = arOriginObject.GetComponent<ARPlaneManager>();
                if (_arPlaneManager == null)
                {
                    _arPlaneManager = arOriginObject.AddComponent<ARPlaneManager>();
                    _arPlaneManager.enabled = false;
                    UnityEngine.Debug.Log("[Race] Added missing ARPlaneManager to AR origin.");
                }
            }

            _arPlaneManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;

            if (_arRaycastManager == null)
            {
                _arRaycastManager = arOriginObject.GetComponent<ARRaycastManager>();
                if (_arRaycastManager == null)
                {
                    _arRaycastManager = arOriginObject.AddComponent<ARRaycastManager>();
                    _arRaycastManager.enabled = false;
                    UnityEngine.Debug.Log("[Race] Added missing ARRaycastManager to AR origin.");
                }
            }

            if (_arSurfaceProbe == null)
            {
                _arSurfaceProbe = GetComponent<ArSurfaceProbe>();
                if (_arSurfaceProbe == null)
                {
                    _arSurfaceProbe = gameObject.AddComponent<ArSurfaceProbe>();
                    _arSurfaceProbe.enabled = false;
                }
            }

            _arSurfaceProbe.Bind(_arCamera, _arPlaneManager, _arRaycastManager);
        }

        private void EnsureArCameraTrackedPoseDriver()
        {
            if (_arCamera == null)
            {
                return;
            }

            TrackedPoseDriver trackedPoseDriver = _arCamera.GetComponent<TrackedPoseDriver>();
            if (trackedPoseDriver == null)
            {
                trackedPoseDriver = _arCamera.gameObject.AddComponent<TrackedPoseDriver>();
                ConfigureTrackedPoseDriver(trackedPoseDriver);
                UnityEngine.Debug.Log("[Race] Added missing TrackedPoseDriver to Main Camera.");
            }

            DisableLegacyArPoseDriver(_arCamera.gameObject);
        }

        private void EnsureAnchorManager()
        {
            GameObject arOriginObject = _trackedImageManager != null
                ? _trackedImageManager.gameObject
                : _arCameraManager != null && _arCameraManager.transform.parent != null
                    ? _arCameraManager.transform.parent.gameObject
                    : gameObject;

            if (_arAnchorManager == null)
            {
                _arAnchorManager = arOriginObject.GetComponent<ARAnchorManager>();
                if (_arAnchorManager == null)
                {
                    _arAnchorManager = arOriginObject.AddComponent<ARAnchorManager>();
                    _arAnchorManager.enabled = false;
                    UnityEngine.Debug.Log("[Race] Added missing ARAnchorManager to AR origin.");
                }
            }
        }

        private void EnsureTrackSizePanel()
        {
            if (_trackSizePanel == null)
            {
                _trackSizePanel = GetComponent<SlotCarRacingAR.Runtime.UI.TrackSizePanel>();
                if (_trackSizePanel == null)
                {
                    _trackSizePanel = gameObject.AddComponent<SlotCarRacingAR.Runtime.UI.TrackSizePanel>();
                }
            }

            _trackSizePanel.Bind(_markerDetectionEntryPoint);
        }

        private void EnsureArDebugOverlay()
        {
            if (_arDebugOverlay == null)
            {
                _arDebugOverlay = gameObject.AddComponent<ArDebugOverlay>();
            }

            _arDebugOverlay.Bind(
                _markerDetectionEntryPoint,
                _telemetryHooks,
                _arSession,
                _arCameraManager,
                _trackedImageManager,
                _arCameraBackground,
                _arCamera,
                _arSurfaceProbe);

            _arDebugOverlay.SetRuntimeBootstrapStatus("scene wired");
        }

        private IEnumerator EnsureArRuntimeReady()
        {
            if (_arRuntimeBootstrapStarted)
            {
                yield break;
            }

            _arRuntimeBootstrapStarted = true;
            ReportRuntimeBootstrapStatus("starting");

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                ReportRuntimeBootstrapStatus("requesting camera permission");

                bool permissionResolved = false;
                bool permissionGranted = false;
                UnityEngine.Android.PermissionCallbacks callbacks = new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionGranted += _ =>
                {
                    permissionGranted = true;
                    permissionResolved = true;
                };
                callbacks.PermissionDenied += _ => permissionResolved = true;
                callbacks.PermissionDeniedAndDontAskAgain += _ => permissionResolved = true;

                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, callbacks);

                while (!permissionResolved)
                {
                    yield return null;
                }

                if (!permissionGranted)
                {
                    ReportRuntimeBootstrapStatus("camera permission denied");
                    yield break;
                }
            }
#endif

            EnableArComponents(true);

            object xrManager = GetXrManager();
            if (xrManager == null)
            {
                ReportRuntimeBootstrapStatus("missing XR manager");
                yield break;
            }

            bool automaticLoading = GetBoolProperty(xrManager, "automaticLoading");
            bool automaticRunning = GetBoolProperty(xrManager, "automaticRunning");
            object activeLoader = GetInstanceProperty(xrManager, "activeLoader");
            bool isInitializationComplete = GetBoolProperty(xrManager, "isInitializationComplete");

            if (automaticLoading)
            {
                ReportRuntimeBootstrapStatus("waiting for XR auto-init");
                float initializationDeadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < initializationDeadline)
                {
                    isInitializationComplete = GetBoolProperty(xrManager, "isInitializationComplete");
                    activeLoader = GetInstanceProperty(xrManager, "activeLoader");
                    if (isInitializationComplete && activeLoader != null)
                    {
                        break;
                    }

                    yield return null;
                }
            }
            else if (!isInitializationComplete || activeLoader == null)
            {
                ReportRuntimeBootstrapStatus("initializing XR loader");
                object initializeLoaderResult = InvokeInstanceMethod(xrManager, "InitializeLoader");
                if (initializeLoaderResult is IEnumerator initializeLoaderEnumerator)
                {
                    yield return initializeLoaderEnumerator;
                }

                isInitializationComplete = GetBoolProperty(xrManager, "isInitializationComplete");
                activeLoader = GetInstanceProperty(xrManager, "activeLoader");
            }

            if (!isInitializationComplete || activeLoader == null)
            {
                ReportRuntimeBootstrapStatus("XR loader unavailable after wait");
                yield break;
            }

            if (!automaticRunning && !HasActiveArSubsystems())
            {
                ReportRuntimeBootstrapStatus($"starting subsystems manually: {GetLoaderName(activeLoader)}");
                InvokeInstanceMethod(xrManager, "StartSubsystems");
            }
            else if (automaticRunning && !HasActiveArSubsystems())
            {
                ReportRuntimeBootstrapStatus($"waiting for subsystem auto-start: {GetLoaderName(activeLoader)}");
                float subsystemDeadline = Time.realtimeSinceStartup + 2f;
                while (Time.realtimeSinceStartup < subsystemDeadline && !HasActiveArSubsystems())
                {
                    yield return null;
                }
            }

            yield return RestartArFoundationComponents();

            float startupDeadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < startupDeadline)
            {
                if (ARSession.state == ARSessionState.SessionInitializing || ARSession.state == ARSessionState.SessionTracking)
                {
                    ReportRuntimeBootstrapStatus($"session advanced: {ARSession.state}");
                    yield break;
                }

                yield return null;
            }

            if (ARSession.state == ARSessionState.Ready)
            {
                ReportRuntimeBootstrapStatus("session stuck in Ready after restart");
                yield break;
            }

            ReportRuntimeBootstrapStatus(
                HasActiveArSubsystems()
                    ? $"subsystems running: {GetLoaderName(activeLoader)}"
                    : $"loader active, waiting: {GetLoaderName(activeLoader)}");
        }

        private static void ConfigureTrackedPoseDriver(TrackedPoseDriver trackedPoseDriver)
        {
            trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            trackedPoseDriver.ignoreTrackingState = true;

            InputAction positionAction = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
            positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");

            InputAction rotationAction = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
            rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");

            trackedPoseDriver.positionInput = new InputActionProperty(positionAction);
            trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);
        }

        private static void DisableLegacyArPoseDriver(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null)
                {
                    continue;
                }

                if (component.GetType().FullName == "UnityEngine.XR.ARFoundation.ARPoseDriver" && component is Behaviour behaviour)
                {
                    behaviour.enabled = false;
                }
            }
        }

        private bool HasActiveArSubsystems()
        {
            return (_arSession != null && _arSession.subsystem != null)
                || (_arCameraManager != null && _arCameraManager.subsystem != null)
                || (_trackedImageManager != null && _trackedImageManager.subsystem != null);
        }

        private IEnumerator RestartArFoundationComponents()
        {
            ReportRuntimeBootstrapStatus("restarting AR components");
            EnableArComponents(false);
            yield return null;
            EnableArComponents(true);
            _arSession?.Reset();
            yield return null;
        }

        private static object GetXrManager()
        {
            Type xrGeneralSettingsType = Type.GetType("UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management");
            if (xrGeneralSettingsType == null)
            {
                return null;
            }

            object xrGeneralSettings = xrGeneralSettingsType
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null);

            return GetInstanceProperty(xrGeneralSettings, "Manager");
        }

        private static object GetInstanceProperty(object instance, string propertyName)
        {
            return instance
                ?.GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(instance);
        }

        private static bool GetBoolProperty(object instance, string propertyName)
        {
            object value = GetInstanceProperty(instance, propertyName);
            return value is bool boolValue && boolValue;
        }

        private static object InvokeInstanceMethod(object instance, string methodName)
        {
            return instance
                ?.GetType()
                .GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
                ?.Invoke(instance, null);
        }

        private static string GetLoaderName(object loader)
        {
            if (loader is UnityEngine.Object loaderObject)
            {
                return string.IsNullOrWhiteSpace(loaderObject.name)
                    ? loaderObject.GetType().Name
                    : loaderObject.name;
            }

            return loader?.GetType().Name ?? "none";
        }

        private void EnableArComponents(bool enabled)
        {
            if (_arSession != null)
            {
                _arSession.enabled = enabled;
            }

            if (_arInputManager != null)
            {
                _arInputManager.enabled = enabled;
            }

            if (_arCameraManager != null)
            {
                _arCameraManager.enabled = enabled;
            }

            if (_trackedImageManager != null)
            {
                _trackedImageManager.enabled = enabled;
            }

            if (_arPlaneManager != null)
            {
                _arPlaneManager.enabled = enabled;
            }

            if (_arRaycastManager != null)
            {
                _arRaycastManager.enabled = enabled;
            }

            if (_arAnchorManager != null)
            {
                _arAnchorManager.enabled = enabled;
            }

            if (_arCameraBackground == null && _arCamera != null)
            {
                _arCameraBackground = _arCamera.GetComponent<ARCameraBackground>();
            }

            if (_arCameraBackground != null)
            {
                _arCameraBackground.enabled = enabled;
            }

            if (_arSurfaceProbe != null)
            {
                _arSurfaceProbe.enabled = enabled;
            }
        }

        private void ReportRuntimeBootstrapStatus(string status)
        {
            _arDebugOverlay?.SetRuntimeBootstrapStatus(status);
            UnityEngine.Debug.Log($"[Race] AR bootstrap: {status}");
        }

        private void InitializeRace()
        {
            if (_markerDetectionEntryPoint == null)
            {
                UnityEngine.Debug.LogWarning("[Race] Missing MarkerDetectionEntryPoint in scene scaffold.");
            }

            if (_trackPlaceholder == null || _carPlaceholder == null)
            {
                UnityEngine.Debug.LogWarning("[Race] Missing world placeholders for track or car.");
            }

            if (_accelerationInputPlaceholder == null)
            {
                UnityEngine.Debug.LogWarning("[Race] Missing acceleration input placeholder.");
            }

            if (_arSession == null || _arCameraManager == null || _arCamera == null)
            {
                UnityEngine.Debug.LogWarning("[Race] Missing AR session or camera components required for device tracking.");
            }

            UnityEngine.Debug.Log("[Race] Composition root initialized.");
        }
    }
}
