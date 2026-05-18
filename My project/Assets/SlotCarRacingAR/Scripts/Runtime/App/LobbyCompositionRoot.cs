using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
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

        private void Awake()
        {
            EnsureInputSystemUiModule();
            CreateStartScreen();
        }

        private void OnDestroy()
        {
            if (_startScreen != null)
            {
                _startScreen.OnCreateMatchClicked -= OnCreateMatchSelected;
                _startScreen.OnJoinMatchClicked -= OnJoinMatchSelected;
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
        /// Placeholder: transitions directly to Race. Story 1.3 will add real session creation.
        /// </summary>
        public void OnCreateMatchSelected()
        {
            UnityEngine.Debug.Log("[Lobby] Create Match selected — transitioning to Race (placeholder).");
            TransitionToRace();
        }

        /// <summary>
        /// Called when the player selects "Join Match" on the start screen.
        /// Placeholder: transitions directly to Race. Story 1.4 will add real session join.
        /// </summary>
        public void OnJoinMatchSelected()
        {
            UnityEngine.Debug.Log("[Lobby] Join Match selected — transitioning to Race (placeholder).");
            TransitionToRace();
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
