using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    /// <summary>
    /// Lobby start screen with two dominant actions: create or join a match.
    /// </summary>
    public sealed class LobbyStartScreen : MonoBehaviour
    {
        private Button _createMatchButton;
        private Button _joinMatchButton;

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
        }

        private void BuildUI()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

            RetroUi.CreateFullScreenBackground(root, "RetroBackground", true);

            RetroUi.CreateLogo(
                root,
                "Face2RaceLogo",
                new Vector2(0.25f, 0.56f),
                new Vector2(0.75f, 0.91f));

            RetroUi.CreateText(
                root,
                "Tagline",
                "Dos jugadores. Una mesa. Una pista.",
                new Vector2(0.22f, 0.47f),
                new Vector2(0.78f, 0.56f),
                34,
                RetroUi.White,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _createMatchButton = RetroUi.CreateButton(
                root,
                "CreateMatchButton",
                "Crear Partida",
                new Vector2(0.28f, 0.27f),
                new Vector2(0.72f, 0.40f),
                RetroUi.Red,
                RetroUi.White,
                42);
            _createMatchButton.onClick.AddListener(HandleCreateMatch);

            _joinMatchButton = RetroUi.CreateButton(
                root,
                "JoinMatchButton",
                "Unirse a la Partida",
                new Vector2(0.28f, 0.12f),
                new Vector2(0.72f, 0.25f),
                RetroUi.Teal,
                RetroUi.White,
                38);
            _joinMatchButton.onClick.AddListener(HandleJoinMatch);

            RectTransform quickGuide = RetroUi.CreatePanel(
                root,
                "QuickStartGuide",
                new Vector2(0.12f, 0.02f),
                new Vector2(0.88f, 0.10f),
                RetroUi.WithAlpha(RetroUi.TealDark, 0.92f),
                false);

            Text quickStartText = RetroUi.CreateText(
                quickGuide,
                "QuickStartText",
                "1 HOST CREA PARTIDA   |   2 INVITADO SE UNE EN LA MISMA RED   |   3 AMBOS APUNTAN AL MARCADOR",
                Vector2.zero,
                Vector2.one,
                22,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);
            quickStartText.resizeTextForBestFit = true;
            quickStartText.resizeTextMinSize = 14;
            quickStartText.resizeTextMaxSize = 22;
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
    }
}
