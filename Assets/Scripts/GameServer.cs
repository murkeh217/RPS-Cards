using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameServer : NetworkBehaviour
{
    public static GameServer Instance;

    private HashSet<ulong> readyPlayers = new HashSet<ulong>();
    private Dictionary<ulong, List<int>> decks = new Dictionary<ulong, List<int>>();
    private Dictionary<ulong, int> drawnCards = new Dictionary<ulong, int>();
    private HashSet<ulong> playedPlayers = new HashSet<ulong>();
    private HashSet<ulong> continueReady = new HashSet<ulong>();
    private HashSet<ulong> rematchReady = new HashSet<ulong>();
    private int currentRound = 0;
    private bool matchActive = false;
    private bool suddenDeath = false;
    private ulong player1 = ulong.MaxValue;
    private ulong player2 = ulong.MaxValue;

    private void Awake()
    {
        Instance = this;
    }

    List<int> BuildDeck()
    {
        List<int> deck = new List<int>();
        for (int i = 0; i < 10; i++) deck.Add(0);
        for (int i = 0; i < 10; i++) deck.Add(1);
        for (int i = 0; i < 10; i++) deck.Add(2);
        ShuffleDeck(deck);
        return deck;
    }

    void ShuffleDeck(List<int> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }

    int DrawFromDeck(ulong clientId)
    {
        if (!decks.ContainsKey(clientId) || decks[clientId].Count == 0)
        {
            decks[clientId] = BuildDeck();
        }
        int card = decks[clientId][0];
        decks[clientId].RemoveAt(0);
        return card;
    }

    Client GetClientComponent(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return null;
        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj == null) return null;
        playerObj.TryGetComponent<Client>(out var client);
        return client;
    }

    public void PlayerReady(ulong clientId)
    {
        if (!IsServer) return;

        readyPlayers.Add(clientId);

        if (readyPlayers.Count >= 2 && !matchActive)
        {
            var ids = readyPlayers.ToList();
            player1 = ids[0];
            player2 = ids[1];

            Client c1 = GetClientComponent(player1);
            Client c2 = GetClientComponent(player2);

            if (c1 != null && c2 != null)
            {
                c1.OpponentName.Value = c2.playerName.Value;
                c2.OpponentName.Value = c1.playerName.Value;
            }

            StartMatch();
        }
    }

    void StartMatch()
    {
        matchActive = true;
        currentRound = 0;
        suddenDeath = false;

        Client c1 = GetClientComponent(player1);
        Client c2 = GetClientComponent(player2);

        if (c1 != null)
        {
            c1.Score.Value = 0;
            c1.OpponentScore.Value = 0;
            c1.CurrentRound.Value = 0;
            c1.MatchOver.Value = false;
            c1.MatchWinner.Value = ulong.MaxValue;
            c1.IsSuddenDeath.Value = false;
            decks[player1] = BuildDeck();
        }

        if (c2 != null)
        {
            c2.Score.Value = 0;
            c2.OpponentScore.Value = 0;
            c2.CurrentRound.Value = 0;
            c2.MatchOver.Value = false;
            c2.MatchWinner.Value = ulong.MaxValue;
            c2.IsSuddenDeath.Value = false;
            decks[player2] = BuildDeck();
        }

        StartNextRound();
    }

    void StartNextRound()
    {
        currentRound++;
        playedPlayers.Clear();
        continueReady.Clear();
        drawnCards.Clear();

        Client c1 = GetClientComponent(player1);
        Client c2 = GetClientComponent(player2);

        if (c1 != null)
        {
            c1.CurrentRound.Value = currentRound;
            c1.HasPlayed.Value = false;
            c1.DrawnCard.Value = -1;
            c1.RevealedCard.Value = -1;
            c1.OpponentRevealed.Value = -1;
            c1.RoundResult.Value = -1;
            c1.IsSuddenDeath.Value = suddenDeath;
            c1.GamePhase.Value = 1;
        }

        if (c2 != null)
        {
            c2.CurrentRound.Value = currentRound;
            c2.HasPlayed.Value = false;
            c2.DrawnCard.Value = -1;
            c2.RevealedCard.Value = -1;
            c2.OpponentRevealed.Value = -1;
            c2.RoundResult.Value = -1;
            c2.IsSuddenDeath.Value = suddenDeath;
            c2.GamePhase.Value = 1;
        }
    }

    public void PlayerDrawCard(ulong clientId)
    {
        if (!IsServer) return;

        Client c = GetClientComponent(clientId);
        if (c == null) return;
        if (c.GamePhase.Value != 1) return;
        if (c.DrawnCard.Value >= 0) return;

        int card = DrawFromDeck(clientId);
        drawnCards[clientId] = card;
        c.DrawnCard.Value = card;
        c.GamePhase.Value = 2;
    }

    public void PlayerPlayCard(ulong clientId)
    {
        if (!IsServer) return;

        Client c = GetClientComponent(clientId);
        if (c == null) return;
        if (c.GamePhase.Value != 2) return;

        c.HasPlayed.Value = true;
        playedPlayers.Add(clientId);

        if (playedPlayers.Contains(player1) && playedPlayers.Contains(player2))
        {
            ResolveRound();
        }
    }

    void ResolveRound()
    {
        int card1 = drawnCards.ContainsKey(player1) ? drawnCards[player1] : -1;
        int card2 = drawnCards.ContainsKey(player2) ? drawnCards[player2] : -1;

        Client c1 = GetClientComponent(player1);
        Client c2 = GetClientComponent(player2);

        if (c1 == null || c2 == null) return;

        c1.RevealedCard.Value = card1;
        c2.RevealedCard.Value = card2;

        c1.OpponentRevealed.Value = card2;
        c2.OpponentRevealed.Value = card1;

        int result1 = DetermineResult(card1, card2);

        if (result1 == 1)
        {
            c1.Score.Value += 1;
        }
        else if (result1 == -1)
        {
            c2.Score.Value += 1;
        }

        c1.OpponentScore.Value = c2.Score.Value;
        c2.OpponentScore.Value = c1.Score.Value;

        c1.RoundResult.Value = result1;
        c2.RoundResult.Value = -result1;

        c1.GamePhase.Value = 3;
        c2.GamePhase.Value = 3;
    }

    int DetermineResult(int card1, int card2)
    {
        if (card1 == card2) return 0;
        if ((card1 == 0 && card2 == 2) ||
            (card1 == 1 && card2 == 0) ||
            (card1 == 2 && card2 == 1))
            return 1;
        return -1;
    }

    public void PlayerReadyForNext(ulong clientId)
    {
        if (!IsServer) return;
        continueReady.Add(clientId);

        if (continueReady.Contains(player1) && continueReady.Contains(player2))
        {
            if (suddenDeath)
            {
                CheckSuddenDeathEnd();
            }
            else if (currentRound >= 7)
            {
                CheckMatchEnd();
            }
            else
            {
                StartNextRound();
            }
        }
    }

    void CheckMatchEnd()
    {
        Client c1 = GetClientComponent(player1);
        Client c2 = GetClientComponent(player2);

        if (c1 == null || c2 == null) return;

        if (c1.Score.Value > c2.Score.Value)
        {
            EndMatch(player1);
        }
        else if (c2.Score.Value > c1.Score.Value)
        {
            EndMatch(player2);
        }
        else
        {
            suddenDeath = true;
            StartNextRound();
        }
    }

    void CheckSuddenDeathEnd()
    {
        Client c1 = GetClientComponent(player1);
        if (c1 == null) return;

        int lastResult = c1.RoundResult.Value;

        if (lastResult == 1)
        {
            EndMatch(player1);
        }
        else if (lastResult == -1)
        {
            EndMatch(player2);
        }
        else
        {
            StartNextRound();
        }
    }

    void EndMatch(ulong winnerId)
    {
        matchActive = false;

        Client c1 = GetClientComponent(player1);
        Client c2 = GetClientComponent(player2);

        if (c1 != null)
        {
            c1.MatchWinner.Value = winnerId;
            c1.MatchOver.Value = true;
            c1.GamePhase.Value = 5;
        }

        if (c2 != null)
        {
            c2.MatchWinner.Value = winnerId;
            c2.MatchOver.Value = true;
            c2.GamePhase.Value = 5;
        }
    }

    public void PlayerRequestRematch(ulong clientId)
    {
        if (!IsServer) return;
        rematchReady.Add(clientId);

        if (rematchReady.Contains(player1) && rematchReady.Contains(player2))
        {
            rematchReady.Clear();
            StartMatch();
        }
    }
}