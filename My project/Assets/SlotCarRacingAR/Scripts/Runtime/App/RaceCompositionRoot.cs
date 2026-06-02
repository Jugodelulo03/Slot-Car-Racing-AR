using System;
using System.Collections;
using System.Reflection;
using SlotCarRacingAR.Runtime.Debug;
using SlotCarRacingAR.Runtime.Features;
using SlotCarRacingAR.Runtime.Infrastructure;
using SlotCarRacingAR.Runtime.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
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
        private ArSetupUI _arSetupUI;
        private TrackStabilityEvaluator _stabilityEvaluator;
        private SharedLobbyState _sharedState;
        private CountdownOverlay _countdownOverlay;
        private RaceHud _raceHud;
        private RacePodiumOverlay _podiumOverlay;
        private ConnectionToast _connectionToast;
        private readonly CarPlaceholder[] _carPresenters = new CarPlaceholder[SharedLobbyState.MaxPlayers + 1];
        [SerializeField] private bool _enableArDebugOverlay;
        private bool _arRuntimeBootstrapStarted;
        private bool _countdownStarted;
        private bool _raceActive;
        private bool _racePresentersConfigured;
        private bool _localPenaltyWasActive;
        private bool _networkDisconnectSubscribed;
        private float _raceElapsedSeconds;
        private byte _lastObservedRematchLobbySignal;
        private readonly RaceCarRuntimeState[] _raceStates =
        {
            null,
            new RaceCarRuntimeState(),
            new RaceCarRuntimeState(),
            new RaceCarRuntimeState(),
            new RaceCarRuntimeState()
        };

        private float _raceMaxSpeedMetersPerSecond = 0.25f;
        private float _raceAccelerationRate = 0.3f;
        private float _raceBrakeRate = 0.6f;
        private float _racePenaltyDurationSeconds = 1.5f;

        private static readonly string[] PlayerCarResourcePaths =
        {
            "",
            "CarModels/RED",
            "CarModels/GREEN",
            "CarModels/YELLOW",
            "CarModels/BLUE"
        };

        private static readonly Color[] PlayerColors =
        {
            Color.white,
            new Color(0.95f, 0.12f, 0.12f),
            new Color(0.1f, 0.85f, 0.25f),
            new Color(1f, 0.75f, 0.1f),
            new Color(0.16f, 0.45f, 1f)
        };

        private static readonly float[] PlayerLaneOffsets =
        {
            0f,
            -0.004f,
            0.004f,
            -0.009f,
            0.009f
        };

        private byte GetLocalPlayerId()
        {
            if (_sharedState == null)
            {
                return 1;
            }

            byte localPlayerId = _sharedState.LocalPlayerId;
            if (localPlayerId != 0)
            {
                return localPlayerId;
            }

            return _sharedState.IsServer ? (byte)1 : (byte)2;
        }

        private static bool IsValidPlayerId(byte playerId)
        {
            return playerId >= 1 && playerId <= SharedLobbyState.MaxPlayers;
        }

        private static string GetPlayerCarResourcePath(byte playerId)
        {
            return IsValidPlayerId(playerId) ? PlayerCarResourcePaths[playerId] : PlayerCarResourcePaths[1];
        }

        private static Color GetPlayerColor(byte playerId)
        {
            return IsValidPlayerId(playerId) ? PlayerColors[playerId] : PlayerColors[1];
        }

        private static float GetPlayerLaneOffset(byte playerId)
        {
            return IsValidPlayerId(playerId) ? PlayerLaneOffsets[playerId] : 0f;
        }

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
            GameAudio.PlayMusic(GameMusic.Menu);
            InitializeRace();

#if UNITY_EDITOR
            EnableArComponents(false);
            SetupEditorCamera();
            ReportRuntimeBootstrapStatus("editor preview: AR runtime disabled");
            // In Editor, marker detection is simulated — show detected + stable immediately
            if (_arSetupUI != null)
            {
                _arSetupUI.ShowMarkerDetected();
                _arSetupUI.UpdateStability(TrackStabilityState.Stable);
                _arSetupUI.UpdateReadySync(false, false);
            }
            // In editor without network, simulate countdown after 2s for testing
            StartCoroutine(EditorSimulateCountdown());
#else
            StartCoroutine(EnsureArRuntimeReady());
#endif
        }

        private void Update()
        {
            if (!_raceActive || _sharedState == null)
            {
                return;
            }

            if (_sharedState.Phase.Value != RacePhase.Racing)
            {
                return;
            }

            if (_sharedState.IsServer)
            {
                TickAuthoritativeRace(Time.deltaTime);
            }

            ApplyAuthoritativePresentation();
        }

