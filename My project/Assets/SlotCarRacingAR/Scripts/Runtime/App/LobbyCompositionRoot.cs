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
        private LobbyStartScreen _startScreen;
        private SessionManager _sessionManager;
        private LobbySessionUI _sessionUI;
        private LobbyJoinUI _joinUI;
        private SharedLobbyUI _sharedLobbyUI;
        private SharedLobbyState _sharedLobbyState;
        private GameObject _lobbyStatePrefab;

        private void Awake()
        {
            EnsureInputSystemUiModule();
            CreateSessionManager();
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

            // Prepare SharedLobbyState prefab for network spawning
            _lobbyStatePrefab = new GameObject("SharedLobbyStatePrefab");
            _lobbyStatePrefab.AddComponent<NetworkObject>();
            _lobbyStatePrefab.AddComponent<SharedLobbyState>();
            _lobbyStatePrefab.SetActive(false);
            NetworkManager nm = NetworkManager.Singleton;
            if (nm != null)
            {
                nm.AddNetworkPrefab(_lobbyStatePrefab);
            }
        }

        private void CreateSessionManager()
        {
            GameObject managerObj = new GameObject("SessionManager");
            managerObj.transform.SetParent(transform, false);
            _sessionManager = managerObj.AddComponent<SessionManager>();
            _sessionManager.OnSessionStateChanged += OnSessionStateChanged;
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

        private void OnGuestConnect(string hostIp)
        {
            UnityEngine.Debug.Log("[Lobby] Guest connecting to " + hostIp);
            _sessionManager.StartGuestSession(hostIp);
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
                _sessionUI.UpdateState(state, _sessionManager.GetLocalIPAddress(), _sessionManager.FailureReason);
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
            ShowSharedLobbyUI();

            // Host spawns the SharedLobbyState networked object
            if (_sessionManager.Role == PlayerRole.Host)
            {
                SpawnSharedLobbyState();
            }
            else
            {
                // Guest: find the spawned SharedLobbyState after a short delay
                Invoke(nameof(FindSharedLobbyState), 0.5f);
            }
        }

        private void SpawnSharedLobbyState()
        {
            if (_lobbyStatePrefab == null) return;

            GameObject instance = Instantiate(_lobbyStatePrefab);
            instance.SetActive(true);
            instance.GetComponent<NetworkObject>().Spawn();

            _sharedLobbyState = instance.GetComponent<SharedLobbyState>();
            _sharedLobbyState.OnPlayerCountChanged += OnLobbyPlayerCountChanged;

            // Initial UI update
            _sharedLobbyUI.UpdatePlayerCount(_sharedLobbyState.PlayerCount.Value, _sessionManager.Role);
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
                _sharedLobbyUI.UpdatePlayerCount(_sharedLobbyState.PlayerCount.Value, _sessionManager.Role);
                if (_sharedLobbyState.PlayerCount.Value >= 2)
                {
                    _sharedLobbyUI.ShowConnectionConfirmation();
                }
            }
            else
            {
                // Retry once more
                Invoke(nameof(FindSharedLobbyState), 1.0f);
            }
        }

        private void OnLobbyPlayerCountChanged(byte oldCount, byte newCount)
        {
            _sharedLobbyUI.UpdatePlayerCount(newCount, _sessionManager.Role);

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
            TransitionToRace();
        }

        private void OnRetrySession()
        {
            _sessionManager.RetryHostSession();
        }

        private void OnBackToStartScreen()
        {
            _sessionManager.Shutdown();
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
