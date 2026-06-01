using System;
using Unity.Netcode;
using UnityEngine;

namespace SlotCarRacingAR.Runtime.Infrastructure
{
    /// <summary>
    /// Race phase as seen by both players.
    /// </summary>
    public enum RacePhase : byte
    {
        Setup = 0,
        Countdown = 1,
        Racing = 2,
        Finished = 3
    }

    /// <summary>
    /// Networked session state visible to both host and guest.
    /// Persists across scene loads (DontDestroyOnLoad via NGO).
    /// Host-authoritative: only server writes, everyone reads.
    /// Used in Lobby for player count, and in Race for readiness/countdown.
    /// </summary>
    public sealed class SharedLobbyState : NetworkBehaviour
    {
        public const byte RaceLapTarget = 3;

        // ─── Lobby State ───
        /// <summary>Current number of connected players (1 or 2).</summary>
        public NetworkVariable<byte> PlayerCount = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // ─── Race Setup State ───
        public NetworkVariable<bool> HostReady = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> GuestReady = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>Current race phase visible to both devices.</summary>
        public NetworkVariable<RacePhase> Phase = new(
            RacePhase.Setup,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>Countdown seconds remaining (3, 2, 1, 0=GO). Updated by host.</summary>
        public NetworkVariable<byte> CountdownValue = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // ─── Events ───
        /// <summary>Fired locally when player count changes (old, new).</summary>
        public event Action<byte, byte> OnPlayerCountChanged;

        /// <summary>Fired locally when either ready state changes. Args: (hostReady, guestReady).</summary>
        public event Action<bool, bool> OnReadyStateChanged;

        /// <summary>Fired when race phase changes.</summary>
        public event Action<RacePhase> OnPhaseChanged;

        /// <summary>Fired when countdown value changes (3, 2, 1, 0).</summary>
        public event Action<byte> OnCountdownTick;

        public NetworkVariable<bool> HostAccelerationHeld = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> GuestAccelerationHeld = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> HostProgress = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> GuestProgress = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> HostSpeed = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> GuestSpeed = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<byte> HostLap = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<byte> GuestLap = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> HostPenaltyActive = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> GuestPenaltyActive = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<byte> WinnerPlayerId = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> HostFinishTimeSeconds = new(
            -1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> GuestFinishTimeSeconds = new(
            -1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> RematchRequested = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> HostRematchAccepted = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> GuestRematchAccepted = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<byte> RematchLobbySignal = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public event Action OnRaceStateChanged;

        public event Action<byte> OnWinnerChanged;

        public override void OnNetworkSpawn()
        {
            // Persist as a root object so scene loads don't destroy the session state.
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            if (IsServer)
            {
                PlayerCount.Value = (byte)NetworkManager.ConnectedClientsIds.Count;
                NetworkManager.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }

            PlayerCount.OnValueChanged += HandlePlayerCountValueChanged;
            HostReady.OnValueChanged += HandleReadyChanged;
            GuestReady.OnValueChanged += HandleReadyChanged;
            Phase.OnValueChanged += HandlePhaseChanged;
            CountdownValue.OnValueChanged += HandleCountdownChanged;
            HostAccelerationHeld.OnValueChanged += HandleRaceBoolChanged;
            GuestAccelerationHeld.OnValueChanged += HandleRaceBoolChanged;
            HostProgress.OnValueChanged += HandleRaceFloatChanged;
            GuestProgress.OnValueChanged += HandleRaceFloatChanged;
            HostSpeed.OnValueChanged += HandleRaceFloatChanged;
            GuestSpeed.OnValueChanged += HandleRaceFloatChanged;
            HostLap.OnValueChanged += HandleRaceByteChanged;
            GuestLap.OnValueChanged += HandleRaceByteChanged;
            HostPenaltyActive.OnValueChanged += HandleRaceBoolChanged;
            GuestPenaltyActive.OnValueChanged += HandleRaceBoolChanged;
            WinnerPlayerId.OnValueChanged += HandleWinnerChanged;
            HostFinishTimeSeconds.OnValueChanged += HandleRaceFloatChanged;
            GuestFinishTimeSeconds.OnValueChanged += HandleRaceFloatChanged;
            RematchRequested.OnValueChanged += HandleRaceBoolChanged;
            HostRematchAccepted.OnValueChanged += HandleRaceBoolChanged;
            GuestRematchAccepted.OnValueChanged += HandleRaceBoolChanged;
            RematchLobbySignal.OnValueChanged += HandleRaceByteChanged;
            UnityEngine.Debug.Log("[SharedLobbyState] Spawned. PlayerCount=" + PlayerCount.Value + " IsServer=" + IsServer);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            PlayerCount.OnValueChanged -= HandlePlayerCountValueChanged;
            HostReady.OnValueChanged -= HandleReadyChanged;
            GuestReady.OnValueChanged -= HandleReadyChanged;
            Phase.OnValueChanged -= HandlePhaseChanged;
            CountdownValue.OnValueChanged -= HandleCountdownChanged;
            HostAccelerationHeld.OnValueChanged -= HandleRaceBoolChanged;
            GuestAccelerationHeld.OnValueChanged -= HandleRaceBoolChanged;
            HostProgress.OnValueChanged -= HandleRaceFloatChanged;
            GuestProgress.OnValueChanged -= HandleRaceFloatChanged;
            HostSpeed.OnValueChanged -= HandleRaceFloatChanged;
            GuestSpeed.OnValueChanged -= HandleRaceFloatChanged;
            HostLap.OnValueChanged -= HandleRaceByteChanged;
            GuestLap.OnValueChanged -= HandleRaceByteChanged;
            HostPenaltyActive.OnValueChanged -= HandleRaceBoolChanged;
            GuestPenaltyActive.OnValueChanged -= HandleRaceBoolChanged;
            WinnerPlayerId.OnValueChanged -= HandleWinnerChanged;
            HostFinishTimeSeconds.OnValueChanged -= HandleRaceFloatChanged;
            GuestFinishTimeSeconds.OnValueChanged -= HandleRaceFloatChanged;
            RematchRequested.OnValueChanged -= HandleRaceBoolChanged;
            HostRematchAccepted.OnValueChanged -= HandleRaceBoolChanged;
            GuestRematchAccepted.OnValueChanged -= HandleRaceBoolChanged;
            RematchLobbySignal.OnValueChanged -= HandleRaceByteChanged;
        }

        // ─── Race Setup Methods ───

        /// <summary>
        /// Called locally by either player to toggle their ready state.
        /// Host sets directly; guest uses ServerRpc.
        /// </summary>
        public void SetLocalReady(bool ready)
        {
            if (IsServer)
            {
                HostReady.Value = ready;
            }
            else
            {
                SetGuestReadyServerRpc(ready);
            }
        }

        /// <summary>Revoke all readiness (e.g. when tracking is lost).</summary>
        public void RevokeAllReadiness()
        {
            if (!IsServer) return;
            HostReady.Value = false;
            GuestReady.Value = false;
        }

        /// <summary>Host initiates countdown phase.</summary>
        public void BeginCountdown()
        {
            if (!IsServer) return;
            Phase.Value = RacePhase.Countdown;
            CountdownValue.Value = 3;
        }

        /// <summary>Host ticks countdown down.</summary>
        public void TickCountdown(byte value)
        {
            if (!IsServer) return;
            CountdownValue.Value = value;
        }

        /// <summary>Host transitions to racing phase.</summary>
        public void BeginRacing()
        {
            if (!IsServer) return;
            ResetRaceState();
            Phase.Value = RacePhase.Racing;
        }

        public bool BothReady => HostReady.Value && GuestReady.Value;

        public void SetLocalAccelerationHeld(bool isHeld)
        {
            if (IsServer)
            {
                HostAccelerationHeld.Value = isHeld;
            }
            else
            {
                SetGuestAccelerationHeldServerRpc(isHeld);
            }
        }

        public void ResetRaceState()
        {
            if (!IsServer) return;

            HostAccelerationHeld.Value = false;
            GuestAccelerationHeld.Value = false;
            HostProgress.Value = 0f;
            GuestProgress.Value = 0f;
            HostSpeed.Value = 0f;
            GuestSpeed.Value = 0f;
            HostLap.Value = 0;
            GuestLap.Value = 0;
            HostPenaltyActive.Value = false;
            GuestPenaltyActive.Value = false;
            WinnerPlayerId.Value = 0;
            HostFinishTimeSeconds.Value = -1f;
            GuestFinishTimeSeconds.Value = -1f;
        }

        public void PublishRaceState(byte playerId, float progress, float speed, byte lap, bool penaltyActive)
        {
            if (!IsServer) return;

            progress = Mathf.Repeat(progress, 1f);
            speed = Mathf.Max(0f, speed);

            if (playerId == 1)
            {
                HostProgress.Value = progress;
                HostSpeed.Value = speed;
                HostLap.Value = lap;
                HostPenaltyActive.Value = penaltyActive;
            }
            else if (playerId == 2)
            {
                GuestProgress.Value = progress;
                GuestSpeed.Value = speed;
                GuestLap.Value = lap;
                GuestPenaltyActive.Value = penaltyActive;
            }
        }

        public void FinishRace(byte winnerPlayerId, float hostFinishTimeSeconds, float guestFinishTimeSeconds)
        {
            if (!IsServer || winnerPlayerId == 0) return;

            HostFinishTimeSeconds.Value = Mathf.Max(0f, hostFinishTimeSeconds);
            GuestFinishTimeSeconds.Value = Mathf.Max(0f, guestFinishTimeSeconds);
            WinnerPlayerId.Value = winnerPlayerId;
            HostAccelerationHeld.Value = false;
            GuestAccelerationHeld.Value = false;
            Phase.Value = RacePhase.Finished;
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
            HostRematchAccepted.Value = true;
            GuestRematchAccepted.Value = false;
        }

        public void AcceptRematch()
        {
            if (IsServer)
            {
                HostRematchAccepted.Value = true;
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

        private void ResetForLobbyOnServer(bool clearRematchState)
        {
            HostReady.Value = false;
            GuestReady.Value = false;
            CountdownValue.Value = 0;
            ResetRaceState();
            Phase.Value = RacePhase.Setup;

            if (clearRematchState)
            {
                RematchRequested.Value = false;
                HostRematchAccepted.Value = false;
                GuestRematchAccepted.Value = false;
            }
        }

        private void TrySignalRematchLobby()
        {
            if (!IsServer || !RematchRequested.Value)
            {
                return;
            }

            bool needsGuestAcceptance = PlayerCount.Value >= 2;
            if (!HostRematchAccepted.Value || (needsGuestAcceptance && !GuestRematchAccepted.Value))
            {
                return;
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

        [ServerRpc(RequireOwnership = false)]
        private void PrepareForRematchLobbyServerRpc()
        {
            ResetForLobbyOnServer(true);
        }

        [ServerRpc(RequireOwnership = false)]
        private void AcceptRematchServerRpc()
        {
            GuestRematchAccepted.Value = true;
            TrySignalRematchLobby();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ReturnToLobbyFromPodiumServerRpc()
        {
            SignalReturnToLobby();
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetGuestReadyServerRpc(bool ready)
        {
            GuestReady.Value = ready;
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetGuestAccelerationHeldServerRpc(bool isHeld)
        {
            GuestAccelerationHeld.Value = isHeld;
        }

        // ─── Lobby Handlers ───

        private void HandleClientConnected(ulong clientId)
        {
            PlayerCount.Value = (byte)NetworkManager.ConnectedClientsIds.Count;
            UnityEngine.Debug.Log("[SharedLobbyState] Client connected. PlayerCount=" + PlayerCount.Value);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            StartCoroutine(UpdatePlayerCountNextFrame());
        }

        private System.Collections.IEnumerator UpdatePlayerCountNextFrame()
        {
            yield return null;
            if (IsServer && NetworkManager != null)
            {
                PlayerCount.Value = (byte)NetworkManager.ConnectedClientsIds.Count;
                UnityEngine.Debug.Log("[SharedLobbyState] Client disconnected. PlayerCount=" + PlayerCount.Value);
            }
        }

        private void HandlePlayerCountValueChanged(byte oldValue, byte newValue)
        {
            UnityEngine.Debug.Log("[SharedLobbyState] PlayerCount changed: " + oldValue + " → " + newValue);
            OnPlayerCountChanged?.Invoke(oldValue, newValue);
        }

        private void HandleReadyChanged(bool oldValue, bool newValue)
        {
            OnReadyStateChanged?.Invoke(HostReady.Value, GuestReady.Value);
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

        private void HandleWinnerChanged(byte oldValue, byte newValue)
        {
            OnRaceStateChanged?.Invoke();
            OnWinnerChanged?.Invoke(newValue);
        }
    }
}
