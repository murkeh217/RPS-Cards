using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    // Server-side storage for choices (only used on host/server)
    private static Dictionary<ulong, int> choices = new Dictionary<ulong, int>();
    private static bool isProcessingRound = false;

    private static GameObject choiceButtonsContainer;
    private static GameObject waitingTextObj;
    private GameObject scoreDisplayObj;

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

        UpdateScoreDisplay(Score.Value, GetOpponentScore());
    }

    int GetOpponentScore()
    {
        if (!NetworkManager.Singleton.IsConnectedClient) return 0;
        ulong localId = NetworkManager.Singleton.LocalClientId;
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            if (kvp.Key == localId) continue;
            if (kvp.Value.PlayerObject != null && kvp.Value.PlayerObject.TryGetComponent<Client>(out var playerData))
            {
                return playerData.Score.Value;
            }
        }
        return 0;
    }

    void UpdateScoreDisplay(int myScore, int oppScore)
    {
        if (scoreDisplayObj == null)
        {
            scoreDisplayObj = new GameObject("ScoreDisplay");
            scoreDisplayObj.transform.SetParent(UI.transform, false);
            
            TextMeshProUGUI tmp = scoreDisplayObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 50;
            tmp.color = Color.yellow;
            tmp.alignment = TextAlignmentOptions.TopRight;
            tmp.fontStyle = FontStyles.Bold;
            
            RectTransform rect = tmp.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-30f, -30f);
            rect.sizeDelta = new Vector2(400f, 150f);
        }
        
        TextMeshProUGUI label = scoreDisplayObj.GetComponent<TextMeshProUGUI>();
        label.text = $"Score: {myScore}\nOpponent: {oppScore}";
    }

    void StartGame()
    {
        isConnected = true;
    }

    // Call this from your UI when the local player picks a card (0-3)
    public void SubmitChoice(int choice)
    {
        SendTurnServerRpc(choice);
    }

    [ServerRpc]
    void SendTurnServerRpc(int choice, ServerRpcParams rpcParams = default)
    {
        if (isProcessingRound) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        choices[senderId] = choice;

        Debug.Log($"[Server] Received choice {choice} from client {senderId} ({choices.Count}/2)");

        if (choices.Count >= 2)
        {
            isProcessingRound = true;

            // Both players submitted — extract choices
            ulong idA = 0;
            int choiceA = 0;
            ulong idB = 0;
            int choiceB = 0;

            int count = 0;
            foreach (var kvp in choices)
            {
                if (count == 0)
                {
                    idA = kvp.Key;
                    choiceA = kvp.Value;
                }
                else if (count == 1)
                {
                    idB = kvp.Key;
                    choiceB = kvp.Value;
                }
                count++;
            }

            int winnerId = -1; // -1 = tie
            if (choiceA != choiceB)
            {
                if ((choiceA == 0 && choiceB == 2) || (choiceA == 1 && choiceB == 0) || (choiceA == 2 && choiceB == 1))
                    winnerId = 0;
                else
                    winnerId = 1;
            }

            if (winnerId == 0)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(idA, out var clientA) && clientA.PlayerObject.TryGetComponent<Client>(out var aComp))
                    aComp.Score.Value++;
            }
            else if (winnerId == 1)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(idB, out var clientB) && clientB.PlayerObject.TryGetComponent<Client>(out var bComp))
                    bComp.Score.Value++;
            }
            Debug.Log($"choices: { choiceA + choiceB}");
            // Broadcast both choices to all clients
            ReceiveTurnClientRpc(idA, choiceA, idB, choiceB);
            
            // Allow next round after a delay
            Invoke(nameof(UnlockNextRound), 3f);
        }
    }

    void UnlockNextRound()
    {
        choices.Clear();
        isProcessingRound = false;
    }

    [ClientRpc]
    void ReceiveTurnClientRpc(ulong idA, int choiceA, ulong idB, int choiceB)
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;

        int myChoice, opponentChoice;
        if (myId == idA)
        {
            myChoice = choiceA;
            opponentChoice = choiceB;
        }
        else
        {
            myChoice = choiceB;
            opponentChoice = choiceA;
        }

        if (waitingTextObj != null) waitingTextObj.SetActive(false);

        string[] choiceNames = { "Rock", "Paper", "Scissors", "Wild" };
        string myName = myChoice >= 0 && myChoice < choiceNames.Length ? choiceNames[myChoice] : myChoice.ToString();
        string oppName = opponentChoice >= 0 && opponentChoice < choiceNames.Length ? choiceNames[opponentChoice] : opponentChoice.ToString();

        string resMsg = "Tie!";
        if ((myChoice == 0 && opponentChoice == 2) || (myChoice == 1 && opponentChoice == 0) || (myChoice == 2 && opponentChoice == 1))
            resMsg = "You Win!";
        else if (myChoice != opponentChoice)
            resMsg = "You Lose!";

        string result = $"You: {myName} | Opponent: {oppName}\n{resMsg}";
        Debug.Log($"[Client] {result}");
        ShowRoundResultOverlay(result);

        Invoke(nameof(ResetRound), 3f);
    }

    void ResetRound()
    {
        if (roundOverlayObj != null) roundOverlayObj.SetActive(false);
        if (waitingTextObj != null) waitingTextObj.SetActive(false);
        if (choiceButtonsContainer != null) choiceButtonsContainer.SetActive(true);
    }

    private static GameObject roundOverlayObj;

    void ShowRoundResultOverlay(string text)
    {
        // Reuse or create the overlay canvas
        if (roundOverlayObj == null)
        {
            roundOverlayObj = new GameObject("RoundResultOverlay");
            Canvas canvas = roundOverlayObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            roundOverlayObj.AddComponent<CanvasScaler>();
            roundOverlayObj.AddComponent<GraphicRaycaster>();

            GameObject textObj = new GameObject("RoundResultText");
            textObj.transform.SetParent(roundOverlayObj.transform, false);

            TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 28;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;

            RectTransform rect = tmp.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -10f);
            rect.sizeDelta = new Vector2(0f, 50f);
        }

        roundOverlayObj.SetActive(true);

        // Update the text
        TMP_Text label = roundOverlayObj.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = text;
    }

    void GetOwnName()
    {
        if (NameField.text.Length > 0 && NameField.text.Length < 32)
        {
            name.Value = NameField.text;
            NameCollector.SetActive(false);
            CreateChoiceButtons();
            StartGame();
        }
    }

    void CreateChoiceButtons()
    {
        choiceButtonsContainer = new GameObject("ChoiceButtonsContainer");
        choiceButtonsContainer.transform.SetParent(UI.transform, false);
        
        RectTransform rect = choiceButtonsContainer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -50); // Slightly below center
        rect.sizeDelta = new Vector2(600, 150);

        HorizontalLayoutGroup layout = choiceButtonsContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        
        string[] labels = { "Rock", "Paper", "Scissors" };
        for (int i = 0; i < 3; i++)
        {
            int choiceIndex = i; 
            GameObject btnObj = new GameObject("ChoiceBtn_" + i);
            btnObj.transform.SetParent(choiceButtonsContainer.transform, false);
            
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 1f); // Visible blue color
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                choiceButtonsContainer.SetActive(false);
                ShowWaitingText();
                SubmitChoice(choiceIndex);
            });
            
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(160, 80);
            
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = labels[i];
            txt.color = Color.white;
            txt.fontSize = 28;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
        }
    }

    void ShowWaitingText()
    {
        if (waitingTextObj == null)
        {
            waitingTextObj = new GameObject("WaitingText");
            waitingTextObj.transform.SetParent(UI.transform, false);
            TextMeshProUGUI txt = waitingTextObj.AddComponent<TextMeshProUGUI>();
            txt.text = "Waiting for Opponent...";
            txt.fontSize = 32;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            
            RectTransform rect = txt.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -50);
            rect.sizeDelta = new Vector2(400, 100);
        }
        waitingTextObj.SetActive(true);
    }

    public string GetOtherPlayerName()
    {
        // Check connection
        if (!NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsServer) return null;
        
        // store local player's id
        ulong localId = NetworkManager.Singleton.LocalClientId;
        
        // loop through all possible players
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            // exclude current player from pool of players
            ulong clientId = kvp.Key;
            if (clientId == localId)
                continue;
                
            // if any of the other connections is a valid player,
            // return their name and exit
            NetworkObject playerObj = kvp.Value.PlayerObject;
            if (playerObj == null) continue;

            if (playerObj.TryGetComponent<Client>(out var playerData))
            {
                if (playerData.name.Value.Length > 0)
                {
                    return playerData.name.Value.ToString();
                }
            }
        }

        return null;
    }
}
