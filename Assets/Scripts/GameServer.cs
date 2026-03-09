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

    List<ulong> GetAllPlayerIds()
    {
        return NetworkManager.Singleton.ConnectedClientsIds.ToList();
    }

    public void PlayerReady(ulong clientId)
    {
        if (!IsServer) return;

        readyPlayers.Add(clientId);

        if (readyPlayers.Count >= 2 && !matchActive)
        {
            StartMatch();
        }
    }

    void StartMatch()
    {
        matchActive = true;
        currentRound = 0;
        suddenDeath = false;

        foreach (ulong id in GetAllPlayerIds())
        {
            Client c = GetClientComponent(id);
            if (c == null) continue;
            c.Score.Value = 0;
            c.OpponentScore.Value = 0;
            c.CurrentRound.Value = 0;
            c.MatchOver.Value = false;
            c.MatchWinner.Value = -1;
            c.IsSuddenDeath.Value = false;
            decks[id] = BuildDeck();
        }

        StartNextRound();
    }

    void StartNextRound()
    {
        currentRound++;
        playedPlayers.Clear();
        continueReady.Clear();
        drawnCards.Clear();

        foreach (ulong id in GetAllPlayerIds())
        {
            Client c = GetClientComponent(id);
            if (c == null) continue;
            c.CurrentRound.Value = currentRound;
            c.HasPlayed.Value = false;
            c.DrawnCard.Value = -1;
            c.RevealedCard.Value = -1;
            c.RoundResult.Value = -1;
            c.IsSuddenDeath.Value = suddenDeath;
            c.GamePhase.Value = 1;
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

        var allIds = GetAllPlayerIds();
        if (playedPlayers.Count >= 2 && playedPlayers.IsSupersetOf(allIds))
        {
            ResolveRound();
        }
    }

    void ResolveRound()
    {
        var ids = GetAllPlayerIds();
        if (ids.Count < 2) return;

        ulong p1 = ids[0];
        ulong p2 = ids[1];

        int card1 = drawnCards.ContainsKey(p1) ? drawnCards[p1] : -1;
        int card2 = drawnCards.ContainsKey(p2) ? drawnCards[p2] : -1;

        Client c1 = GetClientComponent(p1);
        Client c2 = GetClientComponent(p2);

        if (c1 == null || c2 == null) return;

        c1.RevealedCard.Value = card1;
        c2.RevealedCard.Value = card2;

        int result1 = DetermineResult(card1, card2);
        int result2 = -result1;

        if (result1 == 1)
        {
            c1.Score.Value += 1;
            c2.OpponentScore.Value = c1.Score.Value;
            c1.OpponentScore.Value = c2.Score.Value;
        }
        else if (result2 == 1)
        {
            c2.Score.Value += 1;
            c1.OpponentScore.Value = c2.Score.Value;
            c2.OpponentScore.Value = c1.Score.Value;
        }

        c1.RoundResult.Value = result1;
        c2.RoundResult.Value = result2;

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

        var allIds = GetAllPlayerIds();
        if (continueReady.Count >= 2 && continueReady.IsSupersetOf(allIds))
        {
            if (currentRound >= 7 && !suddenDeath)
            {
                CheckMatchEnd();
            }
            else if (suddenDeath)
            {
                CheckSuddenDeathEnd();
            }
            else
            {
                StartNextRound();
            }
        }
    }

    void CheckMatchEnd()
    {
        var ids = GetAllPlayerIds();
        if (ids.Count < 2) return;

        Client c1 = GetClientComponent(ids[0]);
        Client c2 = GetClientComponent(ids[1]);

        if (c1 == null || c2 == null) return;

        if (c1.Score.Value > c2.Score.Value)
        {
            EndMatch((int)ids[0], c1, c2);
        }
        else if (c2.Score.Value > c1.Score.Value)
        {
            EndMatch((int)ids[1], c1, c2);
        }
        else
        {
            suddenDeath = true;
            StartNextRound();
        }
    }

    void CheckSuddenDeathEnd()
    {
        var ids = GetAllPlayerIds();
        if (ids.Count < 2) return;

        Client c1 = GetClientComponent(ids[0]);
        Client c2 = GetClientComponent(ids[1]);

        if (c1 == null || c2 == null) return;

        int lastResult = c1.RoundResult.Value;

        if (lastResult == 1)
        {
            EndMatch((int)ids[0], c1, c2);
        }
        else if (lastResult == -1)
        {
            EndMatch((int)ids[1], c1, c2);
        }
        else
        {
            StartNextRound();
        }
    }

    void EndMatch(int winnerId, Client c1, Client c2)
    {
        matchActive = false;

        c1.MatchWinner.Value = winnerId;
        c2.MatchWinner.Value = winnerId;
        c1.MatchOver.Value = true;
        c2.MatchOver.Value = true;
        c1.GamePhase.Value = 5;
        c2.GamePhase.Value = 5;
    }

    public void PlayerRequestRematch(ulong clientId)
    {
        if (!IsServer) return;
        rematchReady.Add(clientId);

        var allIds = GetAllPlayerIds();
        if (rematchReady.Count >= 2 && rematchReady.IsSupersetOf(allIds))
        {
            rematchReady.Clear();
            readyPlayers.Clear();
            foreach (ulong id in allIds)
            {
                readyPlayers.Add(id);
            }
            StartMatch();
        }
    }
}