using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Config")]
    public ushort defaultPort = 7777;

    [Header("UI")]
    public GameObject ConnectPanel;
    public GameObject LoadPanel;
    public GameObject GamePanel;

    public Button Join;
    public TMP_InputField JoinIP;
    public TMP_InputField JoinPort;

    public Button Host;
    public TMP_InputField HostPort;

    public TMP_Text Round;
    public TMP_Text Score;

    [Header("Other")]
    public GameObject messageBox;

    private UnityTransport unityTransport;

    void Start()
    {
        Join.onClick.AddListener(StartClient);
        Host.onClick.AddListener(StartHost);

        unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (unityTransport != null && unityTransport.ConnectionData.Port == 0)
        {
            unityTransport.ConnectionData.Port = defaultPort;
        }
    }

    public void StartHost()
    {
        ushort hostPort = defaultPort;

        if (!string.IsNullOrEmpty(HostPort.text))
        {
            if (ushort.TryParse(HostPort.text, out ushort parsed))
            {
                hostPort = parsed;
            }
            else
            {
                MessageBox("Invalid host port - using default " + defaultPort);
            }
        }

        unityTransport.SetConnectionData("0.0.0.0", hostPort, "0.0.0.0");

        AttemptStartGame();
        LoadPanel.GetComponentInChildren<TMP_Text>().text = "Hosting...";

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host started on port " + hostPort);
        }
        else
        {
            MessageBox("Failed to start host");
            ConnectPanel.SetActive(true);
            LoadPanel.SetActive(false);
        }
    }

    public void StartClient()
    {
        if (string.IsNullOrEmpty(JoinIP.text))
        {
            MessageBox("Join IP required.");
            return;
        }

        ushort clientPort = defaultPort;

        if (!string.IsNullOrEmpty(JoinPort.text))
        {
            if (ushort.TryParse(JoinPort.text, out ushort parsed))
            {
                clientPort = parsed;
            }
            else
            {
                MessageBox("Invalid join port - using default");
            }
        }

        unityTransport.SetConnectionData(JoinIP.text, clientPort);

        AttemptStartGame();
        LoadPanel.GetComponentInChildren<TMP_Text>().text = "Connecting...";

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Client connecting to " + JoinIP.text + ":" + clientPort);
        }
        else
        {
            MessageBox("Failed to start client");
            ConnectPanel.SetActive(true);
            LoadPanel.SetActive(false);
        }
    }

    public void StartGame()
    {
        ConnectPanel.SetActive(false);
        LoadPanel.SetActive(false);
        GamePanel.SetActive(true);
    }

    public void ShowDisconnected()
    {
        GamePanel.SetActive(false);
        LoadPanel.SetActive(false);
        ConnectPanel.SetActive(true);
        MessageBox("Disconnected from server");
    }

    void AttemptStartGame()
    {
        ConnectPanel.SetActive(false);
        LoadPanel.SetActive(true);
    }

    void MessageBox(string message)
    {
        if (messageBox != null)
        {
            messageBox.SetActive(true);
            var txt = messageBox.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = message;
        }
        else
        {
            Debug.LogWarning("Message: " + message);
        }
    }
}
