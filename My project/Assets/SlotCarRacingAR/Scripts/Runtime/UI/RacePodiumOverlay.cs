using System;
using SlotCarRacingAR.Runtime.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace SlotCarRacingAR.Runtime.UI
{
    public sealed class RacePodiumOverlay : MonoBehaviour
    {
        private enum PrimaryAction
        {
            None,
            RequestRematch,
            AcceptRematch
        }

        private enum SecondaryAction
        {
            MainMenu,
            ReturnToLobby
        }

        private Canvas _canvas;
        private GameObject _root;
        private Text _titleText;
        private Text _firstText;
        private Text _secondText;
        private Text _statusText;
        private Button _primaryButton;
        private Button _secondaryButton;
        private Text _primaryButtonText;
        private Text _secondaryButtonText;
        private PrimaryAction _primaryAction;
        private SecondaryAction _secondaryAction;

        public bool IsVisible => _root != null && _root.activeSelf;

        public event Action OnRematchClicked;
        public event Action OnAcceptRematchClicked;
        public event Action OnReturnToLobbyClicked;
        public event Action OnMainMenuClicked;

        private void Awake()
        {
            BuildUi();
            Hide();
        }

        public void Show(SharedLobbyState sharedState)
        {
            if (_root == null)
            {
                BuildUi();
            }

            _root.SetActive(true);
            Refresh(sharedState);
        }

        public void Refresh(SharedLobbyState sharedState)
        {
            if (_root == null)
            {
                return;
            }

            RefreshResults(sharedState);
            RefreshRematchActions(sharedState);
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void RefreshResults(SharedLobbyState sharedState)
        {
            if (sharedState == null)
            {
                _titleText.text = "PODIO";
                _firstText.text = "1ro  PLAYER 1  --:--";
                _secondText.text = "2do  PLAYER 2  --:--";
                return;
            }

            byte winner = sharedState.WinnerPlayerId.Value;
            float hostTime = sharedState.HostFinishTimeSeconds.Value;
            float guestTime = sharedState.GuestFinishTimeSeconds.Value;
            bool hasHostTime = hostTime >= 0f;
            bool hasGuestTime = guestTime >= 0f;

            byte firstPlayer = winner != 0
                ? winner
                : hasHostTime && hasGuestTime && guestTime < hostTime ? (byte)2 : (byte)1;
            byte secondPlayer = firstPlayer == 1 ? (byte)2 : (byte)1;

            _titleText.text = firstPlayer == 1 ? "GANA PLAYER 1" : "GANA PLAYER 2";
            _firstText.text = FormatRow(1, firstPlayer, firstPlayer == 1 ? hostTime : guestTime);
            _secondText.text = FormatRow(2, secondPlayer, secondPlayer == 1 ? hostTime : guestTime);
        }

        private void RefreshRematchActions(SharedLobbyState sharedState)
        {
            bool isHost = sharedState != null && sharedState.IsServer;
            bool requested = sharedState != null && sharedState.RematchRequested.Value;
            bool guestAccepted = sharedState != null && sharedState.GuestRematchAccepted.Value;

            if (isHost)
            {
                _secondaryAction = SecondaryAction.MainMenu;
                SetButton(_secondaryButton, _secondaryButtonText, "MENU PRINCIPAL", true);

                if (requested)
                {
                    _primaryAction = PrimaryAction.None;
                    _statusText.text = guestAccepted ? "REVANCHA LISTA" : "ESPERANDO AL RIVAL";
                    _statusText.color = guestAccepted ? RetroUi.Green : RetroUi.Yellow;
                    SetButton(_primaryButton, _primaryButtonText, guestAccepted ? "VOLVIENDO" : "ESPERANDO", false);
                }
                else
                {
                    _primaryAction = PrimaryAction.RequestRematch;
                    _statusText.text = "SOLO EL HOST PUEDE PEDIR REVANCHA";
                    _statusText.color = RetroUi.White;
                    SetButton(_primaryButton, _primaryButtonText, "REVANCHA", true);
                }

                return;
            }

            if (requested)
            {
                _primaryAction = PrimaryAction.AcceptRematch;
                _secondaryAction = SecondaryAction.ReturnToLobby;
                _statusText.text = guestAccepted ? "REVANCHA ACEPTADA" : "EL HOST PIDE REVANCHA";
                _statusText.color = guestAccepted ? RetroUi.Green : RetroUi.Yellow;
                SetButton(_primaryButton, _primaryButtonText, guestAccepted ? "ACEPTADO" : "ACEPTAR REVANCHA", !guestAccepted);
                SetButton(_secondaryButton, _secondaryButtonText, "SALIR A SALA", true);
            }
            else
            {
                _primaryAction = PrimaryAction.None;
                _secondaryAction = SecondaryAction.MainMenu;
                _statusText.text = "ESPERANDO AL HOST";
                _statusText.color = RetroUi.Yellow;
                SetButton(_primaryButton, _primaryButtonText, "ESPERANDO HOST", false);
                SetButton(_secondaryButton, _secondaryButtonText, "MENU PRINCIPAL", true);
            }
        }

        private void BuildUi()
        {
            if (_canvas != null)
            {
                return;
            }

            GameObject canvasObj = new GameObject("RacePodiumCanvas");
            canvasObj.transform.SetParent(transform, false);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 120;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            _root = new GameObject("RacePodiumRoot");
            _root.transform.SetParent(canvasObj.transform, false);
            RectTransform rootRect = _root.AddComponent<RectTransform>();
            RetroUi.Fill(rootRect);

            Image shade = _root.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.48f);
            shade.raycastTarget = true;

            RectTransform panel = RetroUi.CreatePanel(
                _root.transform,
                "PodiumPanel",
                new Vector2(0.24f, 0.14f),
                new Vector2(0.76f, 0.90f),
                RetroUi.Cream,
                true);

            _titleText = RetroUi.CreateText(
                panel,
                "Title",
                "PODIO",
                new Vector2(0.06f, 0.78f),
                new Vector2(0.94f, 0.95f),
                48,
                RetroUi.Red,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _firstText = RetroUi.CreateText(
                panel,
                "FirstPlace",
                "1ro  PLAYER 1  --:--",
                new Vector2(0.10f, 0.56f),
                new Vector2(0.90f, 0.72f),
                36,
                RetroUi.Black,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                false);

            _secondText = RetroUi.CreateText(
                panel,
                "SecondPlace",
                "2do  PLAYER 2  --:--",
                new Vector2(0.10f, 0.40f),
                new Vector2(0.90f, 0.54f),
                30,
                RetroUi.TealDark,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic,
                false);

            _statusText = RetroUi.CreateText(
                panel,
                "RematchStatus",
                "ESPERANDO AL HOST",
                new Vector2(0.08f, 0.30f),
                new Vector2(0.92f, 0.39f),
                24,
                RetroUi.Yellow,
                TextAnchor.MiddleCenter,
                FontStyle.BoldAndItalic);

            _primaryButton = RetroUi.CreateButton(
                panel,
                "PrimaryButton",
                "REVANCHA",
                new Vector2(0.08f, 0.10f),
                new Vector2(0.46f, 0.26f),
                RetroUi.Green,
                RetroUi.White,
                26);
            _primaryButtonText = _primaryButton.GetComponentInChildren<Text>();
            _primaryButton.onClick.AddListener(HandlePrimaryClicked);

            _secondaryButton = RetroUi.CreateButton(
                panel,
                "SecondaryButton",
                "MENU PRINCIPAL",
                new Vector2(0.54f, 0.10f),
                new Vector2(0.92f, 0.26f),
                RetroUi.Red,
                RetroUi.White,
                25);
            _secondaryButtonText = _secondaryButton.GetComponentInChildren<Text>();
            _secondaryButton.onClick.AddListener(HandleSecondaryClicked);
        }

        private void HandlePrimaryClicked()
        {
            switch (_primaryAction)
            {
                case PrimaryAction.RequestRematch:
                    OnRematchClicked?.Invoke();
                    break;
                case PrimaryAction.AcceptRematch:
                    OnAcceptRematchClicked?.Invoke();
                    break;
            }
        }

        private void HandleSecondaryClicked()
        {
            if (_secondaryAction == SecondaryAction.ReturnToLobby)
            {
                OnReturnToLobbyClicked?.Invoke();
            }
            else
            {
                OnMainMenuClicked?.Invoke();
            }
        }

        private static void SetButton(Button button, Text text, string label, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }

            if (text != null)
            {
                text.text = label.ToUpperInvariant();
            }
        }

        private static string FormatRow(int rank, byte playerId, float seconds)
        {
            return FormatOrdinal(rank) + "  PLAYER " + playerId + "  " + FormatTime(seconds);
        }

        private static string FormatOrdinal(int position)
        {
            switch (position)
            {
                case 1:
                    return "1ro";
                case 2:
                    return "2do";
                case 3:
                    return "3ro";
                case 4:
                    return "4to";
                default:
                    return position.ToString() + "to";
            }
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f)
            {
                return "--:--";
            }

            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainder = seconds - minutes * 60f;
            return minutes.ToString("00") + ":" + remainder.ToString("00.00");
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
            }
        }
    }
}
