using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using SlotCarRacingAR.Runtime.Infrastructure;
using SlotCarRacingAR.Runtime.UI;

namespace SlotCarRacingAR.Runtime.App
{
    /// <summary>
    /// Lobby scene composition root. Owns session creation, join,
    /// readiness, and pre-race coordination.
    /// </summary>
    public sealed class LobbyCompositionRoot : MonoBehaviour
    {
        private const string SharedLobbyStateResourcePath = "NetworkPrefabs/SharedLobbyState";
        private const uint SharedLobbyStateFallbackHash = 0xF2CA0E01u;

        private LobbyStartScreen _startScreen;
        private SessionManager _sessionManager;
        private LobbySessionUI _sessionUI;
        private LobbyJoinUI _joinUI;
        private SharedLobbyUI _sharedLobbyUI;
        private SharedLobbyState _sharedLobbyState;
        private LanDiscovery _lanDiscovery;
        private GameObject _lobbyStatePrefab;
        private bool _lobbyStatePrefabRegistered;
        private bool _lobbyStatePrefabIsRuntimeFallback;

        private void Awake()
        {
            EnsureInputSystemUiModule();
            CreateSessionManager();
            CreateLanDiscovery();
            CreateStartScreen();
        }

        private void OnDestroy()
        {
            if (_startScreen != null)
            {
                _startScreen.OnCreateMatchClicked -= OnCreateMatchSelected;
                _startScreen.OnJoinMatchClicked -= OnJoinMatchSelected;
            }

            if (_sessionManager != null)
            {
                _sessionManager.OnSessionStateChanged -= OnSessionStateChanged;
            }

            if (_sessionUI != null)
            {
                _sessionUI.OnRetryClicked -= OnRetrySession;
                _sessionUI.OnBackClicked -= OnBackToStartScreen;
            }

            if (_joinUI != null)
            {
                _joinUI.OnConnectClicked -= OnGuestConnect;
                _joinUI.OnRetryClicked -= OnRetryGuestSession;
                _joinUI.OnBackClicked -= OnBackToStartScreen;
            }

            if (_sharedLobbyUI != null)
            {
                _sharedLobbyUI.OnContinueClicked -= OnContinueToRace;
            }

            if (_sharedLobbyState != null)
            {
                _sharedLobbyState.OnPlayerCountChanged -= OnLobbyPlayerCountChanged;
            }
        }

        private void CreateStartScreen()
        {
            // Find the canvas in the scene (PlaceholderCanvas)
            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                UnityEngine.Debug.LogError("[Lobby] No Canvas found — cannot create start screen.");
                return;
            }

            // Deactivate the old placeholder button if present
            Transform oldPlaceholder = canvas.transform.Find("StartRaceButtonPlaceholder");
            if (oldPlaceholder != null)
            {
                oldPlaceholder.gameObject.SetActive(false);
            }

            // Create the start screen as a child of the Canvas
            GameObject screenObj = new GameObject("LobbyStartScreen");
            screenObj.transform.SetParent(canvas.transform, false);

            RectTransform screenRect = screenObj.AddComponent<RectTransform>();
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;

            _startScreen = screenObj.AddComponent<LobbyStartScreen>();
            _startScreen.OnCreateMatchClicked += OnCreateMatchSelected;
            _startScreen.OnJoinMatchClicked += OnJoinMatchSelected;

            // Create the session UI (initially hidden) under the same canvas
            GameObject sessionObj = new GameObject("LobbySessionUI");
            sessionObj.transform.SetParent(canvas.transform, false);

            RectTransform sessionRect = sessionObj.AddComponent<RectTransform>();
            sessionRect.anchorMin = Vector2.zero;
            sessionRect.anchorMax = Vector2.one;
            sessionRect.offsetMin = Vector2.zero;
            sessionRect.offsetMax = Vector2.zero;

            _sessionUI = sessionObj.AddComponent<LobbySessionUI>();
            _sessionUI.OnRetryClicked += OnRetrySession;
            _sessionUI.OnBackClicked += OnBackToStartScreen;
            sessionObj.SetActive(false);

