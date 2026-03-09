using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Client : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> Score = new NetworkVariable<int>(
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

    public NetworkVariable<ulong> MatchWinner = new NetworkVariable<ulong>(
        ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsSuddenDeath = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> OpponentRevealed = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> OpponentScore = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString32Bytes> OpponentName = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Panels")]
    public GameObject UI;
    public GameObject NameCollector;
    public GameObject WaitingPanel;
    public GameObject DrawPanel;
    public GameObject PlayPanel;
    public GameObject RevealPanel;
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

    private float revealTimer = 0f;
    private bool revealAnimating = false;
    private bool gameStarted = false;

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

    public override void OnNetworkSpawn()
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
        MatchOver.OnValueChanged += OnMatchOverChanged;
        OpponentName.OnValueChanged += OnOpponentNameChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        okButton.onClick.RemoveListener(GetOwnName);
        drawCardButton.onClick.RemoveListener(OnDrawCardPressed);
        playCardButton.onClick.RemoveListener(OnPlayCardPressed);
        continueButton.onClick.RemoveListener(OnContinuePressed);
        rematchButton.onClick.RemoveListener(OnRematchPressed);

        GamePhase.OnValueChanged -= OnGamePhaseChanged;
        DrawnCard.OnValueChanged -= OnDrawnCardChanged;
        Score.OnValueChanged -= OnScoreChanged;
        OpponentScore.OnValueChanged -= OnOpponentScoreChanged;
        CurrentRound.OnValueChanged -= OnRoundChanged;
        MatchOver.OnValueChanged -= OnMatchOverChanged;
        OpponentName.OnValueChanged -= OnOpponentNameChanged;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (revealAnimating)
        {
            revealTimer -= Time.deltaTime;
            if (revealTimer <= 0f)
            {
                revealAnimating = false;
                ShowRevealResult();
            }
        }
    }

    void HideAllPanels()
    {
        if (NameCollector != null) NameCollector.SetActive(false);
        if (WaitingPanel != null) WaitingPanel.SetActive(false);
        if (DrawPanel != null) DrawPanel.SetActive(false);
        if (PlayPanel != null) PlayPanel.SetActive(false);
        if (RevealPanel != null) RevealPanel.SetActive(false);
        if (MatchOverPanel != null) MatchOverPanel.SetActive(false);
    }

    void GetOwnName()
    {
        if (NameField.text.Length > 0 && NameField.text.Length < 32)
        {
            playerName.Value = NameField.text;
            if (ownNameText != null) ownNameText.text = NameField.text;
            gameStarted = true;
            HideAllPanels();
            WaitingPanel.SetActive(true);
            NotifyReadyServerRpc();
        }
    }

    void OnOpponentNameChanged(FixedString32Bytes oldVal, FixedString32Bytes newVal)
    {
        if (!IsOwner) return;
        if (otherPlayerNameText != null)
            otherPlayerNameText.text = newVal.ToString();
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
        if (scoreText != null) scoreText.text = "You:\n" + newVal;
    }

    void OnOpponentScoreChanged(int oldVal, int newVal)
    {
        if (!IsOwner) return;
        if (opponentScoreText != null) opponentScoreText.text = "Them:\n" + newVal;
    }

    void OnRoundChanged(int oldVal, int newVal)
    {
        if (!IsOwner) return;
        if (roundText != null)
        {
            if (IsSuddenDeath.Value)
                roundText.text = "Sudden Death";
            else
                roundText.text = "Round\n" + newVal + " / 7";
        }
    }

    void OnMatchOverChanged(bool oldVal, bool newVal)
    {
        if (!IsOwner) return;
        if (newVal) ShowMatchOver();
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
                if (drawPhaseText != null)
                    drawPhaseText.text = IsSuddenDeath.Value
                        ? "Sudden Death!\nDraw your card."
                        : "Round " + CurrentRound.Value + "\nDraw your card.";
                break;
            case 2:
                PlayPanel.SetActive(true);
                break;
            case 3:
                RevealPanel.SetActive(true);
                if (continueButton != null) continueButton.gameObject.SetActive(false);
                StartRevealAnimation();
                break;
            case 5:
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

        if (drawnCardImage != null)
        {
            drawnCardImage.sprite = s;
            drawnCardImage.color = GetCardColor(cardType);
        }
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
        int myCard = RevealedCard.Value;

        if (ownRevealImage != null)
        {
            ownRevealImage.sprite = GetCardSprite(myCard);
            ownRevealImage.color = GetCardColor(myCard);
        }
        if (ownRevealName != null)
            ownRevealName.text = GetCardTypeName(myCard);

        if (opponentRevealImage != null)
        {
            opponentRevealImage.sprite = null;
            opponentRevealImage.color = Color.gray;
        }
        if (opponentRevealName != null) opponentRevealName.text = "?";
        if (revealFlavorText != null) revealFlavorText.text = "";
        if (roundResultText != null) roundResultText.text = "";

        revealTimer = 1.5f;
        revealAnimating = true;
    }

    void ShowRevealResult()
    {
        int opponentCard = OpponentRevealed.Value;

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
                if (roundResultText != null) roundResultText.text = "You Win This Round";
                if (revealFlavorText != null) revealFlavorText.text = WinReflections[Random.Range(0, WinReflections.Length)];
                break;
            case -1:
                if (roundResultText != null) roundResultText.text = "You Lose This Round";
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

        ulong winner = MatchWinner.Value;
        int myScore = Score.Value;
        int theirScore = OpponentScore.Value;

        if (finalScoreText != null)
            finalScoreText.text = myScore + " - " + theirScore;

        if (winner == OwnerClientId)
        {
            if (matchResultText != null) matchResultText.text = "Victory";
            if (matchFlavorText != null) matchFlavorText.text = MatchWinTexts[Random.Range(0, MatchWinTexts.Length)];
        }
        else if (winner == ulong.MaxValue)
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
        GameServer.Instance.PlayerReady(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    void RequestDrawCardServerRpc(ServerRpcParams rpcParams = default)
    {
        GameServer.Instance.PlayerDrawCard(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    void PlayCardServerRpc(ServerRpcParams rpcParams = default)
    {
        GameServer.Instance.PlayerPlayCard(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    void ReadyForNextRoundServerRpc(ServerRpcParams rpcParams = default)
    {
        GameServer.Instance.PlayerReadyForNext(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc]
    void RequestRematchServerRpc(ServerRpcParams rpcParams = default)
    {
        GameServer.Instance.PlayerRequestRematch(rpcParams.Receive.SenderClientId);
    }
}