using System;
using Unity.Netcode;
using UnityEngine;

namespace SlotCarRacingAR.Runtime.Infrastructure
{
    public enum RacePhase : byte
    {
        Setup = 0,
        Countdown = 1,
        Racing = 2,
        Finished = 3
    }

    /// <summary>
    /// Networked lobby/race state. The host owns simulation and publishes state for up to four players.
    /// </summary>
    public sealed class SharedLobbyState : NetworkBehaviour
    {
        public const byte RaceLapTarget = 3;
        public const byte MaxPlayers = 4;
        private const ulong EmptyClientId = ulong.MaxValue;

        public NetworkVariable<byte> PlayerCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<ulong> Player1ClientId = new(EmptyClientId, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<ulong> Player2ClientId = new(EmptyClientId, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<ulong> Player3ClientId = new(EmptyClientId, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<ulong> Player4ClientId = new(EmptyClientId, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> HostReady = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> GuestReady = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Player3Ready = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Player4Ready = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<RacePhase> Phase = new(RacePhase.Setup, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<byte> CountdownValue = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> HostAccelerationHeld = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> GuestAccelerationHeld = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Player3AccelerationHeld = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Player4AccelerationHeld = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<float> HostProgress = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> GuestProgress = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Player3Progress = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Player4Progress = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<float> HostSpeed = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> GuestSpeed = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Player3Speed = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Player4Speed = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<byte> HostLap = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<byte> GuestLap = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<byte> Player3Lap = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<byte> Player4Lap = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> HostPenaltyActive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> GuestPenaltyActive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Player3PenaltyActive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Player4PenaltyActive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<byte> WinnerPlayerId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> HostFinishTimeSeconds = new(-1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> GuestFinishTimeSeconds = new(-1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Player3FinishTimeSeconds = new(-1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> Player4FinishTimeSeconds = new(-1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> RematchRequested = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> HostRematchAccepted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> GuestRematchAccepted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Player3RematchAccepted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Player4RematchAccepted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<byte> RematchLobbySignal = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public event Action<byte, byte> OnPlayerCountChanged;
        public event Action<bool, bool> OnReadyStateChanged;
        public event Action<RacePhase> OnPhaseChanged;
        public event Action<byte> OnCountdownTick;
        public event Action OnRaceStateChanged;
        public event Action<byte> OnWinnerChanged;

        public byte LocalPlayerId
        {
            get
            {
                NetworkManager networkManager = NetworkManager.Singleton;
                return networkManager == null ? (byte)0 : GetPlayerIdForClient(networkManager.LocalClientId);
            }
        }

        public bool AllReady
        {
            get
            {
                for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
                {
                    if (HasPlayer(playerId) && !GetReady(playerId))
                    {
                        return false;
                    }
                }

                return PlayerCount.Value >= 2;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);

            if (IsServer)
            {
                AssignConnectedClients();
                NetworkManager.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }

            SubscribeNetworkVariables();
            UnityEngine.Debug.Log("[SharedLobbyState] Spawned. PlayerCount=" + PlayerCount.Value + " LocalPlayer=" + LocalPlayerId + " IsServer=" + IsServer);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            UnsubscribeNetworkVariables();
        }

        public void SetLocalReady(bool ready)
        {
            if (IsServer)
            {
                SetReady(LocalPlayerId == 0 ? (byte)1 : LocalPlayerId, ready);
            }
            else
            {
                SetReadyServerRpc(ready);
            }
        }

        public void RevokeAllReadiness()
        {
            if (!IsServer) return;
            for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
            {
                SetReady(playerId, false);
            }
        }

        public void BeginCountdown()
        {
            if (!IsServer) return;
            Phase.Value = RacePhase.Countdown;
            CountdownValue.Value = 3;
        }

        public void TickCountdown(byte value)
        {
            if (!IsServer) return;
            CountdownValue.Value = value;
        }

        public void BeginRacing()
        {
            if (!IsServer) return;
            ResetRaceState();
            Phase.Value = RacePhase.Racing;
        }

        public void SetLocalAccelerationHeld(bool isHeld)
        {
            if (IsServer)
            {
                SetAccelerationHeld(LocalPlayerId == 0 ? (byte)1 : LocalPlayerId, isHeld);
            }
            else
            {
                SetAccelerationHeldServerRpc(isHeld);
            }
        }

        public void ResetRaceState()
        {
            if (!IsServer) return;

            for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
            {
                SetAccelerationHeld(playerId, false);
                SetProgress(playerId, 0f);
                SetSpeed(playerId, 0f);
                SetLap(playerId, 0);
                SetPenalty(playerId, false);
                SetFinishTime(playerId, -1f);
            }

            WinnerPlayerId.Value = 0;
        }

        public void PublishRaceState(byte playerId, float progress, float speed, byte lap, bool penaltyActive)
        {
            if (!IsServer || !IsValidPlayerId(playerId)) return;

            SetProgress(playerId, Mathf.Repeat(progress, 1f));
            SetSpeed(playerId, Mathf.Max(0f, speed));
            SetLap(playerId, lap);
            SetPenalty(playerId, penaltyActive);
        }

        public void PublishFinishTime(byte playerId, float finishTimeSeconds)
        {
            if (!IsServer || !IsValidPlayerId(playerId) || finishTimeSeconds < 0f)
            {
                return;
            }

            if (GetFinishTime(playerId) < 0f)
            {
                SetFinishTime(playerId, Mathf.Max(0f, finishTimeSeconds));
                SetAccelerationHeld(playerId, false);
            }
        }

        public void FinishRace(byte winnerPlayerId, float[] finishTimes)
        {
            if (!IsServer || winnerPlayerId == 0 || finishTimes == null) return;

            for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
            {
                int index = playerId - 1;
                if (index < finishTimes.Length)
                {
                    SetFinishTime(playerId, finishTimes[index] >= 0f ? Mathf.Max(0f, finishTimes[index]) : -1f);
                }

                SetAccelerationHeld(playerId, false);
            }

            WinnerPlayerId.Value = winnerPlayerId;
            Phase.Value = RacePhase.Finished;
        }

        public void FinishRace(byte winnerPlayerId, float hostFinishTimeSeconds, float guestFinishTimeSeconds)
        {
            FinishRace(winnerPlayerId, new[] { hostFinishTimeSeconds, guestFinishTimeSeconds, -1f, -1f });
        }

        public void PrepareForRematchLobby()
        {
            if (IsServer)
            {
                ResetForLobbyOnServer(true);
            }
            else
            {
                PrepareForRematchLobbyServerRpc();
            }
        }

        public void RequestRematch()
        {
            if (!IsServer)
            {
                return;
            }

            RematchRequested.Value = true;
            for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
            {
                SetRematchAccepted(playerId, HasPlayer(playerId) && playerId == 1);
            }
        }

        public void AcceptRematch()
        {
            if (IsServer)
            {
                SetRematchAccepted(LocalPlayerId == 0 ? (byte)1 : LocalPlayerId, true);
                TrySignalRematchLobby();
            }
            else
            {
                AcceptRematchServerRpc();
            }
        }

        public void ReturnToLobbyFromPodium()
        {
            if (IsServer)
            {
                SignalReturnToLobby();
            }
            else
            {
                ReturnToLobbyFromPodiumServerRpc();
            }
        }

        public bool HasPlayer(byte playerId)
        {
            return IsValidPlayerId(playerId) && GetClientId(playerId) != EmptyClientId;
        }

        public ulong GetClientId(byte playerId)
        {
            switch (playerId)
            {
                case 1:
                    return Player1ClientId.Value;
                case 2:
                    return Player2ClientId.Value;
                case 3:
                    return Player3ClientId.Value;
                case 4:
                    return Player4ClientId.Value;
                default:
                    return EmptyClientId;
            }
        }

        public byte GetPlayerIdForClient(ulong clientId)
        {
            for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
            {
                if (GetClientId(playerId) == clientId)
                {
                    return playerId;
                }
            }

            return 0;
        }

        public bool GetReady(byte playerId)
        {
            switch (playerId)
            {
                case 1:
                    return HostReady.Value;
                case 2:
                    return GuestReady.Value;
                case 3:
                    return Player3Ready.Value;
                case 4:
                    return Player4Ready.Value;
                default:
                    return false;
            }
        }

        public bool GetAccelerationHeld(byte playerId)
        {
            switch (playerId)
            {
                case 1:
                    return HostAccelerationHeld.Value;
                case 2:
                    return GuestAccelerationHeld.Value;
                case 3:
                    return Player3AccelerationHeld.Value;
                case 4:
                    return Player4AccelerationHeld.Value;
                default:
                    return false;
            }
        }

        public float GetProgress(byte playerId)
        {
            switch (playerId)
            {
                case 1:
                    return HostProgress.Value;
                case 2:
                    return GuestProgress.Value;
                case 3:
                    return Player3Progress.Value;
                case 4:
                    return Player4Progress.Value;
                default:
                    return 0f;
            }
        }

        public float GetSpeed(byte playerId)
        {
            switch (playerId)
            {
                case 1:
                    return HostSpeed.Value;
                case 2:
                    return GuestSpeed.Value;
                case 3:
                    return Player3Speed.Value;
                case 4:
                    return Player4Speed.Value;
                default:
                    return 0f;
            }
        }

        public byte GetLap(byte playerId)
        {
            switch (playerId)
            {
                case 1:
                    return HostLap.Value;
                case 2:
                    return GuestLap.Value;
                case 3:
                    return Player3Lap.Value;
                case 4:
                    return Player4Lap.Value;
                default:
                    return 0;
            }
        }

        public bool GetPenaltyActive(byte playerId)
        {
            switch (playerId)
            {
                case 1:
                    return HostPenaltyActive.Value;
                case 2:
                    return GuestPenaltyActive.Value;
                case 3:
                    return Player3PenaltyActive.Value;
                case 4:
                    return Player4PenaltyActive.Value;
                default:
                    return false;
            }
        }

        public float GetFinishTime(byte playerId)
        {
            switch (playerId)
            {
                case 1:
                    return HostFinishTimeSeconds.Value;
                case 2:
                    return GuestFinishTimeSeconds.Value;
                case 3:
                    return Player3FinishTimeSeconds.Value;
                case 4:
                    return Player4FinishTimeSeconds.Value;
                default:
                    return -1f;
            }
        }

        public bool GetRematchAccepted(byte playerId)
        {
            switch (playerId)
            {
                case 1:
                    return HostRematchAccepted.Value;
                case 2:
                    return GuestRematchAccepted.Value;
                case 3:
                    return Player3RematchAccepted.Value;
                case 4:
                    return Player4RematchAccepted.Value;
                default:
                    return false;
            }
        }

        public int GetFinishedCount()
        {
            int finished = 0;
            for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
            {
                if (HasPlayer(playerId) && GetFinishTime(playerId) >= 0f)
                {
                    finished++;
                }
            }

            return finished;
        }

        public int GetFinishRank(byte playerId)
        {
            float targetTime = GetFinishTime(playerId);
            if (targetTime < 0f)
            {
                return 0;
            }

            int rank = 1;
            for (byte otherId = 1; otherId <= MaxPlayers; otherId++)
            {
                if (otherId == playerId || !HasPlayer(otherId))
                {
                    continue;
                }

                float otherTime = GetFinishTime(otherId);
                if (otherTime >= 0f && otherTime < targetTime)
                {
                    rank++;
                }
            }

            return rank;
        }

        public int GetRacePosition(byte playerId)
        {
            if (!HasPlayer(playerId))
            {
                return 1;
            }

            float localFinish = GetFinishTime(playerId);
            int position = 1;
            float localRaceProgress = GetLap(playerId) + GetProgress(playerId);

            for (byte otherId = 1; otherId <= MaxPlayers; otherId++)
            {
                if (otherId == playerId || !HasPlayer(otherId))
                {
                    continue;
                }

                float otherFinish = GetFinishTime(otherId);
                if (localFinish >= 0f)
                {
                    if (otherFinish >= 0f && otherFinish < localFinish)
                    {
                        position++;
                    }

                    continue;
                }

                if (otherFinish >= 0f)
                {
                    position++;
                    continue;
                }

                float otherRaceProgress = GetLap(otherId) + GetProgress(otherId);
                if (otherRaceProgress > localRaceProgress)
                {
                    position++;
                }
            }

            return Mathf.Clamp(position, 1, MaxPlayers);
        }

        private void ResetForLobbyOnServer(bool clearRematchState)
        {
            RevokeAllReadiness();
            CountdownValue.Value = 0;
            ResetRaceState();
            Phase.Value = RacePhase.Setup;

            if (clearRematchState)
            {
                RematchRequested.Value = false;
                for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
                {
                    SetRematchAccepted(playerId, false);
                }
            }
        }

        private void TrySignalRematchLobby()
        {
            if (!IsServer || !RematchRequested.Value)
            {
                return;
            }

            for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
            {
                if (HasPlayer(playerId) && !GetRematchAccepted(playerId))
                {
                    return;
                }
            }

            SignalReturnToLobby();
        }

        private void SignalReturnToLobby()
        {
            ResetForLobbyOnServer(true);
            unchecked
            {
                RematchLobbySignal.Value++;
            }
        }

        private void AssignConnectedClients()
        {
            if (NetworkManager == null)
            {
                return;
            }

            ClearPlayerClientIds();
            AssignClientToSlot(NetworkManager.LocalClientId);
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                if (clientId == NetworkManager.LocalClientId)
                {
                    continue;
                }

                AssignClientToSlot(clientId);
            }

            RefreshPlayerCount();
        }

        private void AssignClientToSlot(ulong clientId)
        {
            if (GetPlayerIdForClient(clientId) != 0)
            {
                return;
            }

            for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
            {
                if (GetClientId(playerId) == EmptyClientId)
                {
                    SetClientId(playerId, clientId);
                    return;
                }
            }

            UnityEngine.Debug.LogWarning("[SharedLobbyState] Rejecting extra client beyond 4 players: " + clientId);
            if (NetworkManager != null)
            {
                NetworkManager.DisconnectClient(clientId);
            }
        }

        private void RemoveClientFromSlot(ulong clientId)
        {
            byte playerId = GetPlayerIdForClient(clientId);
            if (playerId == 0)
            {
                return;
            }

            SetClientId(playerId, EmptyClientId);
            SetReady(playerId, false);
            SetAccelerationHeld(playerId, false);
            SetProgress(playerId, 0f);
            SetSpeed(playerId, 0f);
            SetLap(playerId, 0);
            SetPenalty(playerId, false);
            SetFinishTime(playerId, -1f);
            SetRematchAccepted(playerId, false);
        }

        private void RefreshPlayerCount()
        {
            PlayerCount.Value = (byte)Mathf.Clamp(CountAssignedPlayers(), 0, MaxPlayers);
        }

        private int CountAssignedPlayers()
        {
            int count = 0;
            for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
            {
                if (GetClientId(playerId) != EmptyClientId)
                {
                    count++;
                }
            }

            return count;
        }

        private void SubscribeNetworkVariables()
        {
            PlayerCount.OnValueChanged += HandlePlayerCountValueChanged;
            Player1ClientId.OnValueChanged += HandleRaceUlongChanged;
            Player2ClientId.OnValueChanged += HandleRaceUlongChanged;
            Player3ClientId.OnValueChanged += HandleRaceUlongChanged;
            Player4ClientId.OnValueChanged += HandleRaceUlongChanged;
            HostReady.OnValueChanged += HandleReadyChanged;
            GuestReady.OnValueChanged += HandleReadyChanged;
            Player3Ready.OnValueChanged += HandleReadyChanged;
            Player4Ready.OnValueChanged += HandleReadyChanged;
            Phase.OnValueChanged += HandlePhaseChanged;
            CountdownValue.OnValueChanged += HandleCountdownChanged;
            HostAccelerationHeld.OnValueChanged += HandleRaceBoolChanged;
            GuestAccelerationHeld.OnValueChanged += HandleRaceBoolChanged;
            Player3AccelerationHeld.OnValueChanged += HandleRaceBoolChanged;
            Player4AccelerationHeld.OnValueChanged += HandleRaceBoolChanged;
            HostProgress.OnValueChanged += HandleRaceFloatChanged;
            GuestProgress.OnValueChanged += HandleRaceFloatChanged;
            Player3Progress.OnValueChanged += HandleRaceFloatChanged;
            Player4Progress.OnValueChanged += HandleRaceFloatChanged;
            HostSpeed.OnValueChanged += HandleRaceFloatChanged;
            GuestSpeed.OnValueChanged += HandleRaceFloatChanged;
            Player3Speed.OnValueChanged += HandleRaceFloatChanged;
            Player4Speed.OnValueChanged += HandleRaceFloatChanged;
            HostLap.OnValueChanged += HandleRaceByteChanged;
            GuestLap.OnValueChanged += HandleRaceByteChanged;
            Player3Lap.OnValueChanged += HandleRaceByteChanged;
            Player4Lap.OnValueChanged += HandleRaceByteChanged;
            HostPenaltyActive.OnValueChanged += HandleRaceBoolChanged;
            GuestPenaltyActive.OnValueChanged += HandleRaceBoolChanged;
            Player3PenaltyActive.OnValueChanged += HandleRaceBoolChanged;
            Player4PenaltyActive.OnValueChanged += HandleRaceBoolChanged;
            WinnerPlayerId.OnValueChanged += HandleWinnerChanged;
            HostFinishTimeSeconds.OnValueChanged += HandleRaceFloatChanged;
            GuestFinishTimeSeconds.OnValueChanged += HandleRaceFloatChanged;
            Player3FinishTimeSeconds.OnValueChanged += HandleRaceFloatChanged;
            Player4FinishTimeSeconds.OnValueChanged += HandleRaceFloatChanged;
            RematchRequested.OnValueChanged += HandleRaceBoolChanged;
            HostRematchAccepted.OnValueChanged += HandleRaceBoolChanged;
            GuestRematchAccepted.OnValueChanged += HandleRaceBoolChanged;
            Player3RematchAccepted.OnValueChanged += HandleRaceBoolChanged;
            Player4RematchAccepted.OnValueChanged += HandleRaceBoolChanged;
            RematchLobbySignal.OnValueChanged += HandleRaceByteChanged;
        }

        private void UnsubscribeNetworkVariables()
        {
            PlayerCount.OnValueChanged -= HandlePlayerCountValueChanged;
            Player1ClientId.OnValueChanged -= HandleRaceUlongChanged;
            Player2ClientId.OnValueChanged -= HandleRaceUlongChanged;
            Player3ClientId.OnValueChanged -= HandleRaceUlongChanged;
            Player4ClientId.OnValueChanged -= HandleRaceUlongChanged;
            HostReady.OnValueChanged -= HandleReadyChanged;
            GuestReady.OnValueChanged -= HandleReadyChanged;
            Player3Ready.OnValueChanged -= HandleReadyChanged;
            Player4Ready.OnValueChanged -= HandleReadyChanged;
            Phase.OnValueChanged -= HandlePhaseChanged;
            CountdownValue.OnValueChanged -= HandleCountdownChanged;
            HostAccelerationHeld.OnValueChanged -= HandleRaceBoolChanged;
            GuestAccelerationHeld.OnValueChanged -= HandleRaceBoolChanged;
            Player3AccelerationHeld.OnValueChanged -= HandleRaceBoolChanged;
            Player4AccelerationHeld.OnValueChanged -= HandleRaceBoolChanged;
            HostProgress.OnValueChanged -= HandleRaceFloatChanged;
            GuestProgress.OnValueChanged -= HandleRaceFloatChanged;
            Player3Progress.OnValueChanged -= HandleRaceFloatChanged;
            Player4Progress.OnValueChanged -= HandleRaceFloatChanged;
            HostSpeed.OnValueChanged -= HandleRaceFloatChanged;
            GuestSpeed.OnValueChanged -= HandleRaceFloatChanged;
            Player3Speed.OnValueChanged -= HandleRaceFloatChanged;
            Player4Speed.OnValueChanged -= HandleRaceFloatChanged;
            HostLap.OnValueChanged -= HandleRaceByteChanged;
            GuestLap.OnValueChanged -= HandleRaceByteChanged;
            Player3Lap.OnValueChanged -= HandleRaceByteChanged;
            Player4Lap.OnValueChanged -= HandleRaceByteChanged;
            HostPenaltyActive.OnValueChanged -= HandleRaceBoolChanged;
            GuestPenaltyActive.OnValueChanged -= HandleRaceBoolChanged;
            Player3PenaltyActive.OnValueChanged -= HandleRaceBoolChanged;
            Player4PenaltyActive.OnValueChanged -= HandleRaceBoolChanged;
            WinnerPlayerId.OnValueChanged -= HandleWinnerChanged;
            HostFinishTimeSeconds.OnValueChanged -= HandleRaceFloatChanged;
            GuestFinishTimeSeconds.OnValueChanged -= HandleRaceFloatChanged;
            Player3FinishTimeSeconds.OnValueChanged -= HandleRaceFloatChanged;
            Player4FinishTimeSeconds.OnValueChanged -= HandleRaceFloatChanged;
            RematchRequested.OnValueChanged -= HandleRaceBoolChanged;
            HostRematchAccepted.OnValueChanged -= HandleRaceBoolChanged;
            GuestRematchAccepted.OnValueChanged -= HandleRaceBoolChanged;
            Player3RematchAccepted.OnValueChanged -= HandleRaceBoolChanged;
            Player4RematchAccepted.OnValueChanged -= HandleRaceBoolChanged;
            RematchLobbySignal.OnValueChanged -= HandleRaceByteChanged;
        }

        private void ClearPlayerClientIds()
        {
            for (byte playerId = 1; playerId <= MaxPlayers; playerId++)
            {
                SetClientId(playerId, EmptyClientId);
            }
        }

        private void SetClientId(byte playerId, ulong clientId)
        {
            switch (playerId)
            {
                case 1:
                    Player1ClientId.Value = clientId;
                    break;
                case 2:
                    Player2ClientId.Value = clientId;
                    break;
                case 3:
                    Player3ClientId.Value = clientId;
                    break;
                case 4:
                    Player4ClientId.Value = clientId;
                    break;
            }
        }

        private void SetReady(byte playerId, bool ready)
        {
            switch (playerId)
            {
                case 1:
                    HostReady.Value = ready;
                    break;
                case 2:
                    GuestReady.Value = ready;
                    break;
                case 3:
                    Player3Ready.Value = ready;
                    break;
                case 4:
                    Player4Ready.Value = ready;
                    break;
            }
        }

        private void SetAccelerationHeld(byte playerId, bool isHeld)
        {
            switch (playerId)
            {
                case 1:
                    HostAccelerationHeld.Value = isHeld;
                    break;
                case 2:
                    GuestAccelerationHeld.Value = isHeld;
                    break;
                case 3:
                    Player3AccelerationHeld.Value = isHeld;
                    break;
                case 4:
                    Player4AccelerationHeld.Value = isHeld;
                    break;
            }
        }

        private void SetProgress(byte playerId, float progress)
        {
            switch (playerId)
            {
                case 1:
                    HostProgress.Value = progress;
                    break;
                case 2:
                    GuestProgress.Value = progress;
                    break;
                case 3:
                    Player3Progress.Value = progress;
                    break;
                case 4:
                    Player4Progress.Value = progress;
                    break;
            }
        }

        private void SetSpeed(byte playerId, float speed)
        {
            switch (playerId)
            {
                case 1:
                    HostSpeed.Value = speed;
                    break;
                case 2:
                    GuestSpeed.Value = speed;
                    break;
                case 3:
                    Player3Speed.Value = speed;
                    break;
                case 4:
                    Player4Speed.Value = speed;
                    break;
            }
        }

        private void SetLap(byte playerId, byte lap)
        {
            switch (playerId)
            {
                case 1:
                    HostLap.Value = lap;
                    break;
                case 2:
                    GuestLap.Value = lap;
                    break;
                case 3:
                    Player3Lap.Value = lap;
                    break;
                case 4:
                    Player4Lap.Value = lap;
                    break;
            }
        }

        private void SetPenalty(byte playerId, bool penaltyActive)
        {
            switch (playerId)
            {
                case 1:
                    HostPenaltyActive.Value = penaltyActive;
                    break;
                case 2:
                    GuestPenaltyActive.Value = penaltyActive;
                    break;
                case 3:
                    Player3PenaltyActive.Value = penaltyActive;
                    break;
                case 4:
                    Player4PenaltyActive.Value = penaltyActive;
                    break;
            }
        }

        private void SetFinishTime(byte playerId, float seconds)
        {
            switch (playerId)
            {
                case 1:
                    HostFinishTimeSeconds.Value = seconds;
                    break;
                case 2:
                    GuestFinishTimeSeconds.Value = seconds;
                    break;
                case 3:
                    Player3FinishTimeSeconds.Value = seconds;
                    break;
                case 4:
                    Player4FinishTimeSeconds.Value = seconds;
                    break;
            }
        }

        private void SetRematchAccepted(byte playerId, bool accepted)
        {
            switch (playerId)
            {
                case 1:
                    HostRematchAccepted.Value = accepted;
                    break;
                case 2:
                    GuestRematchAccepted.Value = accepted;
                    break;
                case 3:
                    Player3RematchAccepted.Value = accepted;
                    break;
                case 4:
                    Player4RematchAccepted.Value = accepted;
                    break;
            }
        }

        private static bool IsValidPlayerId(byte playerId)
        {
            return playerId >= 1 && playerId <= MaxPlayers;
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
        {
            SetReady(GetPlayerIdForClient(rpcParams.Receive.SenderClientId), ready);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetAccelerationHeldServerRpc(bool isHeld, ServerRpcParams rpcParams = default)
        {
            SetAccelerationHeld(GetPlayerIdForClient(rpcParams.Receive.SenderClientId), isHeld);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PrepareForRematchLobbyServerRpc()
        {
            ResetForLobbyOnServer(true);
        }

        [ServerRpc(RequireOwnership = false)]
        private void AcceptRematchServerRpc(ServerRpcParams rpcParams = default)
        {
            SetRematchAccepted(GetPlayerIdForClient(rpcParams.Receive.SenderClientId), true);
            TrySignalRematchLobby();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ReturnToLobbyFromPodiumServerRpc()
        {
            SignalReturnToLobby();
        }

        private void HandleClientConnected(ulong clientId)
        {
            AssignClientToSlot(clientId);
            RefreshPlayerCount();
            UnityEngine.Debug.Log("[SharedLobbyState] Client connected. PlayerCount=" + PlayerCount.Value);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            StartCoroutine(UpdatePlayerCountNextFrame(clientId));
        }

        private System.Collections.IEnumerator UpdatePlayerCountNextFrame(ulong clientId)
        {
            yield return null;
            if (IsServer && NetworkManager != null)
            {
                RemoveClientFromSlot(clientId);
                RefreshPlayerCount();
                UnityEngine.Debug.Log("[SharedLobbyState] Client disconnected. PlayerCount=" + PlayerCount.Value);
            }
        }

        private void HandlePlayerCountValueChanged(byte oldValue, byte newValue)
        {
            UnityEngine.Debug.Log("[SharedLobbyState] PlayerCount changed: " + oldValue + " -> " + newValue);
            OnPlayerCountChanged?.Invoke(oldValue, newValue);
            OnRaceStateChanged?.Invoke();
        }

        private void HandleReadyChanged(bool oldValue, bool newValue)
        {
            OnReadyStateChanged?.Invoke(HostReady.Value, GuestReady.Value);
            OnRaceStateChanged?.Invoke();
        }

        private void HandlePhaseChanged(RacePhase oldValue, RacePhase newValue)
        {
            OnPhaseChanged?.Invoke(newValue);
        }

        private void HandleCountdownChanged(byte oldValue, byte newValue)
        {
            OnCountdownTick?.Invoke(newValue);
        }

        private void HandleRaceFloatChanged(float oldValue, float newValue)
        {
            OnRaceStateChanged?.Invoke();
        }

        private void HandleRaceByteChanged(byte oldValue, byte newValue)
        {
            OnRaceStateChanged?.Invoke();
        }

        private void HandleRaceBoolChanged(bool oldValue, bool newValue)
        {
            OnRaceStateChanged?.Invoke();
        }

        private void HandleRaceUlongChanged(ulong oldValue, ulong newValue)
        {
            OnRaceStateChanged?.Invoke();
        }

        private void HandleWinnerChanged(byte oldValue, byte newValue)
        {
            OnRaceStateChanged?.Invoke();
            OnWinnerChanged?.Invoke(newValue);
        }
    }
}
