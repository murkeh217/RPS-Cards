using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class ConnectionHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private void OnEnable()
    {
        StartCoroutine(WaitForNetworkManagerAndSubscribe());
    }

    private IEnumerator WaitForNetworkManagerAndSubscribe()
    {
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

        var net = NetworkManager.Singleton;

        net.OnClientConnectedCallback += HandleClientConnected;
        net.OnClientDisconnectCallback += HandleClientDisconnected;
        net.OnServerStarted += HandleServerStarted;
    }

    private void OnDisable()
    {
        var net = NetworkManager.Singleton;
        if (net == null) return;

        net.OnClientConnectedCallback -= HandleClientConnected;
        net.OnClientDisconnectCallback -= HandleClientDisconnected;
        net.OnServerStarted -= HandleServerStarted;
    }

    private void HandleServerStarted()
    {
        Debug.Log("[Server] Server started");
        PrintConnectionInfo();

        if (NetworkManager.Singleton.IsHost)
        {
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        var net = NetworkManager.Singleton;

        if (net.IsServer && clientId != net.LocalClientId)
        {
            Debug.Log("[Server] Remote client connected: " + clientId);
        }

        if (net.IsClient && !net.IsHost && clientId == net.LocalClientId)
        {
            Debug.Log("[Client] Connected to server with ID: " + clientId);

            if (gameManager != null)
            {
                gameManager.StartGame();
            }
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        var net = NetworkManager.Singleton;

        if (net.IsServer)
        {
            Debug.Log("[Server] Client disconnected: " + clientId);
        }

        if (net.IsClient && !net.IsHost && clientId == net.LocalClientId)
        {
            Debug.Log("[Client] Disconnected from server");

            if (gameManager != null)
            {
                gameManager.ShowDisconnected();
            }
        }
    }

    private void PrintConnectionInfo()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null) return;

        ushort port = transport.ConnectionData.Port;
        string listenAddress = transport.ConnectionData.ServerListenAddress;

        Debug.Log("[Host/Server] Listening on port: " + port + " (listen: " + listenAddress + ")");

        var localIps = GetLocalIPv4Addresses();
        if (localIps.Count > 0)
        {
            foreach (var ip in localIps)
            {
                Debug.Log("  -> " + ip + ":" + port);
            }
        }
        else
        {
            Debug.Log("  (No local IPv4 addresses found)");
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
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    string ipStr = ip.ToString();
                    if (ipStr != "127.0.0.1")
                    {
                        addresses.Add(ipStr);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[ConnectionHandler] " + ex.Message);
        }

        return addresses;
    }
<<<<<<< HEAD
}
=======

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
        var net = NetworkManager.Singleton;
        if (net.IsHost && gameManager != null)
        {
            gameManager.StartGame();
        }
    }

    private void OnApplicationQuit()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
