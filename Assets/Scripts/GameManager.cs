using System.Net;
using System.Net.Sockets;
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
    public TMP_InputField JoinPort;            // optional � you can use this too

    public Button Host;
    public TMP_InputField HostPort;            // optional � for custom host port

    public TMP_Text Round;
    public TMP_Text Score;

    [Header("Other")]
    public GameObject messageBox;

    private UnityTransport unityTransport;
    private TextMeshProUGUI ipOverlayText;

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
                MessageBox("Invalid host port � using default " + defaultPort);
            }
        }

        // For HOST/SERVER: 
        // - Address is mostly ignored (but can be set to 0.0.0.0 or local IP)
        // - Port = listen port
        // - listenAddress = "0.0.0.0" (critical � listen on all interfaces)
        unityTransport.SetConnectionData("0.0.0.0", hostPort, "0.0.0.0");

        AttemptStartGame();

        if (NetworkManager.Singleton.StartHost())
        {
            string localIP = GetLocalIPv4();
            string ipDisplay = $"{localIP}:{hostPort}";
            ShowIPOverlay(ipDisplay, isHost: true);
            Debug.Log($"Host started - Your IP: {ipDisplay}");
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
                MessageBox("Invalid join port � using default");
            }
        }

        // For CLIENT: set target server IP + port
        // listenAddress usually left null/default for clients
        unityTransport.SetConnectionData(JoinIP.text, clientPort);  // or (JoinIP.text, clientPort, null)

        AttemptStartGame();
        LoadPanel.GetComponentInChildren<TMP_Text>().text = "Connecting...";

        if (NetworkManager.Singleton.StartClient())
        {
            ShowIPOverlay($"{JoinIP.text}:{clientPort}", isHost: false);
            Debug.Log($"Client connecting to {JoinIP.text}:{clientPort}");
        }
        else
        {
            MessageBox("Failed to start client");
        }
    }

    public void StartGame()
    {
        if (ConnectPanel != null) ConnectPanel.SetActive(false);
        if (LoadPanel != null) LoadPanel.SetActive(false);
        if (GamePanel != null) GamePanel.SetActive(true);

        if (Round != null) Round.text = "Round:\n0";
        if (Score != null) Score.text = "Score:\n0";
    }

    void AttemptStartGame()
    {
        if (ConnectPanel != null) ConnectPanel.SetActive(false);
        if (LoadPanel != null) LoadPanel.SetActive(true);
    }

    void ShowIPOverlay(string ipText, bool isHost)
    {
        if (ipOverlayText == null)
        {
            GameObject canvasObj = new GameObject("IPOverlayCanvas");
            DontDestroyOnLoad(canvasObj);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Dark background panel for contrast
            GameObject panelObj = new GameObject("IPPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image bg = panelObj.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0f, 0f, 0f, 0.65f);
            RectTransform panelRect = bg.rectTransform;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(0f, 54f);

            GameObject textObj = new GameObject("IPText");
            textObj.transform.SetParent(panelObj.transform, false);
            ipOverlayText = textObj.AddComponent<TextMeshProUGUI>();
            ipOverlayText.fontSize = 28;
            ipOverlayText.color = Color.white;
            ipOverlayText.alignment = TextAlignmentOptions.Center;
            ipOverlayText.fontStyle = FontStyles.Bold;

            RectTransform rect = ipOverlayText.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        string label = isHost ? "Hosting" : "Connected to";
        ipOverlayText.text = $"{label}:  {ipText}";
    }

    string GetLocalIPv4()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString() != "127.0.0.1")
                    return ip.ToString();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to get local IP: {ex.Message}");
        }
        return "unknown";
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