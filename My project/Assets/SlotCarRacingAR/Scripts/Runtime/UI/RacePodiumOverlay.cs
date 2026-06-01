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
        private readonly Image[] _resultRowBackgrounds = new Image[SharedLobbyState.MaxPlayers];
        private readonly Text[] _resultRows = new Text[SharedLobbyState.MaxPlayers];
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
                for (int i = 0; i < _resultRows.Length; i++)
                {
                    SetResultRow(i, i < 2, FormatRow(i + 1, (byte)(i + 1), -1f), GetRankTextColor(i + 1));
                }

                return;
            }

            byte[] players = new byte[SharedLobbyState.MaxPlayers];
            int playerCount = 0;
            for (byte playerId = 1; playerId <= SharedLobbyState.MaxPlayers; playerId++)
            {
                if (sharedState.HasPlayer(playerId))
                {
                    players[playerCount] = playerId;
                    playerCount++;
                }
            }

            if (playerCount == 0)
            {
                players[0] = 1;
                playerCount = 1;
            }

            SortPlayersByResult(sharedState, players, playerCount);

            byte winner = sharedState.WinnerPlayerId.Value != 0 ? sharedState.WinnerPlayerId.Value : players[0];
            _titleText.text = "GANA PLAYER " + winner;

            for (int rowIndex = 0; rowIndex < _resultRows.Length; rowIndex++)
            {
                bool active = rowIndex < playerCount;
                byte playerId = active ? players[rowIndex] : (byte)(rowIndex + 1);
                SetResultRow(
                    rowIndex,
                    active,
                    FormatRow(rowIndex + 1, playerId, sharedState.GetFinishTime(playerId)),
                    GetRankTextColor(rowIndex + 1));
            }
        }

        private void RefreshRematchActions(SharedLobbyState sharedState)
        {
            bool isHost = sharedState != null && sharedState.IsServer;
            bool requested = sharedState != null && sharedState.RematchRequested.Value;
            byte localPlayerId = sharedState != null ? sharedState.LocalPlayerId : (byte)0;
            if (sharedState != null && localPlayerId == 0)
            {
                localPlayerId = sharedState.IsServer ? (byte)1 : (byte)2;
            }

            bool localAccepted = sharedState != null && sharedState.GetRematchAccepted(localPlayerId);
            bool allAccepted = sharedState != null && AreAllActivePlayersAccepted(sharedState);

            if (isHost)
            {
                _secondaryAction = SecondaryAction.MainMenu;
                SetButton(_secondaryButton, _secondaryButtonText, "MENU PRINCIPAL", true);

                if (requested)
                {
                    _primaryAction = PrimaryAction.None;
                    _statusText.text = allAccepted ? "REVANCHA LISTA" : "ESPERANDO RIVALES";
                    _statusText.color = RetroUi.TealDark;
                    SetButton(_primaryButton, _primaryButtonText, allAccepted ? "VOLVIENDO" : "ESPERANDO", false);
                }
                else
                {
                    _primaryAction = PrimaryAction.RequestRematch;
                    _statusText.text = "SOLO EL HOST PUEDE PEDIR REVANCHA";
                    _statusText.color = RetroUi.TealDark;
                    SetButton(_primaryButton, _primaryButtonText, "REVANCHA", true);
                }

                return;
            }

            if (requested)
            {
                _primaryAction = PrimaryAction.AcceptRematch;
                _secondaryAction = SecondaryAction.ReturnToLobby;
                _statusText.text = localAccepted ? "REVANCHA ACEPTADA" : "EL HOST PIDE REVANCHA";
                _statusText.color = RetroUi.TealDark;
                SetButton(_primaryButton, _primaryButtonText, localAccepted ? "ACEPTADO" : "ACEPTAR REVANCHA", !localAccepted);
                SetButton(_secondaryButton, _secondaryButtonText, "SALIR A SALA", true);
            }
            else
            {
                _primaryAction = PrimaryAction.None;
                _secondaryAction = SecondaryAction.MainMenu;
                _statusText.text = "ESPERANDO AL HOST";
                _statusText.color = RetroUi.TealDark;
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

            for (int rowIndex = 0; rowIndex < _resultRows.Length; rowIndex++)
            {
                float top = 0.77f - rowIndex * 0.105f;
                float bottom = top - 0.095f;
                RectTransform rowPanel = RetroUi.CreatePanel(
                    panel,
                    "ResultRowPanel" + (rowIndex + 1),
                    new Vector2(0.11f, bottom + 0.005f),
                    new Vector2(0.89f, top - 0.005f),
                    RetroUi.TealDark,
                    false,
                    false,
                    true);
                _resultRowBackgrounds[rowIndex] = rowPanel.GetComponent<Image>();

                _resultRows[rowIndex] = RetroUi.CreateText(
                    rowPanel,
                    "ResultRow" + (rowIndex + 1),
                    FormatRow(rowIndex + 1, (byte)(rowIndex + 1), -1f),
                    Vector2.zero,
                    Vector2.one,
                    rowIndex == 0 ? 35 : 28,
                    GetRankTextColor(rowIndex + 1),
                    TextAnchor.MiddleCenter,
                    FontStyle.BoldAndItalic);
                _resultRows[rowIndex].resizeTextForBestFit = true;
                _resultRows[rowIndex].resizeTextMinSize = 18;
                _resultRows[rowIndex].resizeTextMaxSize = rowIndex == 0 ? 35 : 28;
            }

            _statusText = RetroUi.CreateText(
                panel,
                "RematchStatus",
                "ESPERANDO AL HOST",
                new Vector2(0.08f, 0.27f),
                new Vector2(0.92f, 0.35f),
                24,
                RetroUi.TealDark,
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

        private void SetResultRow(int rowIndex, bool active, string text, Color color)
        {
            if (rowIndex < 0 || rowIndex >= _resultRows.Length || _resultRows[rowIndex] == null)
            {
                return;
            }

            _resultRows[rowIndex].gameObject.SetActive(active);
            _resultRows[rowIndex].text = text;
            _resultRows[rowIndex].color = color;
            if (_resultRowBackgrounds[rowIndex] != null)
            {
                _resultRowBackgrounds[rowIndex].gameObject.SetActive(active);
                _resultRowBackgrounds[rowIndex].color = RetroUi.TealDark;
            }
        }

        private static void SortPlayersByResult(SharedLobbyState sharedState, byte[] players, int count)
        {
            for (int i = 0; i < count - 1; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (ComesAfter(sharedState, players[i], players[j]))
                    {
                        byte temp = players[i];
                        players[i] = players[j];
                        players[j] = temp;
                    }
                }
            }
        }

        private static bool ComesAfter(SharedLobbyState sharedState, byte firstPlayer, byte secondPlayer)
        {
            float firstTime = sharedState.GetFinishTime(firstPlayer);
            float secondTime = sharedState.GetFinishTime(secondPlayer);
            if (firstTime >= 0f && secondTime >= 0f)
            {
                return firstTime > secondTime;
            }

            if (firstTime >= 0f)
            {
                return false;
            }

            if (secondTime >= 0f)
            {
                return true;
            }

            float firstProgress = sharedState.GetLap(firstPlayer) + sharedState.GetProgress(firstPlayer);
            float secondProgress = sharedState.GetLap(secondPlayer) + sharedState.GetProgress(secondPlayer);
            return firstProgress < secondProgress;
        }

        private static bool AreAllActivePlayersAccepted(SharedLobbyState sharedState)
        {
            for (byte playerId = 1; playerId <= SharedLobbyState.MaxPlayers; playerId++)
            {
                if (sharedState.HasPlayer(playerId) && !sharedState.GetRematchAccepted(playerId))
                {
                    return false;
                }
            }

            return true;
        }

        private static Color GetRankTextColor(int rank)
        {
            switch (rank)
            {
                case 1:
                    return new Color(1.00f, 0.73f, 0.10f);
                case 2:
                    return new Color(0.78f, 0.82f, 0.86f);
                case 3:
                    return new Color(0.95f, 0.52f, 0.18f);
                default:
                    return RetroUi.Cream;
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