            // Create the join UI (initially hidden) under the same canvas
            GameObject joinObj = new GameObject("LobbyJoinUI");
            joinObj.transform.SetParent(canvas.transform, false);

            RectTransform joinRect = joinObj.AddComponent<RectTransform>();
            joinRect.anchorMin = Vector2.zero;
            joinRect.anchorMax = Vector2.one;
            joinRect.offsetMin = Vector2.zero;
            joinRect.offsetMax = Vector2.zero;

            _joinUI = joinObj.AddComponent<LobbyJoinUI>();
            _joinUI.OnConnectClicked += OnGuestConnect;
            _joinUI.OnRetryClicked += OnRetryGuestSession;
            _joinUI.OnBackClicked += OnBackToStartScreen;
            joinObj.SetActive(false);

            // Create the shared lobby UI (initially hidden)
            GameObject sharedLobbyObj = new GameObject("SharedLobbyUI");
            sharedLobbyObj.transform.SetParent(canvas.transform, false);

            RectTransform sharedRect = sharedLobbyObj.AddComponent<RectTransform>();
            sharedRect.anchorMin = Vector2.zero;
            sharedRect.anchorMax = Vector2.one;
            sharedRect.offsetMin = Vector2.zero;
            sharedRect.offsetMax = Vector2.zero;

            _sharedLobbyUI = sharedLobbyObj.AddComponent<SharedLobbyUI>();
            _sharedLobbyUI.OnContinueClicked += OnContinueToRace;
            sharedLobbyObj.SetActive(false);

            PrepareSharedLobbyStatePrefab();
            _sessionManager?.SetSharedLobbyStatePrefab(_lobbyStatePrefab);
        }

        private void PrepareSharedLobbyStatePrefab()
        {
            _lobbyStatePrefabIsRuntimeFallback = false;
            _lobbyStatePrefab = Resources.Load<GameObject>(SharedLobbyStateResourcePath);

            if (_lobbyStatePrefab != null
                && _lobbyStatePrefab.TryGetComponent(out NetworkObject networkObject)
                && _lobbyStatePrefab.TryGetComponent(out SharedLobbyState _))
            {
                UnityEngine.Debug.Log("[Lobby] Loaded SharedLobbyState prefab from Resources. PrefabHash=" + networkObject.PrefabIdHash);
                return;
            }

            if (_lobbyStatePrefab != null)
            {
                UnityEngine.Debug.LogError("[Lobby] SharedLobbyState prefab resource is missing NetworkObject or SharedLobbyState.");
            }
            else
            {
                UnityEngine.Debug.LogError("[Lobby] SharedLobbyState prefab resource not found at Resources/" + SharedLobbyStateResourcePath + ".");
            }

            _lobbyStatePrefab = CreateRuntimeSharedLobbyStatePrefab();
            _lobbyStatePrefabIsRuntimeFallback = true;
        }

        private static GameObject CreateRuntimeSharedLobbyStatePrefab()
        {
            GameObject prefab = new GameObject("SharedLobbyStateRuntimePrefab");
            NetworkObject networkObject = prefab.AddComponent<NetworkObject>();
            networkObject.SetSceneObjectStatus(false);
            prefab.AddComponent<SharedLobbyState>();
            prefab.SetActive(false);
            DontDestroyOnLoad(prefab);

            if (!TrySetNetworkObjectHash(networkObject, SharedLobbyStateFallbackHash))
            {
                UnityEngine.Debug.LogError("[Lobby] Runtime SharedLobbyState fallback cannot set a stable NetworkObject hash.");
            }

            return prefab;
        }

