using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;

// Fonction à placer dans le script qui gère la fin de la course :

// void OnRaceFinished(float totalTime)
// {
//     // Conversion secondes → millisecondes
//     int finalTime = Mathf.RoundToInt(totalTime * 1000f);

//     // Envoi au leaderboard
//     LeaderboardManager.Instance.SubmitScoreAndRefresh(finalTime);
// }

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;
    
    [Header("Leaderboard Setup")]
    [SerializeField] private string leaderboardKey;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject scrollbar;
    [SerializeField] private int maxResults;
    [SerializeField] private float serverRefreshDelay = 2f;
      
    private readonly List<GameObject> spawnedEntries = new();
    private readonly List<LeaderboardDisplayEntry> leaderboardEntries = new();

    private bool isConnected = false;
    private bool isLoading = false;
    private int localPlayerId;
    private string localPlayerName = "Player";
    private Coroutine pendingServerRefresh;
    private bool hasPendingLocalScore = false;
    private int pendingLocalScore;

    private struct LeaderboardDisplayEntry
    {
        public int PlayerId;
        public string PlayerName;
        public int Score;
        public int Rank;
        public bool IsLocalPlayer;

        public LeaderboardDisplayEntry(int playerId, string playerName, int score, int rank, bool isLocalPlayer)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            Score = score;
            Rank = rank;
            IsLocalPlayer = isLocalPlayer;
        }
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Expression Lambda / Fonction Callback
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            // Si le joueur n'a pas encore de nom
            if (string.IsNullOrEmpty(response.player_name))
            {
                // On fabrique son nom unique à partir de son ID LootLocker publique
                // On prend les 5 premiers caractères de cet ID pour pas que ce soit trop long
                string publicUid = response.public_uid ?? response.player_id.ToString();
                string uniqueName = "Joueur_" + publicUid[..Mathf.Min(4, publicUid.Length)];
                localPlayerName = uniqueName;

                // On envoie ce nom au serveur
                LootLockerSDKManager.SetPlayerName(uniqueName, (nameResponse) =>
                {
                    if (nameResponse.success)
                    {
                        Debug.Log("Nom unique enregistré : " + uniqueName);
                    }
                });
            }
            else
            {
                localPlayerName = response.player_name;
            }

            localPlayerId = response.player_id;
            isConnected = true;
            RefreshLeaderboard();
        });
    }

    public void SubmitScoreAndRefresh(int timeInMilliseconds)
    {
        if (!isConnected) return;

        hasPendingLocalScore = true;
        pendingLocalScore = timeInMilliseconds;
        ApplyLocalScore(timeInMilliseconds);

        LootLockerSDKManager.SubmitScore("", timeInMilliseconds, leaderboardKey, (scoreResponse) =>
        {
            if (scoreResponse.success)
            {
                if (pendingServerRefresh != null)
                    StopCoroutine(pendingServerRefresh);

                pendingServerRefresh = StartCoroutine(RefreshLeaderboardAfterDelay());
            }
            else
            {
                Debug.LogWarning("Impossible d'envoyer le score au leaderboard LootLocker.");
            }
        });
    }

    public void RefreshLeaderboard()
    {
        if (isLoading) return;
        isLoading = true;

        LootLockerSDKManager.GetScoreList(leaderboardKey, maxResults, 0, (response) =>
        {
            if (!response.success)
            {
                isLoading = false;
                return;
            }

            SyncEntriesFromServer(response.items ?? new LootLockerLeaderboardMember[0]);

            if (ServerAlreadyHasLocalScore())
                hasPendingLocalScore = false;

            if (hasPendingLocalScore)
                ApplyLocalScore(pendingLocalScore, false);

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

        foreach (var item in items)
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
        int existingIndex = leaderboardEntries.FindIndex(entry => entry.PlayerId == localPlayerId || entry.IsLocalPlayer);

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
        foreach (var obj in spawnedEntries)
        {
            Destroy(obj);
        }
        spawnedEntries.Clear();

        int visibleEntryCount = leaderboardEntries.Count;
        int displayCount = Mathf.Max(visibleEntryCount, 5);

        for (int i = 0; i < displayCount; i++)
        {
            GameObject obj = Instantiate(entryPrefab, contentParent);
            spawnedEntries.Add(obj);

                
            if (!obj.TryGetComponent<LeaderboardEntryUI>(out var entry))
            {
                Debug.LogError("Prefab sans LeaderboardEntryUI !");
                continue;
            }

            if (i < visibleEntryCount)
            {
                LeaderboardDisplayEntry leaderboardEntry = leaderboardEntries[i];

                if (leaderboardEntry.IsLocalPlayer)
                    entry.SetColor(Color.yellow);
                else
                    entry.SetColor(Color.white);

                entry.rankText.text = "#" + leaderboardEntry.Rank;
                entry.nameText.text = leaderboardEntry.PlayerName;
                entry.scoreText.text = FormatTime(leaderboardEntry.Score);
            }
            else
            {
                // Ligne vide
                entry.SetColor(Color.white);
                entry.rankText.text = "#" + (i + 1);
                entry.nameText.text = "--";
                entry.scoreText.text = "--:--.--";
            }

            // Alternance visuelle
            Image bg = obj.GetComponent<Image>();
            if (bg != null && i % 2 == 0)
                bg.color = new Color(1f, 1f, 1f, 0.05f);
        }

        // Activation du scroll si nécessaire
        bool needScroll = leaderboardEntries.Count > 5;
        scrollRect.vertical = needScroll;
        scrollbar.SetActive(needScroll);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
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

        return string.Format("{0:00}:{1:00}.{2:000}", min, sec, ms);
    }
}
