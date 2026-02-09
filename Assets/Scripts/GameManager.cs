using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Config")]
    public ushort defaultPort = 7777;          // fallback / default

    [Header("UI")]
    public GameObject ConnectPanel;
    public GameObject LoadPanel;
    public GameObject GamePanel;

    public Button Join;
    public TMP_InputField JoinIP;
    public TMP_InputField JoinPort;            // optional – you can use this too

    public Button Host;
    public TMP_InputField HostPort;            // optional – for custom host port

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

        // Optional: enforce default port if fields are empty
        if (unityTransport != null && unityTransport.ConnectionData.Port == 0)
        {
            unityTransport.ConnectionData.Port = defaultPort;
        }
    }

    public void StartHost()
    {
        ushort hostPort = defaultPort;

        // Optional: let user override port for host
        if (!string.IsNullOrEmpty(HostPort.text))
        {
            if (ushort.TryParse(HostPort.text, out ushort parsed))
            {
                hostPort = parsed;
            }
            else
            {
                MessageBox("Invalid host port – using default " + defaultPort);
            }
        }

        // For HOST/SERVER: 
        // - Address is mostly ignored (but can be set to 0.0.0.0 or local IP)
        // - Port = listen port
        // - listenAddress = "0.0.0.0" (critical – listen on all interfaces)
        unityTransport.SetConnectionData("0.0.0.0", hostPort, "0.0.0.0");

        AttemptStartGame();
        LoadPanel.GetComponentInChildren<TMP_Text>().text = "Hosting...";

        if (NetworkManager.Singleton.StartHost())
        {
            // Success – you can log or update UI here
            Debug.Log($"Host started on port {hostPort}");
        }
        else
        {
            MessageBox("Failed to start host");
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

        // Optional: custom port for client connect
        if (!string.IsNullOrEmpty(JoinPort.text))
        {
            if (ushort.TryParse(JoinPort.text, out ushort parsed))
            {
                clientPort = parsed;
            }
            else
            {
                MessageBox("Invalid join port – using default");
            }
        }

        // For CLIENT: set target server IP + port
        // listenAddress usually left null/default for clients
        unityTransport.SetConnectionData(JoinIP.text, clientPort);  // or (JoinIP.text, clientPort, null)

        AttemptStartGame();
        LoadPanel.GetComponentInChildren<TMP_Text>().text = "Connecting...";

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log($"Client connecting to {JoinIP.text}:{clientPort}");
        }
        else
        {
            MessageBox("Failed to start client");
        }
    }

    public void StartGame()
    {
        ConnectPanel.SetActive(false);
        LoadPanel.SetActive(false);
        GamePanel.SetActive(true);

        Round.text = "Round:\n0";
        Score.text = "Score:\n0";
        
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