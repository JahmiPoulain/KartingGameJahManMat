using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const int EntriesPerPage = 10;

    private const string LocalBestScoreKey = "Leaderboard_LocalBestScore";
    private const string HasLocalBestScoreKey = "Leaderboard_HasLocalBestScore";
    private const string PendingUploadScoreKey = "Leaderboard_PendingUploadScore";
    private const string HasPendingUploadKey = "Leaderboard_HasPendingUpload";

    [Header("LootLocker")]
    [SerializeField] private string leaderboardKey;
    [SerializeField] private int maxResults = 50;
    [SerializeField] private float serverRefreshDelay = 2f;
    [SerializeField] private float reconnectInterval = 5f;

    [Header("Pages")]
    [SerializeField] private LeaderboardPage firstPage;
    [SerializeField] private LeaderboardPage normalPagePrefab;
    [SerializeField] private Transform pagesParent;

    [Header("Navigation")]
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private bool startOnFirstPageAfterRefresh = true;

    [Header("Display")]
    [SerializeField] private bool prefixRankInName = true;
    [SerializeField] private string fallbackLocalPlayerName = "Player";

    [Header("Status Message")]
    [SerializeField] private GameObject statusMessageRoot;
    [SerializeField] private TextMeshProUGUI statusMessageText;

    [TextArea]
    [SerializeField]
    private string offlineWithoutScoreMessage =
        "Vous êtes actuellement hors ligne.\nConnectez-vous à Internet pour voir le classement en ligne.";

    [TextArea]
    [SerializeField]
    private string offlineWithScoreMessage =
        "Vous êtes actuellement hors ligne.\nVotre meilleur temps est enregistré localement et sera synchronisé dès qu'une connexion sera disponible.";

    [TextArea]
    [SerializeField]
    private string syncingMessage =
        "Connexion au classement en ligne...";

    [Header("Debug")]
    [SerializeField] private bool simulateOfflineMode;
    [SerializeField] private bool enableDebugSubmit = true;
    [SerializeField] private KeyCode debugSubmitKey = KeyCode.T;
    [SerializeField] private int minRandomTimeMs = 30_000;
    [SerializeField] private int maxRandomTimeMs = 180_000;

    private readonly List<LeaderboardPage> pages = new();
    private readonly List<LeaderboardPage> spawnedNormalPages = new();
    private readonly List<LeaderboardDisplayEntry> leaderboardEntries = new();

    private bool isConnected;
    private bool isStartingSession;
    private bool isLoading;

    private int localPlayerId;
    private string localPlayerName;

    private Coroutine pendingServerRefresh;
    private Coroutine reconnectCoroutine;

    private int currentPageIndex;
    private bool canFocus;

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
        // Force l'assignation immédiate de l'instance
        Instance = this;
        localPlayerName = fallbackLocalPlayerName;
    }

    private void OnEnable()
    {
        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(ShowPreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(ShowNextPage);

        canFocus = false;
    }

    private void OnDisable()
    {
        if (previousPageButton != null)
            previousPageButton.onClick.RemoveListener(ShowPreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.RemoveListener(ShowNextPage);

        canFocus = false;
    }

    private void Start()
    {
        HideNavigationButtons();
        BuildOfflineViewFromLocalSave();
        ShowStatus(syncingMessage);
        StartLootLockerSession();
        StartReconnectLoop();
    }

    private void Update()
    {
        if (!enableDebugSubmit)
            return;

        if (Input.GetKeyDown(debugSubmitKey))
        {
            int randomScore = UnityEngine.Random.Range(minRandomTimeMs, maxRandomTimeMs + 1);
            Debug.Log($"Score debug envoyé : {FormatTime(randomScore)}");
            SubmitScoreAndRefresh(randomScore);
        }
    }

    // UNIQUE FONCTION DE FOCUS : Accessible partout et sécurisée
    public void ForceFocusOnNextButton()
    {
        if (EventSystem.current == null) return;

        canFocus = true;
        EventSystem.current.SetSelectedGameObject(null);

        if (nextPageButton != null && nextPageButton.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(nextPageButton.gameObject);
        }
        else if (previousPageButton != null && previousPageButton.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(previousPageButton.gameObject);
        }
    }

    private void StartLootLockerSession()
    {
        if (simulateOfflineMode)
        {
            isConnected = false;
            isStartingSession = false;
            BuildOfflineViewFromLocalSave();
            return;
        }

        if (isConnected || isStartingSession)
            return;

        isStartingSession = true;

        LootLockerSDKManager.StartGuestSession(response =>
        {
            isStartingSession = false;

            if (!response.success)
            {
                isConnected = false;
                BuildOfflineViewFromLocalSave();
                return;
            }

            isConnected = true;
            localPlayerId = response.player_id;

            if (string.IsNullOrEmpty(response.player_name))
            {
                string publicUid = response.public_uid ?? response.player_id.ToString();
                string uniqueName = "Player_" + publicUid[..Mathf.Min(4, publicUid.Length)];
                localPlayerName = uniqueName;

                LootLockerSDKManager.SetPlayerName(uniqueName, nameResponse => { });
            }
            else
            {
                localPlayerName = response.player_name;
            }

            HideStatus();
            TryUploadPendingLocalScore();
            RefreshLeaderboard();
        });
    }

    private void StartReconnectLoop()
    {
        if (reconnectCoroutine != null)
            StopCoroutine(reconnectCoroutine);

        reconnectCoroutine = StartCoroutine(ReconnectLoop());
    }

    private IEnumerator ReconnectLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(reconnectInterval);

            if (simulateOfflineMode)
            {
                if (isConnected) isConnected = false;
                BuildOfflineViewFromLocalSave();
                continue;
            }

            if (!isConnected && !isStartingSession)
            {
                StartLootLockerSession();
            }
            else if (isConnected && HasPendingUpload())
            {
                TryUploadPendingLocalScore();
            }
        }
    }

    public void SubmitScoreAndRefresh(int timeInMilliseconds)
    {
        bool improvedLocalBest = SaveLocalBestScoreIfBetter(timeInMilliseconds);

        if (!improvedLocalBest)
        {
            RenderLeaderboard();
            return;
        }

        MarkPendingUpload(GetLocalBestScore());
        ApplyLocalScore(GetLocalBestScore());

        if (simulateOfflineMode)
        {
            isConnected = false;
            BuildOfflineViewFromLocalSave();
            return;
        }

        if (!isConnected)
        {
            BuildOfflineViewFromLocalSave();
            StartLootLockerSession();
            return;
        }

        TryUploadPendingLocalScore();
    }

    public void RefreshLeaderboard()
    {
        if (simulateOfflineMode)
        {
            isConnected = false;
            BuildOfflineViewFromLocalSave();
            return;
        }

        if (!isConnected || isLoading)
            return;

        isLoading = true;

        LootLockerSDKManager.GetScoreList(leaderboardKey, maxResults, 0, response =>
        {
            if (!response.success)
            {
                isConnected = false;
                isLoading = false;
                BuildOfflineViewFromLocalSave();
                return;
            }

            SyncEntriesFromServer(response.items ?? Array.Empty<LootLockerLeaderboardMember>());

            if (HasLocalBestScore())
                ApplyLocalScore(GetLocalBestScore(), false);

            SortAndRankEntries();
            RenderLeaderboard();
            HideStatus();
            isLoading = false;
        });
    }

    private void TryUploadPendingLocalScore()
    {
        if (simulateOfflineMode || !isConnected || !HasPendingUpload())
            return;

        int scoreToUpload = GetPendingUploadScore();

        LootLockerSDKManager.SubmitScore("", scoreToUpload, leaderboardKey, scoreResponse =>
        {
            if (!scoreResponse.success) return;

            ClearPendingUpload();

            if (pendingServerRefresh != null)
                StopCoroutine(pendingServerRefresh);

            pendingServerRefresh = StartCoroutine(RefreshLeaderboardAfterDelay());
        });
    }

    private IEnumerator RefreshLeaderboardAfterDelay()
    {
        yield return new WaitForSeconds(serverRefreshDelay);
        pendingServerRefresh = null;
        RefreshLeaderboard();
    }

    private void BuildOfflineViewFromLocalSave()
    {
        leaderboardEntries.Clear();

        if (HasLocalBestScore())
        {
            int localBestScore = GetLocalBestScore();
            leaderboardEntries.Add(new LeaderboardDisplayEntry(localPlayerId, localPlayerName, localBestScore, 1, true));
            ShowStatus(offlineWithScoreMessage);
        }
        else
        {
            ShowStatus(offlineWithoutScoreMessage);
        }

        SortAndRankEntries();
        RenderLeaderboard();
    }

    private void SyncEntriesFromServer(LootLockerLeaderboardMember[] items)
    {
        leaderboardEntries.Clear();

        foreach (LootLockerLeaderboardMember item in items)
        {
            int playerId = item.player != null ? item.player.id : 0;
            string displayName = GetDisplayName(item);
            bool isLocalPlayerEntry = playerId == localPlayerId;

            leaderboardEntries.Add(new LeaderboardDisplayEntry(playerId, displayName, item.score, item.rank, isLocalPlayerEntry));
        }
    }

    private void ApplyLocalScore(int timeInMilliseconds, bool renderAfterApply = true)
    {
        int existingIndex = leaderboardEntries.FindIndex(entry => entry.PlayerId == localPlayerId || entry.IsLocalPlayer);

        if (existingIndex >= 0)
        {
            LeaderboardDisplayEntry existingEntry = leaderboardEntries[existingIndex];

            if (existingEntry.Score <= timeInMilliseconds)
            {
                existingEntry.PlayerId = localPlayerId;
                existingEntry.PlayerName = localPlayerName;
                existingEntry.IsLocalPlayer = true;
                leaderboardEntries[existingIndex] = existingEntry;

                if (renderAfterApply) RenderLeaderboard();
                return;
            }

            existingEntry.PlayerId = localPlayerId;
            existingEntry.Score = timeInMilliseconds;
            existingEntry.PlayerName = localPlayerName;
            existingEntry.IsLocalPlayer = true;
            leaderboardEntries[existingIndex] = existingEntry;
        }
        else
        {
            leaderboardEntries.Add(new LeaderboardDisplayEntry(localPlayerId, localPlayerName, timeInMilliseconds, 0, true));
        }

        SortAndRankEntries();

        if (renderAfterApply)
            RenderLeaderboard();
    }

    private bool SaveLocalBestScoreIfBetter(int timeInMilliseconds)
    {
        if (!HasLocalBestScore())
        {
            SaveLocalBestScore(timeInMilliseconds);
            return true;
        }

        int currentBest = GetLocalBestScore();
        if (timeInMilliseconds >= currentBest) return false;

        SaveLocalBestScore(timeInMilliseconds);
        return true;
    }

    private void SaveLocalBestScore(int timeInMilliseconds)
    {
        PlayerPrefs.SetInt(HasLocalBestScoreKey, 1);
        PlayerPrefs.SetInt(LocalBestScoreKey, timeInMilliseconds);
        PlayerPrefs.Save();
    }

    private bool HasLocalBestScore() => PlayerPrefs.GetInt(HasLocalBestScoreKey, 0) == 1;
    private int GetLocalBestScore() => PlayerPrefs.GetInt(LocalBestScoreKey, int.MaxValue);
    private void MarkPendingUpload(int timeInMilliseconds)
    {
        PlayerPrefs.SetInt(HasPendingUploadKey, 1);
        PlayerPrefs.SetInt(PendingUploadScoreKey, timeInMilliseconds);
        PlayerPrefs.Save();
    }

    private bool HasPendingUpload() => PlayerPrefs.GetInt(HasPendingUploadKey, 0) == 1;
    private int GetPendingUploadScore() => PlayerPrefs.GetInt(PendingUploadScoreKey, int.MaxValue);
    private void ClearPendingUpload()
    {
        PlayerPrefs.DeleteKey(HasPendingUploadKey);
        PlayerPrefs.DeleteKey(PendingUploadScoreKey);
        PlayerPrefs.Save();
    }

    private void SortAndRankEntries()
    {
        leaderboardEntries.Sort((a, b) =>
        {
            int scoreComparison = a.Score.CompareTo(b.Score);
            if (scoreComparison != 0) return scoreComparison;
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
        if (firstPage == null) return;

        ClearSpawnedNormalPages();
        pages.Clear();
        pages.Add(firstPage);

        int totalEntries = leaderboardEntries.Count;
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(totalEntries / (float)EntriesPerPage));

        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            LeaderboardPage page = (pageIndex == 0) ? firstPage : CreateNormalPage();
            if (page == null) continue;

            if (pageIndex > 0) pages.Add(page);
            FillPage(page, pageIndex);
        }

        currentPageIndex = startOnFirstPageAfterRefresh ? 0 : Mathf.Clamp(currentPageIndex, 0, pages.Count - 1);
        ShowPage(currentPageIndex);
    }

    private LeaderboardPage CreateNormalPage()
    {
        if (normalPagePrefab == null) return null;

        Transform parent = pagesParent != null ? pagesParent : transform;
        LeaderboardPage page = Instantiate(normalPagePrefab, parent);
        page.gameObject.SetActive(false);
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
            if (entryIndex >= leaderboardEntries.Count) continue;

            LeaderboardDisplayEntry entry = leaderboardEntries[entryIndex];
            page.SetEntry(slotIndex, entry.PlayerName, FormatTime(entry.Score), entry.IsLocalPlayer, prefixRankInName, entry.Rank);
        }
    }

    private void ShowPreviousPage() => ShowPage(currentPageIndex - 1);
    private void ShowNextPage() => ShowPage(currentPageIndex + 1);

    private void ShowPage(int pageIndex)
    {
        if (pages.Count == 0)
        {
            HideNavigationButtons();
            return;
        }

        currentPageIndex = Mathf.Clamp(pageIndex, 0, pages.Count - 1);

        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] != null)
                pages[i].gameObject.SetActive(i == currentPageIndex);
        }

        UpdateNavigationButtons();

        if (canFocus)
        {
            GameObject currentSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (currentSelected == null || !currentSelected.activeInHierarchy)
            {
                ForceFocusOnNextButton();
            }
        }
    }

    private void UpdateNavigationButtons()
    {
        if (previousPageButton != null) previousPageButton.gameObject.SetActive(currentPageIndex > 0);
        if (nextPageButton != null) nextPageButton.gameObject.SetActive(currentPageIndex < pages.Count - 1);
    }

    private void HideNavigationButtons()
    {
        if (previousPageButton != null) previousPageButton.gameObject.SetActive(false);
        if (nextPageButton != null) nextPageButton.gameObject.SetActive(false);
    }

    private void ClearSpawnedNormalPages()
    {
        foreach (LeaderboardPage page in spawnedNormalPages)
        {
            if (page != null) Destroy(page.gameObject);
        }
        spawnedNormalPages.Clear();
    }

    private string GetDisplayName(LootLockerLeaderboardMember item)
    {
        if (item.player == null) return "Unknown";
        return !string.IsNullOrEmpty(item.player.name) ? item.player.name : "Player " + item.player.id;
    }

    private void ShowStatus(string message)
    {
        if (statusMessageRoot != null) statusMessageRoot.SetActive(true);
        if (statusMessageText != null) statusMessageText.text = message;
    }

    private void HideStatus()
    {
        if (statusMessageRoot != null) statusMessageRoot.SetActive(false);
        if (statusMessageText != null) statusMessageText.text = string.Empty;
    }

    private string FormatTime(int milliseconds)
    {
        int min = milliseconds / 60000;
        int sec = milliseconds / 1000 % 60;
        int ms = milliseconds % 1000;
        return $"{min:00}:{sec:00}.{ms:000}";
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Clear Local Leaderboard Save")]
    private void ClearLocalLeaderboardSave()
    {
        PlayerPrefs.DeleteKey(LocalBestScoreKey);
        PlayerPrefs.DeleteKey(HasLocalBestScoreKey);
        PlayerPrefs.DeleteKey(PendingUploadScoreKey);
        PlayerPrefs.DeleteKey(HasPendingUploadKey);
        PlayerPrefs.Save();
        BuildOfflineViewFromLocalSave();
    }
#endif
}