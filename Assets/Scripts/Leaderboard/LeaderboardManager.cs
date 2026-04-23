using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;
using System.Collections.Generic;

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
      
    private readonly List<GameObject> spawnedEntries = new();

    private bool isConnected = false;
    private bool isLoading = false;
    private bool pendingRefresh = false;
    private System.Action pendingRefreshComplete;
    private int localPlayerId;

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
            if (!response.success) return;

            localPlayerId = response.player_id;
            isConnected = true;

            string playerName = FormatPlayerName(response.player_identifier, response.player_id);
            if (response.player_name != playerName)
            {
                LootLockerSDKManager.SetPlayerName(playerName, (_) =>
                {
                    RefreshLeaderboard();
                });
            }
            else
            {
                RefreshLeaderboard();
            }
        });
    }

    public void SubmitScoreAndRefresh(int timeInMilliseconds, System.Action onComplete = null)
    {
        if (!isConnected)
        {
            onComplete?.Invoke();
            return;
        }

        LootLockerSDKManager.GetMemberRank(leaderboardKey, localPlayerId.ToString(), (rankResponse) =>
        {
            if (!rankResponse.success)
            {
                RefreshLeaderboard(onComplete);
                return;
            }

            bool hasNoScore = rankResponse.rank == 0;
            bool isBetterTime = timeInMilliseconds < rankResponse.score;

            if (!hasNoScore && !isBetterTime)
            {
                RefreshLeaderboard(onComplete);
                return;
            }

            LootLockerSDKManager.SubmitScore("", timeInMilliseconds, leaderboardKey, (_) =>
            {
                RefreshLeaderboard(onComplete);
            });
        });
    }

    public void RefreshLeaderboard(System.Action onComplete = null)
    {
        if (isLoading)
        {
            pendingRefresh = true;
            pendingRefreshComplete += onComplete;
            return;
        }

        isLoading = true;

        LootLockerSDKManager.GetScoreList(leaderboardKey, maxResults, 0, (response) =>
        {
            if (!response.success)
            {
                isLoading = false;
                FinishRefresh(onComplete);
                return;
            }

            LootLockerLeaderboardMember[] items = response.items ?? new LootLockerLeaderboardMember[0];

            // Nettoyage des anciennes entrées
            foreach (var obj in spawnedEntries)
            {
                Destroy(obj);
            }
            spawnedEntries.Clear();

            int displayCount = Mathf.Max(items.Length, 5);

            for (int i = 0; i < displayCount; i++)
            {
                GameObject obj = Instantiate(entryPrefab, contentParent);
                spawnedEntries.Add(obj);

                if (!obj.TryGetComponent<LeaderboardEntryUI>(out var entry))
                {
                    Debug.LogError("Prefab sans LeaderboardEntryUI !");
                    continue;
                }

                if (i < items.Length)
                {
                    var item = items[i];

                    // Couleur joueur local
                    if (item.player != null && item.player.id == localPlayerId)
                        entry.SetColor(Color.yellow);
                    else
                        entry.SetColor(Color.white);

                    // Rank
                    entry.rankText.text = "#" + item.rank;

                    // Nom
                    string displayName = "Unknown";
                    if (item.player != null)
                    {
                        if (!string.IsNullOrEmpty(item.player.name))
                            displayName = item.player.name;
                        else
                            displayName = FormatPlayerName(item.player);
                    }
                    entry.nameText.text = displayName;

                    // Score
                    entry.scoreText.text = FormatTime(item.score);
                }
                else
                {
                    // Ligne vide
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
            bool needScroll = items.Length > 5;
            scrollRect.vertical = needScroll;
            scrollbar.SetActive(needScroll);

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;

            isLoading = false;
            FinishRefresh(onComplete);
        });
    }

    private void FinishRefresh(System.Action onComplete)
    {
        if (pendingRefresh)
        {
            System.Action pendingComplete = pendingRefreshComplete;
            pendingRefresh = false;
            pendingRefreshComplete = null;
            RefreshLeaderboard(pendingComplete);
            return;
        }

        onComplete?.Invoke();
    }

    private string FormatTime(int milliseconds)
    {
        int min = milliseconds / 60000;
        int sec = milliseconds / 1000 % 60;
        int ms = milliseconds % 1000;

        return string.Format("{0:00}:{1:00}.{2:000}", min, sec, ms);
    }

    private string FormatPlayerName(LootLockerPlayer player)
    {
        if (!string.IsNullOrEmpty(player.name))
            return player.name;

        if (!string.IsNullOrEmpty(player.ulid))
            return FormatPlayerName(player.ulid, player.id);

        if (!string.IsNullOrEmpty(player.public_uid))
            return FormatPlayerName(player.public_uid, player.id);

        return FormatPlayerName(null, player.id);
    }

    private string FormatPlayerName(string playerIdentifier, int fallbackPlayerId)
    {
        string id = !string.IsNullOrEmpty(playerIdentifier) ? playerIdentifier : fallbackPlayerId.ToString();
        string shortId = id.Length > 4 ? id.Substring(0, 4) : id;

        return "Joueur_" + shortId.ToUpperInvariant();
    }
}
