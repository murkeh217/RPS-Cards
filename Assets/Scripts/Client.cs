using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Client : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> OpponentScore = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> CurrentRound = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> DrawnCard = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> RevealedCard = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> GamePhase = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> HasPlayed = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> RoundResult = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> MatchOver = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> MatchWinner = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsSuddenDeath = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Panels")]
    public GameObject UI;
    public GameObject NameCollector;
    public GameObject WaitingPanel;
    public GameObject DrawPanel;
    public GameObject PlayPanel;
    public GameObject RevealPanel;
    public GameObject ResultPanel;
    public GameObject MatchOverPanel;

    [Header("Name Entry")]
    public TMP_InputField NameField;
    public Button okButton;

    [Header("HUD")]
    public TMP_Text ownNameText;
    public TMP_Text otherPlayerNameText;
    public TMP_Text roundText;
    public TMP_Text scoreText;
    public TMP_Text opponentScoreText;

<<<<<<< HEAD
    [Header("Draw Phase")]
    public Button drawCardButton;
    public TMP_Text drawPhaseText;

    [Header("Play Phase")]
    public Image drawnCardImage;
    public TMP_Text drawnCardName;
    public TMP_Text drawnCardFlavor;
    public Button playCardButton;

    [Header("Reveal Phase")]
    public Image ownRevealImage;
    public Image opponentRevealImage;
    public TMP_Text ownRevealName;
    public TMP_Text opponentRevealName;
    public TMP_Text revealFlavorText;
    public TMP_Text roundResultText;
    public Button continueButton;

    [Header("Match Over")]
    public TMP_Text matchResultText;
    public TMP_Text finalScoreText;
    public TMP_Text matchFlavorText;
    public Button rematchButton;

    [Header("Card Visuals")]
    public Sprite rockSprite;
    public Sprite paperSprite;
    public Sprite scissorsSprite;
    public Color rockColor = new Color(0.7f, 0.25f, 0.2f);
    public Color paperColor = new Color(0.85f, 0.9f, 1f);
    public Color scissorsColor = new Color(0.5f, 0.2f, 0.7f);

    private List<int> deck = new List<int>();
    private int lastPhase = -1;
    private bool gameStarted = false;
    private float revealTimer = 0f;
    private bool revealAnimating = false;

    private static readonly string[] RockNames = { "Heart", "Body", "Feelings", "Beliefs", "System", "Taste", "Growth", "Foundation", "Endurance", "Stability" };
    private static readonly string[] PaperNames = { "Mind", "Desires", "Sight", "Respect", "Values", "Smell", "Freedom", "Logic", "Imagination", "Perception" };
    private static readonly string[] ScissorsNames = { "Instinct", "Illusion", "Hearing", "Zone", "Setting", "Control", "Reflex", "Shadow", "Whisper", "Edge" };

    private static readonly string[] RockFlavors = {
        "Solid as the truth you carry.",
        "The body remembers what the mind forgets.",
        "Growth begins where comfort ends.",
        "A foundation does not ask to be seen.",
        "Feelings are the oldest language.",
        "Belief is the weight that holds you still.",
        "Taste the world as it is, not as you wish.",
        "The heart beats without permission.",
        "Endurance is silent courage.",
        "Stability is not stillness — it is balance."
    };

    private static readonly string[] PaperFlavors = {
        "Thought unfolds like light through glass.",
        "Desire is the compass of the restless mind.",
        "What you see depends on where you stand.",
        "Respect is the bridge between two truths.",
        "Values are invisible walls that shape your path.",
        "Freedom is not the absence of chains.",
        "Logic is a lantern, not the sun.",
        "Imagination paints what eyes cannot.",
        "Perception is reality's mirror — cracked and beautiful.",
        "The mind wanders so the soul can find."
    };

    private static readonly string[] ScissorsFlavors = {
        "Instinct cuts before thought arrives.",
        "Illusion is the shadow of desire.",
        "Listen — the silence speaks louder.",
        "The zone between fear and action is where you live.",
        "Control is the sharpest illusion of all.",
        "A reflex is a prayer your body makes.",
        "Shadows only exist because of light.",
        "A whisper can shatter certainty.",
        "The edge is where transformation begins.",
        "What haunts you is what you haven't faced."
    };

    private static readonly string[] WinReflections = {
        "Clarity prevails — for now.",
        "Your inner realm speaks true.",
        "The unseen hand favors the aware.",
        "Victory is a question, not an answer."
    };

    private static readonly string[] LoseReflections = {
        "Defeat is the teacher that never lies.",
        "What was hidden has revealed itself.",
        "Loss is the seed of understanding.",
        "Acceptance opens doors that force cannot."
    };

    private static readonly string[] DrawReflections = {
        "Mirror meets mirror — nothing is resolved.",
        "Two truths collide and neither yields.",
        "Balance is not victory, but it is not defeat.",
        "The universe pauses, holding its breath."
    };

    private static readonly string[] MatchWinTexts = {
        "Your inner realms aligned.\nBut winning was never the point.",
        "You understood the patterns.\nOr perhaps the patterns understood you.",
        "Victory echoes — but silence follows.",
        "The game ends. The questions remain."
    };

    private static readonly string[] MatchLoseTexts = {
        "The realms shifted against you.\nBut you showed up. That matters.",
        "Defeat is not failure.\nIt is the universe asking you to look deeper.",
        "You lost the match.\nBut what did you learn?",
        "The cards fell as they must.\nAcceptance is its own kind of strength."
    };

    private static readonly string[] MatchDrawTexts = {
        "Neither prevailed.\nPerhaps that is the most honest outcome.",
        "A tie — the universe could not decide.\nCan you?",
        "Equal in all things.\nThe mirror reflects both ways.",
        "No winner. No loser.\nJust two souls in the same storm."
    };

    private string currentCardName = "";
    private string currentCardFlavor = "";

    public override void OnNetworkSpawn()
