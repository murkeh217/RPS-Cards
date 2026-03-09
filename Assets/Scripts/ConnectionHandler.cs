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
}