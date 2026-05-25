using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SlotCarRacingAR.Runtime.Infrastructure
{
    /// <summary>
    /// LAN discovery via UDP broadcast. Host broadcasts a beacon; guests listen and report found hosts.
    /// </summary>
    public sealed class LanDiscovery : MonoBehaviour
    {
        private const int DiscoveryPort = 47777;
        private const float BroadcastInterval = 1.0f;
        private const string MagicHeader = "SLOTCAR_v1";

        private UdpClient _udpClient;
        private Thread _listenThread;
        private bool _isBroadcasting;
        private bool _isListening;
        private float _broadcastTimer;
        private string _hostName;
        private string _hostIp;
        private int _gamePort;

        /// <summary>Fired on the main thread when a host is discovered (ip, port, hostName).</summary>
        public event Action<string, int, string> OnHostDiscovered;

        private readonly object _discoveredLock = new();
        private string _pendingIp;
        private int _pendingPort;
        private string _pendingName;
        private bool _hasPending;

        private void Update()
        {
            if (_isBroadcasting)
            {
                _broadcastTimer -= Time.unscaledDeltaTime;
                if (_broadcastTimer <= 0f)
                {
                    _broadcastTimer = BroadcastInterval;
                    SendBroadcast();
                }
            }

            // Dispatch discovered hosts on main thread
            lock (_discoveredLock)
            {
                if (_hasPending)
                {
                    _hasPending = false;
                    OnHostDiscovered?.Invoke(_pendingIp, _pendingPort, _pendingName);
                }
            }
        }

        private void OnDestroy()
        {
            StopAll();
        }

        /// <summary>Start broadcasting this host's presence on LAN.</summary>
        public void StartBroadcasting(string hostIp, int gamePort, string hostName = "Partida")
        {
            StopAll();
            _hostIp = hostIp;
            _gamePort = gamePort;
            _hostName = hostName;
            _isBroadcasting = true;
            _broadcastTimer = 0f; // send immediately
            UnityEngine.Debug.Log("[LanDiscovery] Broadcasting started on port " + DiscoveryPort);
        }

        /// <summary>Start listening for host beacons on LAN.</summary>
        public void StartListening()
        {
            StopAll();
            _isListening = true;

            _listenThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "LanDiscoveryListener"
            };
            _listenThread.Start();
            UnityEngine.Debug.Log("[LanDiscovery] Listening for hosts on port " + DiscoveryPort);
        }

        public void StopAll()
        {
            _isBroadcasting = false;
            _isListening = false;

            if (_udpClient != null)
            {
                try { _udpClient.Close(); } catch { }
                _udpClient = null;
            }

            if (_listenThread != null && _listenThread.IsAlive)
            {
                _listenThread.Join(500);
                _listenThread = null;
            }
        }

        private void SendBroadcast()
        {
            try
            {
                string message = MagicHeader + "|" + _hostIp + "|" + _gamePort + "|" + _hostName;
                byte[] data = Encoding.UTF8.GetBytes(message);

                using UdpClient client = new();
                client.EnableBroadcast = true;
                // Send to global broadcast
                client.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));

                // Also send to subnet broadcast (for hotspot scenarios)
                IPAddress subnetBroadcast = GetSubnetBroadcast(_hostIp);
                if (subnetBroadcast != null && !subnetBroadcast.Equals(IPAddress.Broadcast))
                {
                    client.Send(data, data.Length, new IPEndPoint(subnetBroadcast, DiscoveryPort));
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[LanDiscovery] Broadcast error: " + e.Message);
            }
        }

        private void ListenLoop()
        {
            try
            {
                _udpClient = new UdpClient(DiscoveryPort);
                _udpClient.EnableBroadcast = true;
                _udpClient.Client.ReceiveTimeout = 2000;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[LanDiscovery] Could not bind listener: " + e.Message);
                return;
            }

            while (_isListening)
            {
                try
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udpClient.Receive(ref remote);
                    string message = Encoding.UTF8.GetString(data);

                    if (!message.StartsWith(MagicHeader + "|")) continue;

                    string[] parts = message.Split('|');
                    if (parts.Length < 4) continue;

                    string ip = parts[1];
                    int port = int.Parse(parts[2]);
                    string name = parts[3];

                    lock (_discoveredLock)
                    {
                        _pendingIp = ip;
                        _pendingPort = port;
                        _pendingName = name;
                        _hasPending = true;
                    }
                }
                catch (SocketException)
                {
                    // Timeout — normal, loop continues
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (_isListening)
                        UnityEngine.Debug.LogWarning("[LanDiscovery] Listen error: " + e.Message);
                }
            }

            try { _udpClient?.Close(); } catch { }
        }

        /// <summary>
        /// Calculates subnet broadcast address assuming /24 mask (common for hotspots and home LANs).
        /// </summary>
        private static IPAddress GetSubnetBroadcast(string hostIp)
        {
            try
            {
                string[] parts = hostIp.Split('.');
                if (parts.Length != 4) return null;
                return IPAddress.Parse(parts[0] + "." + parts[1] + "." + parts[2] + ".255");
            }
            catch
            {
                return null;
            }
        }
    }
}