=======
    bool isConnected = false;

    // Server-side storage for choices (only used on host/server)
    private static Dictionary<ulong, int> choices = new Dictionary<ulong, int>();
    private static bool isProcessingRound = false;

    private static GameObject choiceButtonsContainer;
    private static GameObject waitingTextObj;
    private GameObject scoreDisplayObj;

    private void Start()
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
    {
        if (!IsOwner)
        {
            if (UI != null) UI.SetActive(false);
            return;
        }

        UI.SetActive(true);
        HideAllPanels();
        NameCollector.SetActive(true);

        okButton.onClick.AddListener(GetOwnName);
        drawCardButton.onClick.AddListener(OnDrawCardPressed);
        playCardButton.onClick.AddListener(OnPlayCardPressed);
        continueButton.onClick.AddListener(OnContinuePressed);
        rematchButton.onClick.AddListener(OnRematchPressed);

        GamePhase.OnValueChanged += OnGamePhaseChanged;
        DrawnCard.OnValueChanged += OnDrawnCardChanged;
        Score.OnValueChanged += OnScoreChanged;
        OpponentScore.OnValueChanged += OnOpponentScoreChanged;
        CurrentRound.OnValueChanged += OnRoundChanged;
        RoundResult.OnValueChanged += OnRoundResultChanged;
        MatchOver.OnValueChanged += OnMatchOverChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        GamePhase.OnValueChanged -= OnGamePhaseChanged;
        DrawnCard.OnValueChanged -= OnDrawnCardChanged;
        Score.OnValueChanged -= OnScoreChanged;
        OpponentScore.OnValueChanged -= OnOpponentScoreChanged;
        CurrentRound.OnValueChanged -= OnRoundChanged;
        RoundResult.OnValueChanged -= OnRoundResultChanged;
        MatchOver.OnValueChanged -= OnMatchOverChanged;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (gameStarted)
        {
            UpdateOtherPlayerName();
        }

        if (revealAnimating)
        {
            revealTimer -= Time.deltaTime;
            if (revealTimer <= 0f)
            {
                revealAnimating = false;
                ShowRevealResult();
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

    void HideAllPanels()
    {
<<<<<<< HEAD
        if (NameCollector != null) NameCollector.SetActive(false);
        if (WaitingPanel != null) WaitingPanel.SetActive(false);
        if (DrawPanel != null) DrawPanel.SetActive(false);
        if (PlayPanel != null) PlayPanel.SetActive(false);
        if (RevealPanel != null) RevealPanel.SetActive(false);
        if (ResultPanel != null) ResultPanel.SetActive(false);
        if (MatchOverPanel != null) MatchOverPanel.SetActive(false);
=======
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

            // Both players submitted â€” extract choices
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
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
    }

    void GetOwnName()
    {
        if (NameField.text.Length > 0 && NameField.text.Length < 32)
        {
            playerName.Value = NameField.text;
            ownNameText.text = NameField.text;
            NameCollector.SetActive(false);
<<<<<<< HEAD
            gameStarted = true;
            HideAllPanels();
            WaitingPanel.SetActive(true);
            NotifyReadyServerRpc();
        }
    }

    void UpdateOtherPlayerName()
    {
        string other = GetOtherPlayerName();
        if (other != null && other.Length > 0)
        {
            otherPlayerNameText.text = other;
        }
        else
        {
            otherPlayerNameText.text = "Waiting...";
=======
            CreateChoiceButtons();
            StartGame();
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
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
<<<<<<< HEAD
            if (clientId == localId) continue;

=======
            if (clientId == localId)
                continue;
                
            // if any of the other connections is a valid player,
            // return their name and exit
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
            NetworkObject playerObj = kvp.Value.PlayerObject;
            if (playerObj == null) continue;

            if (playerObj.TryGetComponent<Client>(out var playerData))
            {
<<<<<<< HEAD
                string n = playerData.playerName.Value.ToString();
                if (n.Length > 0) return n;
=======
                if (playerData.name.Value.Length > 0)
                {
                    return playerData.name.Value.ToString();
                }
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
            }
        }

        return null;
    }
<<<<<<< HEAD

    Client GetOtherClient()
    {
        if (!NetworkManager.Singleton.IsConnectedClient) return null;

        ulong localId = NetworkManager.Singleton.LocalClientId;

        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            if (kvp.Key == localId) continue;

            NetworkObject playerObj = kvp.Value.PlayerObject;
            if (playerObj == null) return null;

            if (playerObj.TryGetComponent<Client>(out var other))
                return other;
        }

        return null;
    }

    void OnGamePhaseChanged(int oldVal, int newVal)
    {
        if (!IsOwner) return;
        RefreshUI(newVal);
    }

    void OnDrawnCardChanged(int oldVal, int newVal)
    {
        if (!IsOwner) return;
        if (newVal >= 0)
        {
            SetupCardVisual(newVal);
        }
    }

    void OnScoreChanged(int oldVal, int newVal)
    {
        if (!IsOwner) return;
        scoreText.text = "Score:\n" + newVal;
    }

    void OnOpponentScoreChanged(int oldVal, int newVal)
    {
        if (!IsOwner) return;
        opponentScoreText.text = "Opponent:\n" + newVal;
    }

    void OnRoundChanged(int oldVal, int newVal)
    {
        if (!IsOwner) return;
        string prefix = IsSuddenDeath.Value ? "Sudden Death" : ("Round:\n" + newVal + " / 7");
        roundText.text = prefix;
    }

    void OnRoundResultChanged(int oldVal, int newVal)
    {
        if (!IsOwner) return;
    }

    void OnMatchOverChanged(bool oldVal, bool newVal)
    {
        if (!IsOwner) return;
        if (newVal)
        {
            ShowMatchOver();
        }
    }

    void RefreshUI(int phase)
    {
        HideAllPanels();

        switch (phase)
        {
            case 0:
                WaitingPanel.SetActive(true);
                break;
            case 1:
                DrawPanel.SetActive(true);
                drawPhaseText.text = IsSuddenDeath.Value ? "Sudden Death!\nDraw your card." : "Round " + CurrentRound.Value + "\nDraw your card.";
                break;
            case 2:
                PlayPanel.SetActive(true);
                break;
            case 3:
                RevealPanel.SetActive(true);
                StartRevealAnimation();
                break;
            case 4:
                ResultPanel.SetActive(true);
                break;
            case 5:
                MatchOverPanel.SetActive(true);
                ShowMatchOver();
                break;
        }
    }

    void SetupCardVisual(int cardType)
    {
        Sprite s = GetCardSprite(cardType);
        string cardName = "";
        string cardFlavor = "";

        switch (cardType)
        {
            case 0:
                cardName = RockNames[Random.Range(0, RockNames.Length)];
                cardFlavor = RockFlavors[Random.Range(0, RockFlavors.Length)];
                break;
            case 1:
                cardName = PaperNames[Random.Range(0, PaperNames.Length)];
                cardFlavor = PaperFlavors[Random.Range(0, PaperFlavors.Length)];
                break;
            case 2:
                cardName = ScissorsNames[Random.Range(0, ScissorsNames.Length)];
                cardFlavor = ScissorsFlavors[Random.Range(0, ScissorsFlavors.Length)];
                break;
        }

        currentCardName = cardName;
        currentCardFlavor = cardFlavor;

        if (drawnCardImage != null) drawnCardImage.sprite = s;
        if (drawnCardImage != null) drawnCardImage.color = GetCardColor(cardType);
        if (drawnCardName != null) drawnCardName.text = cardName;
        if (drawnCardFlavor != null) drawnCardFlavor.text = cardFlavor;
    }

    Sprite GetCardSprite(int cardType)
    {
        switch (cardType)
        {
            case 0: return rockSprite;
            case 1: return paperSprite;
            case 2: return scissorsSprite;
            default: return null;
        }
    }

    Color GetCardColor(int cardType)
    {
        switch (cardType)
        {
            case 0: return rockColor;
            case 1: return paperColor;
            case 2: return scissorsColor;
            default: return Color.white;
        }
    }

    string GetCardTypeName(int cardType)
    {
        switch (cardType)
        {
            case 0: return "Rock";
            case 1: return "Paper";
            case 2: return "Scissors";
            default: return "?";
        }
    }

    void StartRevealAnimation()
    {
        if (ownRevealImage != null)
        {
            ownRevealImage.sprite = GetCardSprite(RevealedCard.Value);
            ownRevealImage.color = GetCardColor(RevealedCard.Value);
        }
        if (ownRevealName != null)
            ownRevealName.text = GetCardTypeName(RevealedCard.Value);

        if (opponentRevealImage != null)
        {
            opponentRevealImage.color = Color.gray;
            opponentRevealImage.sprite = null;
        }
        if (opponentRevealName != null)
            opponentRevealName.text = "?";
        if (revealFlavorText != null)
            revealFlavorText.text = "";
        if (roundResultText != null)
            roundResultText.text = "";

        revealTimer = 1.5f;
        revealAnimating = true;
    }

    void ShowRevealResult()
    {
        Client other = GetOtherClient();
        int opponentCard = -1;
        if (other != null)
        {
            opponentCard = other.RevealedCard.Value;
        }

        if (opponentCard >= 0)
        {
            if (opponentRevealImage != null)
            {
                opponentRevealImage.sprite = GetCardSprite(opponentCard);
                opponentRevealImage.color = GetCardColor(opponentCard);
            }
            if (opponentRevealName != null)
                opponentRevealName.text = GetCardTypeName(opponentCard);
        }

        int result = RoundResult.Value;
        switch (result)
        {
            case 1:
                if (roundResultText != null) roundResultText.text = "You Win!";
                if (revealFlavorText != null) revealFlavorText.text = WinReflections[Random.Range(0, WinReflections.Length)];
                break;
            case -1:
                if (roundResultText != null) roundResultText.text = "You Lose";
                if (revealFlavorText != null) revealFlavorText.text = LoseReflections[Random.Range(0, LoseReflections.Length)];
                break;
            case 0:
                if (roundResultText != null) roundResultText.text = "Draw";
                if (revealFlavorText != null) revealFlavorText.text = DrawReflections[Random.Range(0, DrawReflections.Length)];
                break;
        }

        if (continueButton != null) continueButton.gameObject.SetActive(true);
    }

    void ShowMatchOver()
    {
        HideAllPanels();
        MatchOverPanel.SetActive(true);

        int winner = MatchWinner.Value;
        int myScore = Score.Value;

        Client other = GetOtherClient();
        int theirScore = other != null ? other.Score.Value : 0;

        if (finalScoreText != null)
            finalScoreText.text = myScore + " - " + theirScore;

        if (winner == (int)OwnerClientId)
        {
            if (matchResultText != null) matchResultText.text = "Victory";
            if (matchFlavorText != null) matchFlavorText.text = MatchWinTexts[Random.Range(0, MatchWinTexts.Length)];
        }
        else if (winner == -1)
        {
            if (matchResultText != null) matchResultText.text = "Draw";
            if (matchFlavorText != null) matchFlavorText.text = MatchDrawTexts[Random.Range(0, MatchDrawTexts.Length)];
        }
        else
        {
            if (matchResultText != null) matchResultText.text = "Defeat";
            if (matchFlavorText != null) matchFlavorText.text = MatchLoseTexts[Random.Range(0, MatchLoseTexts.Length)];
        }
    }

    void OnDrawCardPressed()
    {
        RequestDrawCardServerRpc();
    }

    void OnPlayCardPressed()
    {
        PlayCardServerRpc();
    }

    void OnContinuePressed()
    {
        ReadyForNextRoundServerRpc();
    }

    void OnRematchPressed()
    {
        RequestRematchServerRpc();
    }

    [ServerRpc]
    void NotifyReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        GameServer.Instance.PlayerReady(senderId);
    }

    [ServerRpc]
    void RequestDrawCardServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        GameServer.Instance.PlayerDrawCard(senderId);
    }

    [ServerRpc]
    void PlayCardServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        GameServer.Instance.PlayerPlayCard(senderId);
    }

    [ServerRpc]
    void ReadyForNextRoundServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        GameServer.Instance.PlayerReadyForNext(senderId);
    }

    [ServerRpc]
    void RequestRematchServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        GameServer.Instance.PlayerRequestRematch(senderId);
    }
}
=======
}
>>>>>>> 6ad249589dbde9747ad272814bf4865701035a59