#if UNITY_EDITOR
        private void SetupEditorCamera()
        {
            if (_arCamera == null) return;

            // Remove TrackedPoseDriver so camera is free to move
            var poseDriver = _arCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            if (poseDriver != null) poseDriver.enabled = false;

            // Add overhead camera controller
            var controller = _arCamera.GetComponent<SlotCarRacingAR.Runtime.Debug.EditorCameraController>();
            if (controller == null)
                controller = _arCamera.gameObject.AddComponent<SlotCarRacingAR.Runtime.Debug.EditorCameraController>();
            controller.enabled = true;

            // Ensure camera renders something (clear to skybox/solid color)
            _arCamera.clearFlags = CameraClearFlags.SolidColor;
            _arCamera.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        }

        private IEnumerator EditorSimulateCountdown()
        {
            // Wait a moment so UI is visible, then simulate both-ready + countdown
            yield return new WaitForSeconds(2f);

            if (_arSetupUI != null)
            {
                _arSetupUI.UpdateReadySync(true, true);
            }
            _arSetupUI?.Hide();

            // Simulate countdown ticks
            _countdownOverlay?.Show(3);
            yield return new WaitForSeconds(1f);
            _countdownOverlay?.Show(2);
            yield return new WaitForSeconds(1f);
            _countdownOverlay?.Show(1);
            yield return new WaitForSeconds(1f);
            _countdownOverlay?.Show(0); // GO!
            yield return new WaitForSeconds(0.8f);

            StartRace();
        }
