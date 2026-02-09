using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Client : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> name = new NetworkVariable<FixedString32Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public GameObject UI;
    public GameObject NameCollector;

    public TMP_InputField NameField;
    public Button ok;

    public TMP_Text otherPlayerName;

    bool isConnected = false;
    private void Start()
    {
        if (!IsOwner) return;
        UI.SetActive(true);

        ok.onClick.AddListener(GetOwnName);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (otherPlayerName.text.Length > 0 && isConnected)
        {
            if (GetOtherPlayerName() != null)
            {
                otherPlayerName.text = GetOtherPlayerName();
            }
        }
    }

    void StartGame()
    {
        isConnected = true;
    }

    [ServerRpc]
    void SendTurnServerRpc()
    {

    }


    [ClientRpc]
    void ReceiveTurnClientRpc()
    {

    }

    void GetOwnName()
    {
        if (NameField.text.Length > 0 && NameField.text.Length < 32)
        {
            name.Value = NameField.text;
            NameCollector.SetActive(false);
            StartGame();
        }
    }

    public string GetOtherPlayerName()
    {
        if (!NetworkManager.Singleton.IsConnectedClient) return null;

        ulong localId = NetworkManager.Singleton.LocalClientId;

        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = kvp.Key;
            if (clientId == localId)
                continue;

            NetworkObject playerObj = kvp.Value.PlayerObject;
            if (playerObj == null) return null;

            if (playerObj.TryGetComponent<Client>(out var playerData))
            {
                return playerData.name.Value.ToString();
            }
        }

        return null;
    }
}