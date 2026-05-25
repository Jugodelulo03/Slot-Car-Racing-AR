using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace SlotCarRacingAR.Runtime.Infrastructure
{
    /// <summary>
    /// Cleans up any zombie NetworkManager left from a previous Play session.
    /// Runs before any scene loads to ensure the transport port is free.
    /// </summary>
    public static class NetworkCleanup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Cleanup()
        {
            if (NetworkManager.Singleton != null)
            {
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.Shutdown();
                }

                if (NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                }

                Object.DestroyImmediate(NetworkManager.Singleton.gameObject);
                UnityEngine.Debug.Log("[NetworkCleanup] Destroyed zombie NetworkManager from previous session.");
            }
        }
    }
}