        private static bool TrySetNetworkObjectHash(NetworkObject networkObject, uint hash)
        {
            var hashField = typeof(NetworkObject).GetField(
                "GlobalObjectIdHash",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (hashField == null)
            {
                return false;
            }

            hashField.SetValue(networkObject, hash);
            return true;
        }

        private void CreateSessionManager()
        {
            GameObject managerObj = new GameObject("SessionManager");
            managerObj.transform.SetParent(transform, false);
            _sessionManager = managerObj.AddComponent<SessionManager>();
            _sessionManager.OnSessionStateChanged += OnSessionStateChanged;
        }

        private void CreateLanDiscovery()
        {
            GameObject discoveryObj = new GameObject("LanDiscovery");
            discoveryObj.transform.SetParent(transform, false);
            _lanDiscovery = discoveryObj.AddComponent<LanDiscovery>();
            _lanDiscovery.OnHostDiscovered += OnHostDiscovered;
        }

        private void OnHostDiscovered(string ip, int port, string hostName)
        {
            if (_joinUI != null && _joinUI.gameObject.activeSelf)
            {
                _joinUI.ShowDiscoveredHost(ip, port, hostName);
            }
        }

        private static void EnsureInputSystemUiModule()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = Object.FindAnyObjectByType<EventSystem>();
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
            InitializeLobby();
        }

        private void InitializeLobby()
        {
            UnityEngine.Debug.Log("[Lobby] Composition root initialized. Start screen ready.");
        }

        /// <summary>
        /// Called when the player selects "Create Match" on the start screen.
        /// Starts a host session via SessionManager and shows session UI.
        /// </summary>
        public void OnCreateMatchSelected()
        {
            UnityEngine.Debug.Log("[Lobby] Create Match selected — starting host session.");
            ShowSessionUI();
            _sessionManager.SetSharedLobbyStatePrefab(_lobbyStatePrefab);
            _sessionManager.StartHostSession();
        }

        /// <summary>
        /// Called when the player selects "Join Match" on the start screen.
        /// Shows the join UI with IP input for connecting to the host.
        /// </summary>
        public void OnJoinMatchSelected()
        {
            UnityEngine.Debug.Log("[Lobby] Join Match selected — showing join UI.");
            ShowJoinUI();
        }

        private void OnGuestConnect(string hostIp, int port)
        {
            UnityEngine.Debug.Log("[Lobby] Guest connecting to " + hostIp + ":" + port);
            _sessionManager.SetSharedLobbyStatePrefab(_lobbyStatePrefab);
            _sessionManager.StartGuestSession(hostIp, port);
        }

        private void OnRetryGuestSession()
        {
            _sessionManager.RetryGuestSession();
        }

        private void OnSessionStateChanged(SessionState state)
        {
            UnityEngine.Debug.Log("[Lobby] Session state → " + state);

            if (_sessionManager.Role == PlayerRole.Host)
            {
                string hostAddr = _sessionManager.GetLocalIPAddress() + ":" + _sessionManager.BoundPort;
                _sessionUI.UpdateState(state, hostAddr, _sessionManager.FailureReason);

                // Start LAN broadcast once host is ready
                if (state == SessionState.WaitingForPlayer)
                {
                    string localIp = _sessionManager.GetLocalIPAddress();
                    _lanDiscovery.StartBroadcasting(localIp, _sessionManager.BoundPort);
                }
            }
            else if (_sessionManager.Role == PlayerRole.Guest)
            {
                _joinUI.UpdateState(state, _sessionManager.FailureReason);
            }

            if (state == SessionState.Connected)
            {
                // Show shared lobby instead of auto-transitioning to race
                Invoke(nameof(EnterSharedLobby), 1.0f);
            }
        }

        private void EnterSharedLobby()
        {
            _lanDiscovery.StopAll();
            ShowSharedLobbyUI();
            RegisterLobbyStatePrefab();

            // If guest made it here, the connection is established — show 2 players immediately
            if (_sessionManager.Role == PlayerRole.Guest)
            {
                _sharedLobbyUI.UpdatePlayerCount(2, PlayerRole.Guest, _sessionManager.GetLocalIPAddress());
                _sharedLobbyUI.ShowConnectionConfirmation();
            }

            // Host spawns the SharedLobbyState networked object
            if (_sessionManager.Role == PlayerRole.Host)
            {
                SpawnSharedLobbyState();
            }
            else
            {
                // Guest: find the spawned SharedLobbyState for disconnect detection
                Invoke(nameof(FindSharedLobbyState), 0.5f);
            }
        }

