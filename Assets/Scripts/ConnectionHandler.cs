using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class ConnectionHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;  // assign in Inspector

    private void OnEnable()
    {
        StartCoroutine(WaitForNetworkManagerAndSubscribe());
    }

    private IEnumerator WaitForNetworkManagerAndSubscribe()
    {
        // Wait until NetworkManager is fully initialized
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

        var net = NetworkManager.Singleton;

        // Subscribe to events
        net.OnClientConnectedCallback += HandleClientConnected;
        net.OnServerStarted += HandleServerStarted;

        // Print IP/port information immediately after NetworkManager is ready
        PrintConnectionInfo();
    }

    private void PrintConnectionInfo()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogWarning("[ConnectionHandler] No UnityTransport component found on NetworkManager!");
            return;
        }

        var net = NetworkManager.Singleton;

        if (net.IsServer || net.IsHost)
        {
            ushort port = transport.ConnectionData.Port;
            string listenAddress = transport.ConnectionData.ServerListenAddress; // usually "0.0.0.0"

            Debug.Log($"[{(net.IsHost ? "Host" : "Server")}] Listening on port: {port} " +
                      $"(ServerListenAddress: {listenAddress})");

            // Show local IPv4 addresses clients can use to connect (LAN)
            var localIps = GetLocalIPv4Addresses();
            Debug.Log($"[Server/Host] Local IPv4 addresses to share:");
            foreach (var ip in localIps)
            {
                Debug.Log($"  → {ip}:{port}");
            }
            if (localIps.Count == 0)
            {
                Debug.Log("  (No local IPv4 addresses detected – check network adapters)");
            }
        }

        if (net.IsClient && !net.IsHost)
        {
            string targetIp = transport.ConnectionData.Address;
            ushort targetPort = transport.ConnectionData.Port;
            Debug.Log($"[Client] Attempting connection to: {targetIp}:{targetPort}");
        }
    }

    private List<string> GetLocalIPv4Addresses()
    {
        var addresses = new List<string>();

        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
                {
                    string ipStr = ip.ToString();
                    if (ipStr != "127.0.0.1") // skip loopback by default
                    {
                        addresses.Add(ipStr);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ConnectionHandler] Failed to get local IPs: {ex.Message}");
        }

        return addresses;
    }

    private void OnDisable()
    {
        var net = NetworkManager.Singleton;
        if (net == null) return;

        net.OnClientConnectedCallback -= HandleClientConnected;
        net.OnServerStarted -= HandleServerStarted;
    }

    private void HandleClientConnected(ulong clientId)
    {
        var net = NetworkManager.Singleton;

        if (net.IsServer)
        {
            Debug.Log($"[Server] New client connected: ClientID = {clientId}");

            // Optional: skip host itself (clientId == 0 usually)
            if (clientId != net.LocalClientId)
            {
                // → Here you typically spawn player prefab for this remote client
                // NetworkObject player = Instantiate(playerPrefab);
                // player.SpawnAsPlayerObject(clientId);
            }
        }

        if (net.IsClient && clientId == net.LocalClientId)
        {
            Debug.Log($"[Client] Successfully connected (my ClientID: {clientId})");

            // Only call StartGame on pure remote clients (not host)
            if (!net.IsHost)
            {
                if (gameManager != null)
                {
                    gameManager.StartGame();
                }
                else
                {
                    Debug.LogWarning("[Client] GameManager reference is null – cannot call StartGame()");
                }
            }
        }
    }

    private void HandleServerStarted()
    {
        Debug.Log("[Server] Server fully started and ready for connections");
        // → Load networked scene, spawn world objects, initialize match state, etc.
    }
}