using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace SlotCarRacingAR.Runtime.Infrastructure
{
    public enum SessionState
    {
        Idle,
        Creating,
        Joining,
        WaitingForPlayer,
        Connected,
        Failed
    }

    public enum PlayerRole
    {
        None,
        Host,
        Guest
    }

    /// <summary>
    /// Wraps NetworkManager for local session lifecycle.
    /// Thin MonoBehaviour: serializes nothing, delegates to NGO, exposes typed events.
    /// </summary>
    public sealed class SessionManager : MonoBehaviour
    {
        private const float JoinTimeoutSeconds = 5f;

        private SessionState _state = SessionState.Idle;
        private PlayerRole _role = PlayerRole.None;
        private int _playerId;
        private string _failureReason = string.Empty;
        private Coroutine _joinTimeoutCoroutine;
        private string _lastHostIp = string.Empty;

        public SessionState State => _state;
        public PlayerRole Role => _role;
        public int PlayerId => _playerId;
        public string FailureReason => _failureReason;

        /// <summary>Fired every time the session state changes.</summary>
        public event Action<SessionState> OnSessionStateChanged;

        public void StartHostSession()
        {
            if (_state != SessionState.Idle && _state != SessionState.Failed)
            {
                UnityEngine.Debug.LogWarning("[SessionManager] Cannot start host — state is " + _state);
                return;
            }

            SetState(SessionState.Creating);

            NetworkManager nm = EnsureNetworkManager();
            if (nm == null)
            {
                Fail("Could not create NetworkManager.");
                return;
            }

            // Configure transport for LAN
            UnityTransport transport = nm.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Fail("UnityTransport component not found on NetworkManager.");
                return;
            }

            transport.ConnectionData.Address = "0.0.0.0";
            transport.ConnectionData.Port = 7777;
            transport.ConnectionData.ServerListenAddress = "0.0.0.0";

            // Subscribe to connection callbacks before starting
            nm.OnClientConnectedCallback += OnClientConnected;
            nm.OnClientDisconnectCallback += OnClientDisconnected;

            bool started = nm.StartHost();
            if (!started)
            {
                nm.OnClientConnectedCallback -= OnClientConnected;
                nm.OnClientDisconnectCallback -= OnClientDisconnected;
                Fail("Failed to start host. Check network adapter availability.");
                return;
            }

            _role = PlayerRole.Host;
            _playerId = 1;
            SetState(SessionState.WaitingForPlayer);
            UnityEngine.Debug.Log("[SessionManager] Host started. Waiting for player on " + GetLocalIPAddress() + ":7777");
        }

        public void StartGuestSession(string hostIp)
        {
            if (_state != SessionState.Idle && _state != SessionState.Failed)
            {
                UnityEngine.Debug.LogWarning("[SessionManager] Cannot join — state is " + _state);
                return;
            }

            if (string.IsNullOrWhiteSpace(hostIp))
            {
                Fail("Host IP address is empty.");
                return;
            }

            _lastHostIp = hostIp;
            SetState(SessionState.Joining);

            NetworkManager nm = EnsureNetworkManager();
            if (nm == null)
            {
                Fail("Could not create NetworkManager.");
                return;
            }

            UnityTransport transport = nm.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Fail("UnityTransport component not found on NetworkManager.");
                return;
            }

            transport.ConnectionData.Address = hostIp;
            transport.ConnectionData.Port = 7777;

            nm.OnClientConnectedCallback += OnGuestConnected;
            nm.OnClientDisconnectCallback += OnGuestDisconnected;

            bool started = nm.StartClient();
            if (!started)
            {
                nm.OnClientConnectedCallback -= OnGuestConnected;
                nm.OnClientDisconnectCallback -= OnGuestDisconnected;
                Fail("Failed to start client. Check network settings.");
                return;
            }

            _role = PlayerRole.Guest;
            _playerId = 2;
            _joinTimeoutCoroutine = StartCoroutine(JoinTimeoutRoutine());
            UnityEngine.Debug.Log("[SessionManager] Guest connecting to " + hostIp + ":7777...");
        }

        public void RetryGuestSession()
        {
            Shutdown();
            StartGuestSession(_lastHostIp);
        }

        private IEnumerator JoinTimeoutRoutine()
        {
            yield return new WaitForSeconds(JoinTimeoutSeconds);

            if (_state == SessionState.Joining)
            {
                NetworkManager nm = NetworkManager.Singleton;
                if (nm != null)
                {
                    nm.OnClientConnectedCallback -= OnGuestConnected;
                    nm.OnClientDisconnectCallback -= OnGuestDisconnected;
                    if (nm.IsListening) nm.Shutdown();
                }
                Fail("Connection timed out. Ensure both devices are on the same Wi-Fi / hotspot.");
            }
            _joinTimeoutCoroutine = null;
        }

        private void OnGuestConnected(ulong clientId)
        {
            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null) return;

            // For the guest, our own connection is the success signal
            if (clientId != nm.LocalClientId) return;

            CancelJoinTimeout();
            UnityEngine.Debug.Log("[SessionManager] Guest connected to host!");
            PlayerPrefs.SetString("LastHostIP", _lastHostIp);
            PlayerPrefs.Save();
            SetState(SessionState.Connected);
        }

        private void OnGuestDisconnected(ulong clientId)
        {
            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null) return;
            if (clientId != nm.LocalClientId) return;

            CancelJoinTimeout();

            if (_state == SessionState.Joining || _state == SessionState.Connected)
            {
                Fail("Disconnected from host. Check network connection.");
            }
        }

        private void CancelJoinTimeout()
        {
            if (_joinTimeoutCoroutine != null)
            {
                StopCoroutine(_joinTimeoutCoroutine);
                _joinTimeoutCoroutine = null;
            }
        }

        /// <summary>
        /// Returns the existing NetworkManager.Singleton or creates one programmatically.
        /// NGO's NetworkManager handles DontDestroyOnLoad internally.
        /// </summary>
        private static NetworkManager EnsureNetworkManager()
        {
            if (NetworkManager.Singleton != null)
                return NetworkManager.Singleton;

            GameObject nmObj = new GameObject("NetworkManager");
            NetworkManager nm = nmObj.AddComponent<NetworkManager>();
            UnityTransport transport = nmObj.AddComponent<UnityTransport>();
            nm.NetworkConfig.NetworkTransport = transport;
            return nm;
        }

        public void Shutdown()
        {
            CancelJoinTimeout();

            NetworkManager nm = NetworkManager.Singleton;
            if (nm != null)
            {
                nm.OnClientConnectedCallback -= OnClientConnected;
                nm.OnClientDisconnectCallback -= OnClientDisconnected;
                nm.OnClientConnectedCallback -= OnGuestConnected;
                nm.OnClientDisconnectCallback -= OnGuestDisconnected;

                if (nm.IsListening)
                {
                    nm.Shutdown();
                }
            }

            _role = PlayerRole.None;
            _playerId = 0;
            _failureReason = string.Empty;
            SetState(SessionState.Idle);
        }

        public void RetryHostSession()
        {
            Shutdown();
            StartHostSession();
        }

        public string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SessionManager] Could not resolve local IP: " + e.Message);
            }
            return "127.0.0.1";
        }

        private void OnClientConnected(ulong clientId)
        {
            // Ignore our own connection (host local client)
            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null) return;
            if (clientId == nm.LocalClientId) return;

            // A remote client connected — we have player 2
            UnityEngine.Debug.Log("[SessionManager] Client connected: " + clientId);
            SetState(SessionState.Connected);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null) return;
            if (clientId == nm.LocalClientId) return;

            // If we were connected and remote leaves, go back to waiting
            if (_state == SessionState.Connected)
            {
                UnityEngine.Debug.Log("[SessionManager] Client disconnected: " + clientId + " — back to waiting.");
                SetState(SessionState.WaitingForPlayer);
            }
        }

        private void Fail(string reason)
        {
            _failureReason = reason;
            UnityEngine.Debug.LogError("[SessionManager] " + reason);
            SetState(SessionState.Failed);
        }

        private void SetState(SessionState newState)
        {
            if (_state == newState) return;
            _state = newState;
            OnSessionStateChanged?.Invoke(newState);
        }

        private void OnDestroy()
        {
            CancelJoinTimeout();

            NetworkManager nm = NetworkManager.Singleton;
            if (nm != null)
            {
                nm.OnClientConnectedCallback -= OnClientConnected;
                nm.OnClientDisconnectCallback -= OnClientDisconnected;
                nm.OnClientConnectedCallback -= OnGuestConnected;
                nm.OnClientDisconnectCallback -= OnGuestDisconnected;
            }
        }
    }
}