        private void RegisterLobbyStatePrefab()
        {
            if (_lobbyStatePrefabRegistered) return;
            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null || _lobbyStatePrefab == null) return;
            if (nm.NetworkConfig.Prefabs.Contains(_lobbyStatePrefab))
            {
                _lobbyStatePrefabRegistered = true;
                return;
            }

            nm.AddNetworkPrefab(_lobbyStatePrefab);
            _lobbyStatePrefabRegistered = true;
        }

        private void SpawnSharedLobbyState()
        {
            if (_lobbyStatePrefab == null) return;
            if (_sharedLobbyState != null && _sharedLobbyState.IsSpawned) return;

            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening)
            {
                UnityEngine.Debug.LogError("[Lobby] Cannot spawn SharedLobbyState before NetworkManager is listening.");
                return;
            }

            RegisterLobbyStatePrefab();

            NetworkObject networkObject;
            if (_lobbyStatePrefabIsRuntimeFallback)
            {
                GameObject instance = Instantiate(_lobbyStatePrefab);
                instance.SetActive(true);
                networkObject = instance.GetComponent<NetworkObject>();
                networkObject.Spawn();
            }
            else
            {
                networkObject = NetworkObject.InstantiateAndSpawn(_lobbyStatePrefab, nm);
            }

            if (networkObject == null)
            {
                UnityEngine.Debug.LogError("[Lobby] Failed to spawn SharedLobbyState NetworkObject.");
                return;
            }

            _sharedLobbyState = networkObject.GetComponent<SharedLobbyState>();
            if (_sharedLobbyState == null)
            {
                UnityEngine.Debug.LogError("[Lobby] Spawned SharedLobbyState object is missing SharedLobbyState component.");
                return;
            }

            _sharedLobbyState.OnPlayerCountChanged += OnLobbyPlayerCountChanged;

            // Initial UI update
            _sharedLobbyUI.UpdatePlayerCount(_sharedLobbyState.PlayerCount.Value, _sessionManager.Role, _sessionManager.GetLocalIPAddress());
            if (_sharedLobbyState.PlayerCount.Value >= 2)
            {
                _sharedLobbyUI.ShowConnectionConfirmation();
            }
        }

        private void FindSharedLobbyState()
        {
            SharedLobbyState found = FindAnyObjectByType<SharedLobbyState>();
            if (found != null)
            {
                _sharedLobbyState = found;
                _sharedLobbyState.OnPlayerCountChanged += OnLobbyPlayerCountChanged;
                _sharedLobbyUI.UpdatePlayerCount(_sharedLobbyState.PlayerCount.Value, _sessionManager.Role, _sessionManager.GetLocalIPAddress());
                if (_sharedLobbyState.PlayerCount.Value >= 2)
                {
                    _sharedLobbyUI.ShowConnectionConfirmation();
                }
                else
                {
                    // NetworkVariable sync may still be in flight — recheck shortly
                    Invoke(nameof(RecheckPlayerCount), 0.5f);
                }
            }
            else
            {
                // Retry once more
                Invoke(nameof(FindSharedLobbyState), 1.0f);
            }
        }

        private int _recheckAttempts;

        private void RecheckPlayerCount()
        {
            if (_sharedLobbyState == null) return;
            byte count = _sharedLobbyState.PlayerCount.Value;
            UnityEngine.Debug.Log("[Lobby] Recheck PlayerCount=" + count + " attempt=" + _recheckAttempts);
            _sharedLobbyUI.UpdatePlayerCount(count, _sessionManager.Role, _sessionManager.GetLocalIPAddress());
            if (count >= 2)
            {
                _sharedLobbyUI.ShowConnectionConfirmation();
                _recheckAttempts = 0;
            }
            else if (_recheckAttempts < 5)
            {
                _recheckAttempts++;
                Invoke(nameof(RecheckPlayerCount), 1.0f);
            }
            else
            {
                _recheckAttempts = 0;
            }
        }

        private void OnLobbyPlayerCountChanged(byte oldCount, byte newCount)
        {
            _sharedLobbyUI.UpdatePlayerCount(newCount, _sessionManager.Role, _sessionManager.GetLocalIPAddress());

            if (oldCount < 2 && newCount >= 2)
            {
                _sharedLobbyUI.ShowConnectionConfirmation();
            }
            else if (oldCount >= 2 && newCount < 2)
            {
                _sharedLobbyUI.ShowDisconnected();
            }
        }

        private void OnContinueToRace()
        {
            if (_sharedLobbyState != null)
            {
                TransitionToRace();
            }
            else
            {
                // SharedLobbyState not yet replicated — wait for it before transitioning
                StartCoroutine(WaitForSharedStateAndTransition());
            }
        }

        private System.Collections.IEnumerator WaitForSharedStateAndTransition()
        {
            UnityEngine.Debug.Log("[Lobby] Waiting for SharedLobbyState before race transition...");
            for (int i = 0; i < 20; i++) // up to 10 seconds
            {
                _sharedLobbyState = FindAnyObjectByType<SharedLobbyState>();
                if (_sharedLobbyState != null)
                {
                    UnityEngine.Debug.Log("[Lobby] SharedLobbyState found — transitioning to Race.");
                    TransitionToRace();
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }
            UnityEngine.Debug.LogWarning("[Lobby] SharedLobbyState never appeared — cannot transition.");
        }

        private void OnRetrySession()
        {
            _sessionManager.RetryHostSession();
        }

        private void OnBackToStartScreen()
        {
            _sessionManager.Shutdown();
            _lanDiscovery.StopAll();
            _lobbyStatePrefabRegistered = false;
            ShowStartScreen();
        }

        private void ShowSessionUI()
        {
            if (_startScreen != null) _startScreen.gameObject.SetActive(false);
            if (_joinUI != null) _joinUI.gameObject.SetActive(false);
            if (_sharedLobbyUI != null) _sharedLobbyUI.gameObject.SetActive(false);
            if (_sessionUI != null) _sessionUI.gameObject.SetActive(true);
        }

        private void ShowJoinUI()
        {
            if (_startScreen != null) _startScreen.gameObject.SetActive(false);
            if (_sessionUI != null) _sessionUI.gameObject.SetActive(false);
            if (_sharedLobbyUI != null) _sharedLobbyUI.gameObject.SetActive(false);
            if (_joinUI != null)
            {
                _joinUI.gameObject.SetActive(true);
                _joinUI.ResetToInput();
            }
            _lanDiscovery.StartListening();
        }

        private void ShowSharedLobbyUI()
        {
            if (_startScreen != null) _startScreen.gameObject.SetActive(false);
            if (_sessionUI != null) _sessionUI.gameObject.SetActive(false);
            if (_joinUI != null) _joinUI.gameObject.SetActive(false);
            if (_sharedLobbyUI != null) _sharedLobbyUI.gameObject.SetActive(true);
        }

        private void ShowStartScreen()
        {
            if (_sessionUI != null) _sessionUI.gameObject.SetActive(false);
            if (_joinUI != null) _joinUI.gameObject.SetActive(false);
            if (_sharedLobbyUI != null) _sharedLobbyUI.gameObject.SetActive(false);
            if (_startScreen != null) _startScreen.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when all players are ready and the race should begin.
        /// </summary>
        public void TransitionToRace()
        {
            RequestCameraPermissionAndTransition();
        }

        private void RequestCameraPermissionAndTransition()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                LoadRace();
                return;
            }

            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += _ => LoadRace();
            callbacks.PermissionDenied += _ =>
                UnityEngine.Debug.LogWarning("[Lobby] Camera permission denied. Staying in Lobby.");
            callbacks.PermissionDeniedAndDontAskAgain += _ =>
                UnityEngine.Debug.LogWarning("[Lobby] Camera permission denied with 'Don't ask again'. Staying in Lobby.");

            UnityEngine.Android.Permission.RequestUserPermission(
                UnityEngine.Android.Permission.Camera,
                callbacks);
#else
            LoadRace();
#endif
        }

        private static void LoadRace()
        {
            SceneManager.LoadScene("Race");
        }
    }
}
