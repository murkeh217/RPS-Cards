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
    public ushort defaultPort = 7777;

    [Header("UI")]
    public GameObject ConnectPanel;
    public GameObject LoadPanel;
    public GameObject GamePanel;

    public Button Join;
    public TMP_InputField JoinIP;
<<<<<<< HEAD
    public TMP_InputField JoinPort;

    public Button Host;
    public TMP_InputField HostPort;
=======
    public TMP_InputField JoinPort;            // optional � you can use this too

    public Button Host;
    public TMP_InputField HostPort;            // optional � for custom host port
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59

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
<<<<<<< HEAD
                MessageBox("Invalid host port - using default " + defaultPort);
            }
        }

=======
                MessageBox("Invalid host port � using default " + defaultPort);
            }
        }

        // For HOST/SERVER: 
        // - Address is mostly ignored (but can be set to 0.0.0.0 or local IP)
        // - Port = listen port
        // - listenAddress = "0.0.0.0" (critical � listen on all interfaces)
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
        unityTransport.SetConnectionData("0.0.0.0", hostPort, "0.0.0.0");

        AttemptStartGame();

        if (NetworkManager.Singleton.StartHost())
        {
<<<<<<< HEAD
            Debug.Log("Host started on port " + hostPort);
=======
            string localIP = GetLocalIPv4();
            string ipDisplay = $"{localIP}:{hostPort}";
            ShowIPOverlay(ipDisplay, isHost: true);
            Debug.Log($"Host started - Your IP: {ipDisplay}");
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
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
<<<<<<< HEAD
                MessageBox("Invalid join port - using default");
=======
                MessageBox("Invalid join port � using default");
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
            }
        }

        unityTransport.SetConnectionData(JoinIP.text, clientPort);

        AttemptStartGame();
        LoadPanel.GetComponentInChildren<TMP_Text>().text = "Connecting...";

        if (NetworkManager.Singleton.StartClient())
        {
<<<<<<< HEAD
            Debug.Log("Client connecting to " + JoinIP.text + ":" + clientPort);
=======
            ShowIPOverlay($"{JoinIP.text}:{clientPort}", isHost: false);
            Debug.Log($"Client connecting to {JoinIP.text}:{clientPort}");
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
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
<<<<<<< HEAD
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
=======
        if (ConnectPanel != null) ConnectPanel.SetActive(false);
        if (LoadPanel != null) LoadPanel.SetActive(false);
        if (GamePanel != null) GamePanel.SetActive(true);

        if (Round != null) Round.text = "Round:\n0";
        if (Score != null) Score.text = "Score:\n0";
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
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