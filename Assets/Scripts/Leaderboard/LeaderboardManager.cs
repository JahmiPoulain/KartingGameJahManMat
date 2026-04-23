using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;
using System.Collections.Generic;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;
    private const int EncodedScoreBase = int.MaxValue;
    
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

            LootLockerSDKManager.GetLeaderboardData(leaderboardKey, (leaderboardResponse) =>
            {
                if (leaderboardResponse.success)
                {
                    Debug.Log($"Leaderboard '{leaderboardKey}' direction={leaderboardResponse.direction_method}, overwrite={leaderboardResponse.overwrite_score_on_submit}, type={leaderboardResponse.type}");
                }
                else
                {
                    Debug.LogWarning($"Impossible de lire la config du leaderboard '{leaderboardKey}' : {leaderboardResponse.errorData.message}");
                }
            });

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

    public void SubmitScoreAndRefresh(int timeInMilliseconds)
    {
        if (!isConnected) return;

        LootLockerSDKManager.GetMemberRank(leaderboardKey, localPlayerId.ToString(), (rankResponse) =>
        {
            if (!rankResponse.success)
            {
                RefreshLeaderboard();
                return;
            }

            bool hasNoScore = rankResponse.rank == 0;
            int previousTime = DecodeScore(rankResponse.score);
            bool isBetterTime = timeInMilliseconds < previousTime;
            Debug.Log($"Leaderboard compare: nouveau={timeInMilliseconds}, ancien={previousTime}, rank={rankResponse.rank}, submit={hasNoScore || isBetterTime}");

            if (!hasNoScore && !isBetterTime)
            {
                RefreshLeaderboard();
                return;
            }

            SubmitScore(timeInMilliseconds);
        });
    }

    private void SubmitScore(int timeInMilliseconds)
    {
        int encodedScore = EncodeScore(timeInMilliseconds);
        LootLockerSDKManager.SubmitScore("", encodedScore, leaderboardKey, (scoreResponse) =>
        {
            if (scoreResponse.success)
                Debug.Log($"Temps envoye: {timeInMilliseconds}, score encode LootLocker apres submit: {scoreResponse.score}");
            else
                Debug.LogWarning($"Erreur submit leaderboard: {scoreResponse.errorData.message}");

            RefreshLeaderboard();
        });
    }

    public void RefreshLeaderboard()
    {
        if (isLoading)
        {
            pendingRefresh = true;
            return;
        }

        isLoading = true;
        pendingRefresh = false;

        LootLockerSDKManager.GetScoreList(leaderboardKey, maxResults, 0, (response) =>
        {
            if (!response.success)
            {
                isLoading = false;
                RefreshPendingLeaderboard();
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
                    entry.scoreText.text = FormatTime(DecodeScore(item.score));
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
            RefreshPendingLeaderboard();
        });
    }

    private void RefreshPendingLeaderboard()
    {
        if (pendingRefresh)
            RefreshLeaderboard();
    }

    private string FormatTime(int milliseconds)
    {
        int min = milliseconds / 60000;
        int sec = milliseconds / 1000 % 60;
        int ms = milliseconds % 1000;

        return string.Format("{0:00}:{1:00}.{2:000}", min, sec, ms);
    }

    private int EncodeScore(int milliseconds)
    {
        return EncodedScoreBase - milliseconds;
    }

    private int DecodeScore(int score)
    {
        // Anciennes valeurs deja envoyees en millisecondes directes.
        if (score < 1000000000)
            return score;

        return EncodedScoreBase - score;
    }

    private string FormatPlayerName(LootLockerPlayer player)
    {
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
