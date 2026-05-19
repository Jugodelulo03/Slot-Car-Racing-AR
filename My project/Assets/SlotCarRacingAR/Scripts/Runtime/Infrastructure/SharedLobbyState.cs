using Unity.Netcode;
using UnityEngine;

namespace SlotCarRacingAR.Runtime.Infrastructure
{
    /// <summary>
    /// Networked lobby state visible to both host and guest.
    /// Host-authoritative: only server writes, everyone reads.
    /// </summary>
    public sealed class SharedLobbyState : NetworkBehaviour
    {
        /// <summary>Current number of connected players (1 or 2).</summary>
        public NetworkVariable<byte> PlayerCount = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>Fired locally when player count changes (old, new).</summary>
        public event System.Action<byte, byte> OnPlayerCountChanged;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                PlayerCount.Value = (byte)NetworkManager.ConnectedClientsIds.Count;
                NetworkManager.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }

            PlayerCount.OnValueChanged += HandlePlayerCountValueChanged;
            Debug.Log("[SharedLobbyState] Spawned. PlayerCount=" + PlayerCount.Value + " IsServer=" + IsServer);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            PlayerCount.OnValueChanged -= HandlePlayerCountValueChanged;
        }

        private void HandleClientConnected(ulong clientId)
        {
            PlayerCount.Value = (byte)NetworkManager.ConnectedClientsIds.Count;
            Debug.Log("[SharedLobbyState] Client connected. PlayerCount=" + PlayerCount.Value);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            // ConnectedClientsIds still includes the disconnecting client at this point in some NGO versions
            // Schedule a frame-delayed update to get accurate count
            StartCoroutine(UpdatePlayerCountNextFrame());
        }

        private System.Collections.IEnumerator UpdatePlayerCountNextFrame()
        {
            yield return null;
            if (IsServer && NetworkManager != null)
            {
                PlayerCount.Value = (byte)NetworkManager.ConnectedClientsIds.Count;
                Debug.Log("[SharedLobbyState] Client disconnected. PlayerCount=" + PlayerCount.Value);
            }
        }

        private void HandlePlayerCountValueChanged(byte oldValue, byte newValue)
        {
            Debug.Log("[SharedLobbyState] PlayerCount changed: " + oldValue + " → " + newValue);
            OnPlayerCountChanged?.Invoke(oldValue, newValue);
        }
    }
}
