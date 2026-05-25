using System;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
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
        private const float JoinTimeoutSeconds = 10f;

        private SessionState _state = SessionState.Idle;
        private PlayerRole _role = PlayerRole.None;
        private int _playerId;
        private string _failureReason = string.Empty;
        private Coroutine _joinTimeoutCoroutine;
        private string _lastHostIp = string.Empty;
        private int _lastHostPort = 7777;
        private ushort _boundPort = 7777;
        private GameObject _sharedLobbyStatePrefab;

        public SessionState State => _state;
        public PlayerRole Role => _role;
        public int PlayerId => _playerId;
        public string FailureReason => _failureReason;
        public ushort BoundPort => _boundPort;

        /// <summary>Fired every time the session state changes.</summary>
        public event Action<SessionState> OnSessionStateChanged;

        public void SetSharedLobbyStatePrefab(GameObject prefab)
        {
            _sharedLobbyStatePrefab = prefab;

            NetworkManager nm = NetworkManager.Singleton;
            if (nm != null)
            {
                RegisterSharedLobbyStatePrefab(nm);
            }
        }

        private void RegisterSharedLobbyStatePrefab(NetworkManager nm)
        {
            if (nm == null || _sharedLobbyStatePrefab == null)
            {
                return;
            }

            NetworkObject networkObject = _sharedLobbyStatePrefab.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                UnityEngine.Debug.LogError("[SessionManager] SharedLobbyState prefab is missing a NetworkObject.");
                return;
            }

            if (nm.NetworkConfig.Prefabs.Contains(_sharedLobbyStatePrefab))
            {
                return;
            }

            nm.AddNetworkPrefab(_sharedLobbyStatePrefab);
            UnityEngine.Debug.Log("[SessionManager] Registered SharedLobbyState prefab. PrefabHash=" + networkObject.PrefabIdHash);
        }

        public void StartHostSession()
        {
            if (_state != SessionState.Idle && _state != SessionState.Failed)
            {
                UnityEngine.Debug.LogWarning("[SessionManager] Cannot start host — state is " + _state);
                return;
            }

            _role = PlayerRole.Host;
            _playerId = 1;
            SetState(SessionState.Creating);
            StartCoroutine(StartHostRoutine());
        }

        private IEnumerator StartHostRoutine()
        {
            // If a previous NetworkManager exists, shut it down and wait for port release
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                }
                // Destroy old NM and wait for socket to fully close
                UnityEngine.Object.DestroyImmediate(NetworkManager.Singleton.gameObject);
                yield return null; // wait one frame for OS to release the port
            }

            NetworkManager nm;
            try
            {
                nm = EnsureNetworkManager();
            }
            catch (System.Exception e)
            {
                Fail("Exception creating NetworkManager: " + e.Message);
                yield break;
            }

            if (nm == null)
            {
                Fail("Could not create NetworkManager.");
                yield break;
            }

            RegisterSharedLobbyStatePrefab(nm);

            // Configure transport for LAN
            UnityTransport transport = nm.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Fail("UnityTransport component not found on NetworkManager.");
                yield break;
            }

            // Try ports 7777-7780 in case one is still bound from a previous session
            bool started = false;
            ushort boundPort = 0;
            for (ushort port = 7777; port <= 7780; port++)
            {
                try
                {
                    transport.ConnectionData.Address = "0.0.0.0";
                    transport.ConnectionData.Port = port;
                    transport.ConnectionData.ServerListenAddress = "0.0.0.0";
                }
                catch (System.Exception e)
                {
                    Fail("Exception configuring transport: " + e.Message);
                    yield break;
                }

                nm.OnClientConnectedCallback += OnClientConnected;
                nm.OnClientDisconnectCallback += OnClientDisconnected;

                try
                {
                    started = nm.StartHost();
                }
                catch (System.Exception e)
                {
                    nm.OnClientConnectedCallback -= OnClientConnected;
                    nm.OnClientDisconnectCallback -= OnClientDisconnected;
                    Fail("Exception starting host: " + e.Message);
                    yield break;
                }

                if (started)
                {
                    boundPort = port;
                    break;
                }

                // Port failed, clean up callbacks and try next
                nm.OnClientConnectedCallback -= OnClientConnected;
                nm.OnClientDisconnectCallback -= OnClientDisconnected;
                UnityEngine.Debug.LogWarning("[SessionManager] Port " + port + " unavailable, trying next...");
                yield return null;
            }

            if (!started)
            {
                Fail("Could not bind to any port (7777-7780). Close other network apps or restart Unity.");
                yield break;
            }

            // StartHost succeeded
            _boundPort = boundPort;
            string ip = GetLocalIPAddress();
            UnityEngine.Debug.Log("[SessionManager] Host started successfully. IP=" + ip + ":" + boundPort);
            SetState(SessionState.WaitingForPlayer);
        }

        public void StartGuestSession(string hostIp, int port = 7777)
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
            _lastHostPort = port;
            _role = PlayerRole.Guest;
            _playerId = 2;
            SetState(SessionState.Joining);

            NetworkManager nm = EnsureNetworkManager();
            if (nm == null)
            {
                Fail("Could not create NetworkManager.");
                return;
            }

            RegisterSharedLobbyStatePrefab(nm);

            UnityTransport transport = nm.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Fail("UnityTransport component not found on NetworkManager.");
                return;
            }

            transport.ConnectionData.Address = hostIp;
            transport.ConnectionData.Port = (ushort)port;

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

            _joinTimeoutCoroutine = StartCoroutine(JoinTimeoutRoutine());
            UnityEngine.Debug.Log("[SessionManager] Guest connecting to " + hostIp + ":" + port + "...");
        }

        public void RetryGuestSession()
        {
            Shutdown();
            StartGuestSession(_lastHostIp, _lastHostPort);
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
            // Reuse existing NetworkManager if available (caller must have shut it down already)
            if (NetworkManager.Singleton != null)
            {
                return NetworkManager.Singleton;
            }

            GameObject nmObj = new GameObject("NetworkManager");
            UnityTransport transport = nmObj.AddComponent<UnityTransport>();
            NetworkManager nm = nmObj.AddComponent<NetworkManager>();

            // NetworkConfig is initialized in Awake(), which hasn't run yet
            // after AddComponent. Force initialization if null.
            if (nm.NetworkConfig == null)
            {
                nm.NetworkConfig = new Unity.Netcode.NetworkConfig();
            }
            nm.NetworkConfig.NetworkTransport = transport;
            nm.NetworkConfig.EnableSceneManagement = false;
            nm.NetworkConfig.ForceSamePrefabs = false;
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
            // Method 1: UDP socket trick (works when internet route exists)
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Connect("8.8.8.8", 80);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    if (endPoint != null && !IPAddress.IsLoopback(endPoint.Address))
                        return endPoint.Address.ToString();
                }
            }
            catch { }

            // Method 2: Enumerate network interfaces (works for hotspot/no-internet)
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    foreach (UnicastIPAddressInformation addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork
                            && !IPAddress.IsLoopback(addr.Address))
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SessionManager] NetworkInterface enumeration failed: " + e.Message);
            }

            // Method 3: DNS fallback
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                        return ip.ToString();
                }
            }
            catch { }

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
