using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
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


    void Start()
    {
        Join.onClick.AddListener(StartClient); // button listeners
        Host.onClick.AddListener(StartHost);
    }

    void Update()
    {
        
    }

    void StartGame()
    {
        Round.text = "Round:\n0";
        Score.text = "Score:\n0";
    }

    void AttemptStartGame()
    {
        ConnectPanel.SetActive(false);
        LoadPanel.SetActive(true);
        LoadPanel.GetComponentInChildren<TMP_Text>().text = "Connecting...";
    }
    
    public void StartHost()
    {

        if (string.IsNullOrEmpty(HostPort.text))
        {
            MessageBox("Host Port Empty. Unable to Host.");
            return;
        }

        NetworkManager.Singleton.StartServer();

        AttemptStartGame();
    }

    public void StartClient()
    {
        if (string.IsNullOrEmpty(JoinIP.text) || string.IsNullOrEmpty(JoinPort.text))
        {
            MessageBox("Join IP or Port Empty. Unable to Join.");
            return;
        }

        NetworkManager.Singleton.StartClient();
    }

    void MessageBox(string message)
    {
        messageBox.SetActive(true);
        messageBox.GetComponentInChildren<TMP_Text>().text = message;
    }
}
