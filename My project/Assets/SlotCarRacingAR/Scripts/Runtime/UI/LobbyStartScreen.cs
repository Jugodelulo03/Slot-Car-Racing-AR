using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Lobby start screen with two dominant actions: create or join a match.
    /// </summary>
    public sealed class LobbyStartScreen : MonoBehaviour
    {
        [SerializeField] private string _printableMarkerUrl = "https://drive.google.com/file/d/1LyVEcWAhbsZlZi74Q2JRocgWYT5pCMOm/view?usp=sharing";

        private Button _createMatchButton;
        private Button _joinMatchButton;
        private Button _markerLinkButton;

        public event System.Action OnCreateMatchClicked;
        public event System.Action OnJoinMatchClicked;

        private void Awake()
        {
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_createMatchButton != null)
            {
                _createMatchButton.onClick.RemoveAllListeners();
            }

            if (_joinMatchButton != null)
            {
                _joinMatchButton.onClick.RemoveAllListeners();
            }

            if (_markerLinkButton != null)
            {
                _markerLinkButton.onClick.RemoveAllListeners();
            }
        }

        private void BuildUI()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

            RetroUi.CreateFullScreenBackground(root, "RetroBackground", true);

            Image logo = RetroUi.CreateLogo(
                root,
                "Face2RaceLogo",
                new Vector2(0.30f, 0.51f),
                new Vector2(0.70f, 0.91f));
            RetroUiAnimator.Attach(logo.gameObject)?.PlayPop(1.04f, 0.28f);

            _createMatchButton = RetroUi.CreateButton(
                root,
                "CreateMatchButton",
                "Crear Partida",
                new Vector2(0.27f, 0.34f),
                new Vector2(0.73f, 0.47f),
                RetroUi.Red,
                RetroUi.White,
                42);
            _createMatchButton.onClick.AddListener(HandleCreateMatch);
            RetroUiAnimator.Attach(_createMatchButton.gameObject)?.PlaySlideIn(new Vector2(0f, -44f), 0.25f);
            RetroUiAnimator.Attach(_createMatchButton.gameObject)?.PlayFadeIn(0.18f);

            _joinMatchButton = RetroUi.CreateButton(
                root,
                "JoinMatchButton",
                "Unirse a la Partida",
                new Vector2(0.27f, 0.18f),
                new Vector2(0.73f, 0.31f),
                RetroUi.Teal,
                RetroUi.White,
                38);
            _joinMatchButton.onClick.AddListener(HandleJoinMatch);
            RetroUiAnimator.Attach(_joinMatchButton.gameObject)?.PlaySlideIn(new Vector2(0f, -44f), 0.28f);
            RetroUiAnimator.Attach(_joinMatchButton.gameObject)?.PlayFadeIn(0.20f);

            RectTransform quickGuide = RetroUi.CreatePanel(
                root,
                "QuickStartGuide",
                new Vector2(0.15f, 0.045f),
                new Vector2(0.85f, 0.125f),
                RetroUi.WithAlpha(RetroUi.TealDark, 0.92f),
                false);

            Text quickStartText = RetroUi.CreateText(
                quickGuide,
                "QuickStartText",
                "1 HOST CREA PARTIDA   |   2 INVITADO SE UNE   |   3 APUNTAN AL MARCADOR   |   4 GO!",
                Vector2.zero,
                Vector2.one,
                22,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            quickStartText.resizeTextForBestFit = true;
            quickStartText.resizeTextMinSize = 14;
            quickStartText.resizeTextMaxSize = 22;
            RetroUiAnimator.Attach(quickGuide.gameObject)?.PlaySlideIn(new Vector2(0f, -32f), 0.30f);
            RetroUiAnimator.Attach(quickGuide.gameObject)?.PlayFadeIn(0.18f);

            CreatePrintableMarkerButton(root);
        }

        private void HandleCreateMatch()
        {
            UnityEngine.Debug.Log("[LobbyStartScreen] Create Match selected.");
            OnCreateMatchClicked?.Invoke();
        }

        private void HandleJoinMatch()
        {
            UnityEngine.Debug.Log("[LobbyStartScreen] Join Match selected.");
            OnJoinMatchClicked?.Invoke();
        }

        private void CreatePrintableMarkerButton(RectTransform root)
        {
            RectTransform callout = RetroUi.CreatePanel(
                root,
                "PrintableMarkerCallout",
                new Vector2(0.745f, 0.185f),
                new Vector2(0.965f, 0.255f),
                RetroUi.WithAlpha(RetroUi.TealDark, 0.92f),
                false,
                true,
                true);

            Text calloutText = RetroUi.CreateText(
                callout,
                "PrintableMarkerCalloutText",
                "DESCARGA EL MARCADOR AQUI",
                Vector2.zero,
                Vector2.one,
                20,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            calloutText.resizeTextForBestFit = true;
            calloutText.resizeTextMinSize = 13;
            calloutText.resizeTextMaxSize = 20;

            Text arrow = RetroUi.CreateText(
                root,
                "PrintableMarkerArrow",
                "↓",
                new Vector2(0.91f, 0.135f),
                new Vector2(0.955f, 0.205f),
                46,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);
            arrow.resizeTextForBestFit = true;
            arrow.resizeTextMinSize = 24;
            arrow.resizeTextMaxSize = 46;

            RectTransform buttonRect = RetroUi.CreatePanel(
                root,
                "PrintableMarkerButton",
                new Vector2(0.895f, 0.035f),
                new Vector2(0.965f, 0.16f),
                RetroUi.Yellow,
                true);

            Image buttonImage = buttonRect.GetComponent<Image>();
            RetroUi.StyleImageAsCircle(buttonImage, RetroUi.Yellow);

            _markerLinkButton = buttonRect.gameObject.AddComponent<Button>();
            _markerLinkButton.targetGraphic = buttonImage;
            ColorBlock colors = _markerLinkButton.colors;
            colors.normalColor = RetroUi.Yellow;
            colors.highlightedColor = Color.Lerp(RetroUi.Yellow, RetroUi.White, 0.14f);
            colors.pressedColor = Color.Lerp(RetroUi.Yellow, RetroUi.Black, 0.18f);
            colors.selectedColor = RetroUi.Yellow;
            colors.disabledColor = RetroUi.WithAlpha(RetroUi.Yellow, 0.45f);
            _markerLinkButton.colors = colors;
            buttonRect.gameObject.AddComponent<RetroButtonPress>();
            _markerLinkButton.onClick.AddListener(HandlePrintableMarkerClicked);

            Text label = RetroUi.CreateText(
                buttonRect,
                "PrintableMarkerLabel",
                "PDF",
                Vector2.zero,
                Vector2.one,
                34,
                RetroUi.TealDark,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                false);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 20;
            label.resizeTextMaxSize = 34;

            RetroUiAnimator.Attach(callout.gameObject)?.PlaySlideIn(new Vector2(34f, 0f), 0.28f);
            RetroUiAnimator.Attach(callout.gameObject)?.PlayFadeIn(0.16f);
            RetroUiAnimator.Attach(arrow.gameObject)?.PlayPop(1.12f, 0.22f);
            RetroUiAnimator.Attach(buttonRect.gameObject)?.PlayPop(1.08f, 0.24f);
        }

        private void HandlePrintableMarkerClicked()
        {
            if (string.IsNullOrWhiteSpace(_printableMarkerUrl))
            {
                UnityEngine.Debug.LogWarning("[LobbyStartScreen] Printable marker URL is empty.");
                return;
            }

            UnityEngine.Debug.Log($"[LobbyStartScreen] Opening printable marker URL: {_printableMarkerUrl}");
            Application.OpenURL(_printableMarkerUrl);
        }
    }
}
