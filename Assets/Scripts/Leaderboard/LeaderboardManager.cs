using UnityEngine;
using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const int EntriesPerPage = 10;

    [Header("LootLocker")]
    [SerializeField] private string leaderboardKey;
    [SerializeField] private int maxResults = 50;
    [SerializeField] private float serverRefreshDelay = 2f;

    [Header("Pages")]
    [SerializeField] private LeaderboardPage firstPage;
    [SerializeField] private LeaderboardPage normalPagePrefab;
    [SerializeField] private Transform pagesParent;

    [Header("Display")]
    [SerializeField] private bool prefixRankInName = true;

    private readonly List<LeaderboardPage> spawnedNormalPages = new();
    private readonly List<LeaderboardDisplayEntry> leaderboardEntries = new();

    private bool isConnected;
    private bool isLoading;

    private int localPlayerId;
    private string localPlayerName = "Player";

    private Coroutine pendingServerRefresh;

    private bool hasPendingLocalScore;
    private int pendingLocalScore;

    private struct LeaderboardDisplayEntry
    {
        public int PlayerId;
        public string PlayerName;
        public int Score;
        public int Rank;
        public bool IsLocalPlayer;

        public LeaderboardDisplayEntry(
            int playerId,
            string playerName,
            int score,
            int rank,
            bool isLocalPlayer
        )
        {
            PlayerId = playerId;
            PlayerName = playerName;
            Score = score;
            Rank = rank;
            IsLocalPlayer = isLocalPlayer;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartLootLockerSession();
    }

    private void StartLootLockerSession()
    {
        LootLockerSDKManager.StartGuestSession(response =>
        {
            if (!response.success)
            {
                Debug.LogWarning("Impossible de démarrer la session LootLocker.");
                return;
            }

            localPlayerId = response.player_id;

            if (string.IsNullOrEmpty(response.player_name))
            {
                string publicUid = response.public_uid ?? response.player_id.ToString();
                string uniqueName = "Joueur_" + publicUid[..Mathf.Min(4, publicUid.Length)];

                localPlayerName = uniqueName;

                LootLockerSDKManager.SetPlayerName(uniqueName, nameResponse =>
                {
                    if (nameResponse.success)
                    {
                        Debug.Log("Nom unique enregistré : " + uniqueName);
                    }
                    else
                    {
                        Debug.LogWarning("Impossible d'enregistrer le nom du joueur.");
                    }
                });
            }
            else
            {
                localPlayerName = response.player_name;
            }

            isConnected = true;
            RefreshLeaderboard();
        });
    }

    public void SubmitScoreAndRefresh(int timeInMilliseconds)
    {
        if (!isConnected)
        {
            Debug.LogWarning("Score non envoyé : pas encore connecté à LootLocker.");
            return;
        }

        hasPendingLocalScore = true;
        pendingLocalScore = timeInMilliseconds;

        ApplyLocalScore(timeInMilliseconds);

        LootLockerSDKManager.SubmitScore("", timeInMilliseconds, leaderboardKey, scoreResponse =>
        {
            if (!scoreResponse.success)
            {
                Debug.LogWarning("Impossible d'envoyer le score au leaderboard LootLocker.");
                return;
            }

            if (pendingServerRefresh != null)
                StopCoroutine(pendingServerRefresh);

            pendingServerRefresh = StartCoroutine(RefreshLeaderboardAfterDelay());
        });
    }

    public void RefreshLeaderboard()
    {
        if (!isConnected || isLoading)
            return;

        isLoading = true;

        LootLockerSDKManager.GetScoreList(leaderboardKey, maxResults, 0, response =>
        {
            if (!response.success)
            {
                Debug.LogWarning("Impossible de récupérer le leaderboard LootLocker.");
                isLoading = false;
                return;
            }

            SyncEntriesFromServer(response.items ?? Array.Empty<LootLockerLeaderboardMember>());

            if (ServerAlreadyHasLocalScore())
                hasPendingLocalScore = false;

            if (hasPendingLocalScore)
                ApplyLocalScore(pendingLocalScore, false);

            SortAndRankEntries();
            RenderLeaderboard();

            isLoading = false;
        });
    }

    private IEnumerator RefreshLeaderboardAfterDelay()
    {
        yield return new WaitForSeconds(serverRefreshDelay);

        pendingServerRefresh = null;
        RefreshLeaderboard();
    }

    private void SyncEntriesFromServer(LootLockerLeaderboardMember[] items)
    {
        leaderboardEntries.Clear();

        foreach (LootLockerLeaderboardMember item in items)
        {
            int playerId = item.player != null ? item.player.id : 0;
            string displayName = GetDisplayName(item);
            bool isLocalPlayerEntry = playerId == localPlayerId;

            leaderboardEntries.Add(new LeaderboardDisplayEntry(
                playerId,
                displayName,
                item.score,
                item.rank,
                isLocalPlayerEntry
            ));
        }
    }

    private bool ServerAlreadyHasLocalScore()
    {
        if (!hasPendingLocalScore)
            return false;

        return leaderboardEntries.Exists(entry =>
            entry.PlayerId == localPlayerId &&
            entry.Score <= pendingLocalScore
        );
    }

    private void ApplyLocalScore(int timeInMilliseconds, bool renderAfterApply = true)
    {
        int existingIndex = leaderboardEntries.FindIndex(entry =>
            entry.PlayerId == localPlayerId ||
            entry.IsLocalPlayer
        );

        if (existingIndex >= 0)
        {
            LeaderboardDisplayEntry existingEntry = leaderboardEntries[existingIndex];

            // En contre-la-montre, le meilleur score est le temps le plus bas.
            if (existingEntry.Score <= timeInMilliseconds)
            {
                existingEntry.IsLocalPlayer = true;
                existingEntry.PlayerName = localPlayerName;
                leaderboardEntries[existingIndex] = existingEntry;

                if (renderAfterApply)
                    RenderLeaderboard();

                return;
            }

            existingEntry.Score = timeInMilliseconds;
            existingEntry.PlayerName = localPlayerName;
            existingEntry.IsLocalPlayer = true;
            leaderboardEntries[existingIndex] = existingEntry;
        }
        else
        {
            leaderboardEntries.Add(new LeaderboardDisplayEntry(
                localPlayerId,
                localPlayerName,
                timeInMilliseconds,
                0,
                true
            ));
        }

        SortAndRankEntries();

        if (renderAfterApply)
            RenderLeaderboard();
    }

    private void SortAndRankEntries()
    {
        leaderboardEntries.Sort((a, b) =>
        {
            int scoreComparison = a.Score.CompareTo(b.Score);

            if (scoreComparison != 0)
                return scoreComparison;

            return string.Compare(a.PlayerName, b.PlayerName, StringComparison.Ordinal);
        });

        for (int i = 0; i < leaderboardEntries.Count; i++)
        {
            LeaderboardDisplayEntry entry = leaderboardEntries[i];
            entry.Rank = i + 1;
            leaderboardEntries[i] = entry;
        }
    }

    private void RenderLeaderboard()
    {
        if (firstPage == null)
        {
            Debug.LogError("FirstPage n'est pas assignée dans le LeaderboardManager.");
            return;
        }

        ClearSpawnedNormalPages();

        int totalEntries = leaderboardEntries.Count;
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(totalEntries / (float)EntriesPerPage));

        firstPage.gameObject.SetActive(true);

        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            LeaderboardPage page = GetPage(pageIndex);

            if (page == null)
                continue;

            FillPage(page, pageIndex);
        }
    }

    private LeaderboardPage GetPage(int pageIndex)
    {
        if (pageIndex == 0)
            return firstPage;

        if (normalPagePrefab == null)
        {
            Debug.LogError("NormalPagePrefab n'est pas assigné dans le LeaderboardManager.");
            return null;
        }

        Transform parent = pagesParent != null ? pagesParent : transform;

        LeaderboardPage page = Instantiate(normalPagePrefab, parent);
        page.gameObject.SetActive(true);

        spawnedNormalPages.Add(page);

        return page;
    }

    private void FillPage(LeaderboardPage page, int pageIndex)
    {
        int startEntryIndex = pageIndex * EntriesPerPage;
        int startRank = startEntryIndex + 1;

        page.ClearPage(prefixRankInName, startRank);

        for (int slotIndex = 0; slotIndex < EntriesPerPage; slotIndex++)
        {
            int entryIndex = startEntryIndex + slotIndex;

            if (entryIndex >= leaderboardEntries.Count)
                continue;

            LeaderboardDisplayEntry entry = leaderboardEntries[entryIndex];

            page.SetEntry(
                slotIndex,
                entry.PlayerName,
                FormatTime(entry.Score),
                entry.IsLocalPlayer,
                prefixRankInName,
                entry.Rank
            );
        }
    }

    private void ClearSpawnedNormalPages()
    {
        foreach (LeaderboardPage page in spawnedNormalPages)
        {
            if (page != null)
                Destroy(page.gameObject);
        }

        spawnedNormalPages.Clear();
    }

    private string GetDisplayName(LootLockerLeaderboardMember item)
    {
        if (item.player == null)
            return "Unknown";

        if (!string.IsNullOrEmpty(item.player.name))
            return item.player.name;

        return "Player " + item.player.id;
    }

    private string FormatTime(int milliseconds)
    {
        int min = milliseconds / 60000;
        int sec = milliseconds / 1000 % 60;
        int ms = milliseconds % 1000;

        return $"{min:00}:{sec:00}.{ms:000}";
    }
}