#endif

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
            _arDebugOverlay = GetComponentInChildren<ArDebugOverlay>(true);
            _raceHud = GetComponentInChildren<RaceHud>(true);
            _podiumOverlay = GetComponentInChildren<RacePodiumOverlay>(true);
            _connectionToast = GetComponentInChildren<ConnectionToast>(true);
        }

        private void WireSceneDependencies()
        {
            EnsureArCameraTrackedPoseDriver();
            EnsureSurfaceProbe();
            EnsureAnchorManager();
            if (_accelerationInputPlaceholder != null)
            {
                _accelerationInputPlaceholder.Bind(_carPlaceholder);
                _accelerationInputPlaceholder.OnHoldChanged += HandleAccelerationHeldChanged;
            }
            _markerDetectionEntryPoint?.Bind(_trackPlaceholder, _carPlaceholder, _telemetryHooks, _trackedImageManager);
            _markerDetectionEntryPoint?.BindAnchorManager(_arAnchorManager);
            EnsureTrackSizePanel();
            EnsureArDebugOverlay();
            EnsureConnectionToast();
            SubscribeNetworkDisconnectCallbacks();
        }

        private void EnsureConnectionToast()
        {
            if (_connectionToast == null)
            {
                _connectionToast = gameObject.AddComponent<ConnectionToast>();
            }
        }

        private void SubscribeNetworkDisconnectCallbacks()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || _networkDisconnectSubscribed)
            {
                return;
            }

            networkManager.OnClientDisconnectCallback += HandleNetworkClientDisconnected;
            _networkDisconnectSubscribed = true;
        }

        private void UnsubscribeNetworkDisconnectCallbacks()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !_networkDisconnectSubscribed)
            {
                return;
            }

            networkManager.OnClientDisconnectCallback -= HandleNetworkClientDisconnected;
            _networkDisconnectSubscribed = false;
        }

        private void HandleNetworkClientDisconnected(ulong clientId)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && !networkManager.IsServer && clientId == NetworkManager.ServerClientId)
            {
                _connectionToast?.Show("Host desconectado", RetroUi.Red);
                return;
            }

            _connectionToast?.Show("Jugador desconectado", RetroUi.Red);
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
            if (!_enableArDebugOverlay)
            {
                DisableArDebugOverlay();
                return;
            }

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

        private void DisableArDebugOverlay()
        {
            ArDebugOverlay[] overlays = UnityEngine.Object.FindObjectsByType<ArDebugOverlay>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (ArDebugOverlay overlay in overlays)
            {
                if (overlay == null)
                {
                    continue;
                }

                overlay.enabled = false;
            }

            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != null && child.name == "ArDebugOverlayCanvas")
                {
                    Destroy(child.gameObject);
                }
            }
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

            // Create AR setup UI (scanning guidance + detection toast)
            CreateArSetupUI();

            // Create countdown overlay (hidden initially)
            GameObject countdownObj = new GameObject("CountdownOverlay");
            _countdownOverlay = countdownObj.AddComponent<CountdownOverlay>();

            EnsureRaceHud();
            EnsurePodiumOverlay();

            // Disable acceleration input until race starts
            if (_accelerationInputPlaceholder != null)
            {
                _accelerationInputPlaceholder.gameObject.SetActive(false);
            }

            // Subscribe to marker detection event
            if (_markerDetectionEntryPoint != null)
            {
                _markerDetectionEntryPoint.OnTrackAnchored += HandleTrackAnchored;
                _markerDetectionEntryPoint.OnTrackLost += HandleTrackingLost;
            }

            // Find the SharedLobbyState (persists from Lobby via NGO DontDestroyOnLoad)
            SpawnOrFindRaceSetupState();

            UnityEngine.Debug.Log("[Race] Composition root initialized.");
        }

        private void CreateArSetupUI()
        {
            GameObject uiObj = new GameObject("ArSetupUI");
            _arSetupUI = uiObj.AddComponent<ArSetupUI>();
            _arSetupUI.ShowScanning();
            _arSetupUI.OnReadyPressed += HandleLocalReadyPressed;
            _arSetupUI.OnRescanPressed += HandleRescanTrackingPressed;

            // Create stability evaluator
            GameObject stabObj = new GameObject("TrackStabilityEvaluator");
            _stabilityEvaluator = stabObj.AddComponent<TrackStabilityEvaluator>();
            _stabilityEvaluator.OnStabilityChanged += HandleStabilityChanged;
        }

        private void EnsureRaceHud()
        {
            if (_raceHud == null)
            {
                _raceHud = gameObject.AddComponent<RaceHud>();
            }

            _raceHud.BindLocalCar(_carPlaceholder);
            _raceHud.SetVisible(false);
        }

        private void EnsurePodiumOverlay()
        {
            if (_podiumOverlay == null)
            {
                _podiumOverlay = gameObject.AddComponent<RacePodiumOverlay>();
            }

            _podiumOverlay.OnRematchClicked += HandleRematchClicked;
            _podiumOverlay.OnAcceptRematchClicked += HandleAcceptRematchClicked;
            _podiumOverlay.OnReturnToLobbyClicked += HandleReturnToLobbyClicked;
            _podiumOverlay.OnMainMenuClicked += HandleMainMenuClicked;
            _podiumOverlay.Hide();
        }

        private void HandleTrackAnchored()
        {
            EvaluationLog.MarkFlowStep(2, "Detectar pista");

            if (_arSetupUI != null)
            {
                _arSetupUI.ShowMarkerDetected();
            }

            // Start stability evaluation on the track anchor
            if (_stabilityEvaluator != null && _markerDetectionEntryPoint != null)
            {
                // Use the track placeholder's parent (the anchor) as reference
                Transform anchorRef = _trackPlaceholder != null ? _trackPlaceholder.transform.parent : null;
                if (anchorRef != null)
                {
                    _stabilityEvaluator.BeginEvaluation(anchorRef);
                }
                else
                {
                    // Fallback: immediately mark as stable
                    _stabilityEvaluator.BeginEvaluation(_trackPlaceholder != null ? _trackPlaceholder.transform : transform);
                }
            }

            // Subscribe to tracking lost
            if (_markerDetectionEntryPoint != null)
            {
                _markerDetectionEntryPoint.OnTrackAnchored -= HandleTrackAnchored; // one-shot
            }
        }

        private void HandleStabilityChanged(TrackStabilityState state)
        {
            if (_arSetupUI != null)
            {
                _arSetupUI.UpdateStability(state);
            }
        }

        private void SpawnOrFindRaceSetupState()
        {
            // SharedLobbyState persists from Lobby scene via DontDestroyOnLoad (in OnNetworkSpawn)
            StartCoroutine(FindSharedStateWithRetry());
        }

        private IEnumerator FindSharedStateWithRetry()
        {
            // May need a frame for DontDestroyOnLoad objects to be findable after scene load
            for (int attempt = 0; attempt < 10; attempt++)
            {
                // Also search children of NetworkManager (our parenting strategy)
                if (NetworkManager.Singleton != null)
                {
                    _sharedState = NetworkManager.Singleton.GetComponentInChildren<SharedLobbyState>(true);
                }
                if (_sharedState == null)
                {
                    _sharedState = FindAnyObjectByType<SharedLobbyState>();
                }
                if (_sharedState != null)
                {
                    BindSharedState(_sharedState);

                    string role = _sharedState.IsServer ? "Host" : "Guest";
                    if (_arSetupUI != null)
                        _arSetupUI.UpdateConnectionStatus($"{role} | sync OK | {_sharedState.PlayerCount.Value}P", new Color(0.2f, 0.9f, 0.4f));

                    UnityEngine.Debug.Log($"[Race] Found SharedLobbyState (attempt {attempt}). Role={role}, Players={_sharedState.PlayerCount.Value}");
                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            // Last resort: check NetworkManager's spawned objects directly
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
                {
                    SharedLobbyState sls = kvp.Value.GetComponent<SharedLobbyState>();
                    if (sls != null)
                    {
                        BindSharedState(sls);

                        string role = _sharedState.IsServer ? "Host" : "Guest";
                        if (_arSetupUI != null)
                            _arSetupUI.UpdateConnectionStatus($"{role} | sync OK (spawn mgr) | {_sharedState.PlayerCount.Value}P", new Color(0.2f, 0.9f, 0.4f));

                        UnityEngine.Debug.Log($"[Race] Found SharedLobbyState via SpawnManager. Role={role}");
                        yield break;
                    }
                }
            }

            if (_arSetupUI != null)
                _arSetupUI.UpdateConnectionStatus("⚠ Sin conexión de red", new Color(0.95f, 0.3f, 0.3f));
            UnityEngine.Debug.LogWarning("[Race] SharedLobbyState not found after retries.");
        }

        private void BindSharedState(SharedLobbyState sharedState)
        {
            if (sharedState == null)
            {
                return;
            }

            if (_sharedState != null)
            {
                _sharedState.OnReadyStateChanged -= HandleReadyStateChanged;
                _sharedState.OnCountdownTick -= HandleCountdownTick;
                _sharedState.OnPhaseChanged -= HandlePhaseChanged;
                _sharedState.OnRaceStateChanged -= HandleRaceStateChanged;
                _sharedState.OnPlayerCountChanged -= HandleRacePlayerCountChanged;
            }

            _sharedState = sharedState;
            _lastObservedRematchLobbySignal = _sharedState.RematchLobbySignal.Value;
            _sharedState.OnReadyStateChanged += HandleReadyStateChanged;
            _sharedState.OnCountdownTick += HandleCountdownTick;
            _sharedState.OnPhaseChanged += HandlePhaseChanged;
            _sharedState.OnRaceStateChanged += HandleRaceStateChanged;
            _sharedState.OnPlayerCountChanged += HandleRacePlayerCountChanged;
            _raceHud?.Bind(_sharedState);
            _podiumOverlay?.Refresh(_sharedState);
        }

        private void HandleRacePlayerCountChanged(byte oldCount, byte newCount)
        {
            if (newCount < oldCount)
            {
                _connectionToast?.ShowPlayerDisconnected(oldCount, newCount);
            }
        }

        private void HandleLocalReadyPressed(bool ready)
        {
            if (_sharedState != null)
            {
                _sharedState.SetLocalReady(ready);
                if (ready)
                {
                    EvaluationLog.MarkFlowStep(3, "Confirmacion de tracking");
                }

                UnityEngine.Debug.Log($"[Race] Local ready set to {ready}");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Race] Ready pressed but SharedLobbyState not found!");
            }
        }

        private void HandleRescanTrackingPressed()
        {
            if (_sharedState != null && _sharedState.Phase.Value != RacePhase.Setup)
            {
                _connectionToast?.Show("La carrera ya va a empezar", RetroUi.Yellow);
                return;
            }

            UnityEngine.Debug.Log("[Race] Rescan tracking requested.");

            if (_sharedState != null)
            {
                _sharedState.SetLocalReady(false);
            }

            _arSetupUI?.RevokeReady();
            _stabilityEvaluator?.StopEvaluation();
            _trackSizePanel?.SetAdjustmentsAvailable(true);

            if (_markerDetectionEntryPoint != null)
            {
                _markerDetectionEntryPoint.OnTrackAnchored -= HandleTrackAnchored;
                _markerDetectionEntryPoint.OnTrackAnchored += HandleTrackAnchored;
                _markerDetectionEntryPoint.ResetAnchor();
            }
            else
            {
                UnityEngine.Debug.LogWarning("[Race] Rescan requested but MarkerDetectionEntryPoint is missing.");
            }

            _arSession?.Reset();
            _arSetupUI?.ShowScanning();
        }

        private void HandleReadyStateChanged(bool hostReady, bool guestReady)
        {
            if (_arSetupUI != null)
            {
                _arSetupUI.UpdateReadySync(_sharedState);
            }

            if (_sharedState != null && _sharedState.AllReady && !_countdownStarted)
            {
                _countdownStarted = true;
                UnityEngine.Debug.Log("[Race] All players ready - freezing track and starting countdown.");

                // Freeze AR tracking updates (track position is locked)
                FreezeTrack();
                _trackSizePanel?.SetAdjustmentsAvailable(false);

                // Host drives the countdown
                if (_sharedState != null && _sharedState.IsServer)
                {
                    StartCoroutine(RunCountdownCoroutine());
                }
            }
        }

        private void FreezeTrack()
        {
            // Disable further AR tracking updates — the track stays where it is
            if (_trackedImageManager != null)
            {
                _trackedImageManager.enabled = false;
            }

            // Hide setup UI
            if (_arSetupUI != null)
            {
                _arSetupUI.Hide();
            }

            UnityEngine.Debug.Log("[Race] Track frozen — AR image tracking disabled.");
        }

        private IEnumerator RunCountdownCoroutine()
        {
            // Host-driven countdown: 3, 2, 1, GO
            _sharedState.BeginCountdown(); // sets Phase=Countdown, CountdownValue=3
            yield return new WaitForSeconds(1f);

            _sharedState.TickCountdown(2);
            yield return new WaitForSeconds(1f);

            _sharedState.TickCountdown(1);
            yield return new WaitForSeconds(1f);

            _sharedState.TickCountdown(0); // GO!
            yield return new WaitForSeconds(0.8f);

            _sharedState.BeginRacing(); // Phase=Racing
        }

        private void HandleCountdownTick(byte value)
        {
            if (_countdownOverlay != null)
            {
                _countdownOverlay.Show(value);
            }
        }

        private void HandlePhaseChanged(RacePhase phase)
        {
            UnityEngine.Debug.Log($"[Race] Phase changed to: {phase}");

            switch (phase)
            {
                case RacePhase.Countdown:
                    // Freeze track on guest side too
                    FreezeTrack();
                    _trackSizePanel?.SetAdjustmentsAvailable(false);
                    GameAudio.StopLocalEngine();
                    break;

                case RacePhase.Racing:
                    StartRace();
                    break;

                case RacePhase.Finished:
                    FinishRacePresentation();
                    break;
            }
        }

        private void StartRace()
        {
            int activePlayers = _sharedState != null ? _sharedState.PlayerCount.Value : 0;
            EvaluationLog.MarkFlowStep(4, "Iniciar carrera");
            EvaluationLog.CompleteSetupAtRaceStart(activePlayers);
            if (_sharedState != null && _sharedState.IsServer)
            {
                EvaluationLog.RecordRaceStarted(activePlayers);
            }

            // Hide countdown overlay
            if (_countdownOverlay != null)
            {
                _countdownOverlay.Hide();
            }

            // Enable acceleration input
            if (_accelerationInputPlaceholder != null)
            {
                _accelerationInputPlaceholder.gameObject.SetActive(true);
            }

            ConfigureRacePresenters();
            _raceActive = true;
            _localPenaltyWasActive = false;
            GameAudio.StopLocalEngine();
            _raceHud?.SetVisible(true);
            _podiumOverlay?.Hide();
            _trackSizePanel?.SetAdjustmentsAvailable(false);
            GameAudio.PlayMusic(GameMusic.Race);
            GameAudio.Play(GameSfx.RaceStart);

            if (_sharedState != null && _sharedState.IsServer)
            {
                RefreshRaceTuningFromCar();
                for (byte playerId = 1; playerId <= SharedLobbyState.MaxPlayers; playerId++)
                {
                    _raceStates[playerId].Reset();
                }

                _raceElapsedSeconds = 0f;
                UnityEngine.Debug.Log("[Race] Host authoritative race simulation started.");
            }

            UnityEngine.Debug.Log("[Race] Race started! Input enabled.");
        }

        private void FinishRacePresentation()
        {
            _raceActive = false;
            GameAudio.StopLocalEngine();
            if (_accelerationInputPlaceholder != null)
            {
                _accelerationInputPlaceholder.gameObject.SetActive(false);
            }

            _raceHud?.SetVisible(false);
            _localPenaltyWasActive = false;
            ApplyAuthoritativePresentation();
            _podiumOverlay?.Show(_sharedState);
            GameAudio.PlayMusic(GameMusic.Menu);
            GameAudio.Play(GameSfx.Finish);
            EvaluationLog.MarkFlowStep(5, "Terminar carrera");
            if (_sharedState != null && _sharedState.IsServer)
            {
                int activePlayers = 0;
                float[] finishTimes = new float[SharedLobbyState.MaxPlayers];
                for (byte playerId = 1; playerId <= SharedLobbyState.MaxPlayers; playerId++)
                {
                    if (_sharedState.HasPlayer(playerId))
                    {
                        activePlayers++;
                    }

                    finishTimes[playerId - 1] = _sharedState.GetFinishTime(playerId);
                }

                EvaluationLog.RecordRaceFinishedByAll(
                    activePlayers,
                    _sharedState.GetFinishedCount(),
                    _sharedState.WinnerPlayerId.Value,
                    finishTimes);
            }

            UnityEngine.Debug.Log("[Race] Race finished. Winner player=" + (_sharedState != null ? _sharedState.WinnerPlayerId.Value : 0));
        }

        private void HandleRaceStateChanged()
        {
            if (_sharedState == null)
            {
                return;
            }

            if (_sharedState.RematchLobbySignal.Value != _lastObservedRematchLobbySignal)
            {
                _lastObservedRematchLobbySignal = _sharedState.RematchLobbySignal.Value;
                SceneManager.LoadScene("Lobby");
                return;
            }

            if (_sharedState.Phase.Value == RacePhase.Finished && _podiumOverlay != null && _podiumOverlay.IsVisible)
            {
                _podiumOverlay.Refresh(_sharedState);
            }
        }

        private void HandleAccelerationHeldChanged(bool isHeld)
        {
            if (_sharedState != null && _sharedState.Phase.Value == RacePhase.Racing)
            {
                _sharedState.SetLocalAccelerationHeld(isHeld);
                GameAudio.SetLocalEngineAccelerating(isHeld);
                return;
            }

            GameAudio.StopLocalEngine();
        }

        private void ConfigureRacePresenters()
        {
            if (_racePresentersConfigured || _carPlaceholder == null || _carPlaceholder.Track == null)
            {
                return;
            }

            byte localPlayerId = GetLocalPlayerId();

            RefreshRaceTuningFromCar();

            _carPresenters[localPlayerId] = _carPlaceholder;
            _carPlaceholder.SetLaneOffset(GetPlayerLaneOffset(localPlayerId));
            if (!_carPlaceholder.LoadVisualFromResource(GetPlayerCarResourcePath(localPlayerId)))
            {
                _carPlaceholder.SetVisualColor(GetPlayerColor(localPlayerId));
            }
            _carPlaceholder.SetPlayerMarker(GetPlayerColor(localPlayerId), true);

            if (_sharedState != null)
            {
                for (byte playerId = 1; playerId <= SharedLobbyState.MaxPlayers; playerId++)
                {
                    if (playerId == localPlayerId || !_sharedState.HasPlayer(playerId))
                    {
                        continue;
                    }

                    EnsureRemoteCarPresenter(playerId, _carPlaceholder.VisualRoot);
                }
            }

            _carPlaceholder.SetExternalRaceStateEnabled(_sharedState != null);
            _raceHud?.BindLocalCar(_carPlaceholder);
            _raceHud?.SetMaxSpeed(_raceMaxSpeedMetersPerSecond);

            _racePresentersConfigured = true;
        }

        private void EnsureRemoteCarPresenter(byte playerId, Transform transformTemplate)
        {
            if (!IsValidPlayerId(playerId) || _carPresenters[playerId] != null || _carPlaceholder == null || _carPlaceholder.Track == null)
            {
                return;
            }

            GameObject remoteCarObject = new GameObject("RemoteCarPlaceholder_P" + playerId);
            remoteCarObject.transform.SetParent(_carPlaceholder.transform.parent, false);
            remoteCarObject.transform.localScale = _carPlaceholder.transform.localScale;
            CarPlaceholder presenter = remoteCarObject.AddComponent<CarPlaceholder>();
            presenter.SetLaneOffset(GetPlayerLaneOffset(playerId));
            presenter.SetRideHeightMeters(_carPlaceholder.RideHeightMeters);
            if (!presenter.LoadVisualFromResource(GetPlayerCarResourcePath(playerId), transformTemplate))
            {
                presenter.SetVisualColor(GetPlayerColor(playerId));
            }

            presenter.SetPlayerMarker(GetPlayerColor(playerId), false);
            presenter.SetExternalRaceStateEnabled(true);
            presenter.BindTrack(_carPlaceholder.Track);
            _carPresenters[playerId] = presenter;
        }

        private void RefreshRaceTuningFromCar()
        {
            if (_carPlaceholder == null)
            {
                return;
            }

            _raceMaxSpeedMetersPerSecond = _carPlaceholder.MaxSpeed;
            _raceAccelerationRate = _carPlaceholder.AccelerationRate;
            _raceBrakeRate = _carPlaceholder.BrakeRate;
            _racePenaltyDurationSeconds = _carPlaceholder.SpinOutDuration;
        }

        private void TickAuthoritativeRace(float deltaTime)
        {
            if (_carPlaceholder == null || _carPlaceholder.Track == null || _sharedState == null)
            {
                return;
            }

            _raceElapsedSeconds += deltaTime;

            OvalTrackDefinition track = _carPlaceholder.Track;
            bool allActivePlayersFinished = true;
            float[] finishTimes = new float[SharedLobbyState.MaxPlayers];
            byte winnerPlayerId = 0;
            float winningTime = float.MaxValue;

            for (byte playerId = 1; playerId <= SharedLobbyState.MaxPlayers; playerId++)
            {
                RaceCarRuntimeState state = _raceStates[playerId];
                if (!_sharedState.HasPlayer(playerId))
                {
                    state.Reset();
                    finishTimes[playerId - 1] = -1f;
                    continue;
                }

                StepRaceCar(state, _sharedState.GetAccelerationHeld(playerId), track, deltaTime);
                MarkFinishedIfNeeded(state, playerId);
                _sharedState.PublishRaceState(
                    playerId,
                    state.Progress,
                    state.Speed,
                    state.Lap,
                    state.PenaltyRemainingSeconds > 0f);
                if (state.Finished)
                {
                    _sharedState.PublishFinishTime(playerId, state.FinishTimeSeconds);
                }

                finishTimes[playerId - 1] = state.FinishTimeSeconds;
                allActivePlayersFinished &= state.Finished;
                if (state.Finished && state.FinishTimeSeconds < winningTime)
                {
                    winningTime = state.FinishTimeSeconds;
                    winnerPlayerId = playerId;
                }
            }

            if (allActivePlayersFinished && winnerPlayerId != 0)
            {
                _sharedState.FinishRace(winnerPlayerId, finishTimes);
            }
        }

        private void StepRaceCar(RaceCarRuntimeState state, bool accelerating, OvalTrackDefinition track, float deltaTime)
        {
            if (state.Finished)
            {
                state.Speed = 0f;
                state.PenaltyRemainingSeconds = 0f;
                return;
            }

            if (track == null || track.TotalLength <= 0f)
            {
                return;
            }

            if (state.PenaltyRemainingSeconds > 0f)
            {
                state.PenaltyRemainingSeconds = Mathf.Max(0f, state.PenaltyRemainingSeconds - deltaTime);
                state.Speed = 0f;
                return;
            }

            state.Speed = accelerating
                ? Mathf.MoveTowards(state.Speed, _raceMaxSpeedMetersPerSecond, _raceAccelerationRate * deltaTime)
                : Mathf.MoveTowards(state.Speed, 0f, _raceBrakeRate * deltaTime);

            CurveDifficulty difficulty = track.GetDifficultyAtProgress(state.Progress);
            float safeSpeed = _carPlaceholder != null
                ? _carPlaceholder.GetSafeSpeedForDifficulty(difficulty)
                : _raceMaxSpeedMetersPerSecond;

            if (difficulty > CurveDifficulty.Gentle && state.Speed > safeSpeed)
            {
                float triggerSpeed = state.Speed;
                state.Speed = 0f;
                state.PenaltyRemainingSeconds = _racePenaltyDurationSeconds;
                UnityEngine.Debug.Log(
                    $"[Race] Curve penalty. Difficulty={difficulty} Speed={triggerSpeed:F3} Safe={safeSpeed:F3}");
                return;
            }

            float distance = state.Speed * deltaTime;
            state.Progress += distance / track.TotalLength;
            while (state.Progress >= 1f)
            {
                state.Progress -= 1f;
                if (state.Lap < byte.MaxValue)
                {
                    state.Lap++;
                }
            }
        }

        private void MarkFinishedIfNeeded(RaceCarRuntimeState state, byte playerId)
        {
            if (state.Finished || state.Lap < SharedLobbyState.RaceLapTarget)
            {
                return;
            }

            state.Finished = true;
            state.FinishTimeSeconds = _raceElapsedSeconds;
            state.Speed = 0f;
            state.Progress = 0f;
            state.PenaltyRemainingSeconds = 0f;
            UnityEngine.Debug.Log($"[Race] Player {playerId} finished at {state.FinishTimeSeconds:F2}s.");
        }

        private void ApplyAuthoritativePresentation()
        {
            if (_sharedState == null || _carPlaceholder == null)
            {
                return;
            }

            ConfigureRacePresenters();

            byte localPlayerId = GetLocalPlayerId();
            bool localFinished = _sharedState.GetFinishTime(localPlayerId) >= 0f;
            UpdateLocalInputAfterFinish(localFinished);

            bool localPenaltyActive = _sharedState.GetPenaltyActive(localPlayerId);
            _carPlaceholder.ApplyAuthoritativeState(
                _sharedState.GetProgress(localPlayerId),
                _sharedState.GetSpeed(localPlayerId),
                _sharedState.GetLap(localPlayerId),
                localPenaltyActive);

            if (localPenaltyActive && !_localPenaltyWasActive)
            {
                GameAudio.Play(GameSfx.Penalty);
            }

            _localPenaltyWasActive = localPenaltyActive;

            for (byte playerId = 1; playerId <= SharedLobbyState.MaxPlayers; playerId++)
            {
                if (playerId == localPlayerId)
                {
                    continue;
                }

                CarPlaceholder presenter = _carPresenters[playerId];
                if (presenter == null)
                {
                    continue;
                }

                bool hasPlayer = _sharedState.HasPlayer(playerId);
                presenter.gameObject.SetActive(hasPlayer);
                if (!hasPlayer)
                {
                    continue;
                }

                presenter.ApplyAuthoritativeState(
                    _sharedState.GetProgress(playerId),
                    _sharedState.GetSpeed(playerId),
                    _sharedState.GetLap(playerId),
                    _sharedState.GetPenaltyActive(playerId));
            }
        }

        private void UpdateLocalInputAfterFinish(bool localFinished)
        {
            if (_accelerationInputPlaceholder == null || _sharedState == null || _sharedState.Phase.Value != RacePhase.Racing)
            {
                return;
            }

            if (localFinished)
            {
                if (_sharedState.GetAccelerationHeld(GetLocalPlayerId()))
                {
                    _sharedState.SetLocalAccelerationHeld(false);
                }

                GameAudio.StopLocalEngine();
                if (_accelerationInputPlaceholder.gameObject.activeSelf)
                {
                    _accelerationInputPlaceholder.gameObject.SetActive(false);
                }

                return;
            }

            if (!_accelerationInputPlaceholder.gameObject.activeSelf)
            {
                _accelerationInputPlaceholder.gameObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            GameAudio.StopLocalEngine();

            if (_accelerationInputPlaceholder != null)
            {
                _accelerationInputPlaceholder.OnHoldChanged -= HandleAccelerationHeldChanged;
            }

            if (_sharedState != null)
            {
                _sharedState.OnReadyStateChanged -= HandleReadyStateChanged;
                _sharedState.OnCountdownTick -= HandleCountdownTick;
                _sharedState.OnPhaseChanged -= HandlePhaseChanged;
                _sharedState.OnRaceStateChanged -= HandleRaceStateChanged;
                _sharedState.OnPlayerCountChanged -= HandleRacePlayerCountChanged;
            }

            UnsubscribeNetworkDisconnectCallbacks();

            if (_podiumOverlay != null)
            {
                _podiumOverlay.OnRematchClicked -= HandleRematchClicked;
                _podiumOverlay.OnAcceptRematchClicked -= HandleAcceptRematchClicked;
                _podiumOverlay.OnReturnToLobbyClicked -= HandleReturnToLobbyClicked;
                _podiumOverlay.OnMainMenuClicked -= HandleMainMenuClicked;
            }

            if (_markerDetectionEntryPoint != null)
            {
                _markerDetectionEntryPoint.OnTrackAnchored -= HandleTrackAnchored;
                _markerDetectionEntryPoint.OnTrackLost -= HandleTrackingLost;
            }

            if (_arSetupUI != null)
            {
                _arSetupUI.OnReadyPressed -= HandleLocalReadyPressed;
                _arSetupUI.OnRescanPressed -= HandleRescanTrackingPressed;
            }
        }

        private sealed class RaceCarRuntimeState
        {
            public float Progress;
            public float Speed;
            public byte Lap;
            public float PenaltyRemainingSeconds;
            public bool Finished;
            public float FinishTimeSeconds;

            public void Reset()
            {
                Progress = 0f;
                Speed = 0f;
                Lap = 0;
                PenaltyRemainingSeconds = 0f;
                Finished = false;
                FinishTimeSeconds = -1f;
            }
        }

        private void HandleRematchClicked()
        {
            _sharedState?.RequestRematch();
            _podiumOverlay?.Refresh(_sharedState);
        }

        private void HandleAcceptRematchClicked()
        {
            _sharedState?.AcceptRematch();
            _podiumOverlay?.Refresh(_sharedState);
        }

        private void HandleReturnToLobbyClicked()
        {
            if (_sharedState != null)
            {
                _sharedState.ReturnToLobbyFromPodium();
            }
            else
            {
                SceneManager.LoadScene("Lobby");
            }
        }

        private void HandleMainMenuClicked()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                Destroy(NetworkManager.Singleton.gameObject);
            }

            if (_sharedState != null)
            {
                Destroy(_sharedState.gameObject);
            }

            SceneManager.LoadScene("Lobby");
        }

        private void HandleTrackingLost()
        {
            if (_stabilityEvaluator != null)
            {
                _stabilityEvaluator.StopEvaluation();
            }
            if (_arSetupUI != null)
            {
                _arSetupUI.ShowTrackingLost();
                _arSetupUI.RevokeReady();
            }
            if (_sharedState != null && _sharedState.IsServer)
            {
                _sharedState.RevokeAllReadiness();
            }
        }
    }
}
