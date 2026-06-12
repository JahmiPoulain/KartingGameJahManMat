using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;

public enum LeaderboardGameMode
{
    TimeAttack,
    ContreLaMontre
}

public enum LeaderboardCircuitMode
{
    Normal,
    Reverse
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const int EntriesPerPage = 10;

    private const string LocalBestScoreKeyPrefix = "Leaderboard_LocalBestScore";
    private const string HasLocalBestScoreKeyPrefix = "Leaderboard_HasLocalBestScore";
    private const string PendingUploadScoreKeyPrefix = "Leaderboard_PendingUploadScore";
    private const string HasPendingUploadKeyPrefix = "Leaderboard_HasPendingUpload";

    [Header("LootLocker Leaderboard Keys")]
    [SerializeField] private string timeAttackNormalKey;
    [SerializeField] private string timeAttackReverseKey;
    [SerializeField] private string contreLaMontreNormalKey;
    [SerializeField] private string contreLaMontreReverseKey;

    [Header("LootLocker Settings")]
    [SerializeField] private int maxResults = 50;
    [SerializeField] private float serverRefreshDelay = 2f;
    [SerializeField] private float reconnectInterval = 5f;

    [Header("Current Displayed Leaderboard")]
    [SerializeField] private LeaderboardGameMode currentGameMode = LeaderboardGameMode.TimeAttack;
    [SerializeField] private LeaderboardCircuitMode currentCircuitMode = LeaderboardCircuitMode.Normal;

    [Header("Toggle Buttons")]
    [SerializeField] private Button gameModeToggleButton;
    [SerializeField] private Button circuitModeToggleButton;

    [Header("Toggle Button Labels")]
    [SerializeField] private TextMeshProUGUI gameModeToggleButtonText;
    [SerializeField] private TextMeshProUGUI circuitModeToggleButtonText;

    [Header("Profiles")]
    [SerializeField] private ProfilsManager profilsManager;
    [SerializeField] private bool hideModeButtonsWhileManagingPlayers = true;

    [Header("Optional Labels")]
    [SerializeField] private TextMeshProUGUI leaderboardTitleText;
    [SerializeField] private TextMeshProUGUI circuitModeText;

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

    private struct LeaderboardDisplayEntry
    {
        public int PlayerId;
        public string MemberId;
        public string PlayerName;
        public int Score;
        public int Rank;
        public bool IsLocalPlayer;

        public LeaderboardDisplayEntry(
            int playerId,
            string memberId,
            string playerName,
            int score,
            int rank,
            bool isLocalPlayer
        )
        {
            PlayerId = playerId;
            MemberId = memberId;
            PlayerName = playerName;
            Score = score;
            Rank = rank;
            IsLocalPlayer = isLocalPlayer;
        }
    }

    private readonly struct LeaderboardTarget
    {
        public readonly LeaderboardGameMode GameMode;
        public readonly LeaderboardCircuitMode CircuitMode;

        public LeaderboardTarget(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
        {
            GameMode = gameMode;
            CircuitMode = circuitMode;
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
        localPlayerName = "Player";
    }

    private void OnEnable()
    {
        ConfigureControllerNavigation();

        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(ShowPreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(ShowNextPage);

        if (gameModeToggleButton != null)
            gameModeToggleButton.onClick.AddListener(ToggleGameMode);

        if (circuitModeToggleButton != null)
            circuitModeToggleButton.onClick.AddListener(ToggleCircuitMode);
    }

    private void OnDisable()
    {
        if (previousPageButton != null)
            previousPageButton.onClick.RemoveListener(ShowPreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.RemoveListener(ShowNextPage);

        if (gameModeToggleButton != null)
            gameModeToggleButton.onClick.RemoveListener(ToggleGameMode);

        if (circuitModeToggleButton != null)
            circuitModeToggleButton.onClick.RemoveListener(ToggleCircuitMode);
    }

    private void Start()
    {
        EnsureProfilsManager();
        BindProfilsManager();
        HideNavigationButtons();
        UpdateModeLabels();
        ConfigureControllerNavigation();
        HideStatus();

        StartLootLockerSession();
        StartReconnectLoop();
    }

    private void EnsureProfilsManager()
    {
        if (profilsManager == null)
            profilsManager = GetComponent<ProfilsManager>();

        if (profilsManager == null)
            profilsManager = FindFirstObjectByType<ProfilsManager>();

        if (profilsManager == null)
            Debug.LogError("ProfilsManager n'est pas assigné dans le LeaderboardManager.");
    }

    private void BindProfilsManager()
    {
        if (profilsManager == null)
            return;

        profilsManager.ManagementModeChanged -= OnProfileManagementModeChanged;
        profilsManager.CurrentProfileChanged -= OnCurrentProfileChanged;
        profilsManager.ProfileRenamed -= OnProfileRenamed;
        profilsManager.ProfileDeleted -= OnProfileDeleted;

        profilsManager.ManagementModeChanged += OnProfileManagementModeChanged;
        profilsManager.CurrentProfileChanged += OnCurrentProfileChanged;
        profilsManager.ProfileRenamed += OnProfileRenamed;
        profilsManager.ProfileDeleted += OnProfileDeleted;

        profilsManager.SetLeaderboardKeys(GetAllLeaderboardKeys());
        profilsManager.SetOnlineState(simulateOfflineMode, isConnected);

        localPlayerName = profilsManager.CurrentProfileName;
    }

    private void SyncProfilsOnlineState()
    {
        if (profilsManager != null)
            profilsManager.SetOnlineState(simulateOfflineMode, isConnected);
    }

    private void Update()
    {
        if (profilsManager != null && profilsManager.IsManagingProfiles)
            return;

        if (!enableDebugSubmit)
            return;

        if (Input.GetKeyDown(debugSubmitKey))
        {
            int randomScore = UnityEngine.Random.Range(minRandomTimeMs, maxRandomTimeMs + 1);

            Debug.Log($"Score debug envoyé : {FormatTime(randomScore)}");

            SubmitScoreAndRefresh(randomScore);
        }
    }

    // -------------------------------------------------------------------------
    // Public UI methods
    // -------------------------------------------------------------------------

    public void ToggleGameMode()
    {
        LeaderboardGameMode nextGameMode = currentGameMode == LeaderboardGameMode.TimeAttack
            ? LeaderboardGameMode.ContreLaMontre
            : LeaderboardGameMode.TimeAttack;

        SetDisplayedLeaderboard(nextGameMode, currentCircuitMode);
    }

    public void ToggleCircuitMode()
    {
        LeaderboardCircuitMode nextCircuitMode = currentCircuitMode == LeaderboardCircuitMode.Normal
            ? LeaderboardCircuitMode.Reverse
            : LeaderboardCircuitMode.Normal;

        SetDisplayedLeaderboard(currentGameMode, nextCircuitMode);
    }

    public void ShowTimeAttack()
    {
        SetDisplayedLeaderboard(LeaderboardGameMode.TimeAttack, currentCircuitMode);
    }

    public void ShowContreLaMontre()
    {
        SetDisplayedLeaderboard(LeaderboardGameMode.ContreLaMontre, currentCircuitMode);
    }

    public void ShowNormalCircuit()
    {
        SetDisplayedLeaderboard(currentGameMode, LeaderboardCircuitMode.Normal);
    }

    public void ShowReverseCircuit()
    {
        SetDisplayedLeaderboard(currentGameMode, LeaderboardCircuitMode.Reverse);
    }

    public void ShowTimeAttackNormal()
    {
        SetDisplayedLeaderboard(LeaderboardGameMode.TimeAttack, LeaderboardCircuitMode.Normal);
    }

    public void ShowTimeAttackReverse()
    {
        SetDisplayedLeaderboard(LeaderboardGameMode.TimeAttack, LeaderboardCircuitMode.Reverse);
    }

    public void ShowContreLaMontreNormal()
    {
        SetDisplayedLeaderboard(LeaderboardGameMode.ContreLaMontre, LeaderboardCircuitMode.Normal);
    }

    public void ShowContreLaMontreReverse()
    {
        SetDisplayedLeaderboard(LeaderboardGameMode.ContreLaMontre, LeaderboardCircuitMode.Reverse);
    }

    // -------------------------------------------------------------------------
    // Public submit methods to call from your race script
    // -------------------------------------------------------------------------

    public void SubmitTimeAttackNormal(int timeInMilliseconds)
    {
        SubmitScore(
            timeInMilliseconds,
            LeaderboardGameMode.TimeAttack,
            LeaderboardCircuitMode.Normal
        );
    }

    public void SubmitTimeAttackReverse(int timeInMilliseconds)
    {
        SubmitScore(
            timeInMilliseconds,
            LeaderboardGameMode.TimeAttack,
            LeaderboardCircuitMode.Reverse
        );
    }

    public void SubmitContreLaMontreNormal(int timeInMilliseconds)
    {
        SubmitScore(
            timeInMilliseconds,
            LeaderboardGameMode.ContreLaMontre,
            LeaderboardCircuitMode.Normal
        );
    }

    public void SubmitContreLaMontreReverse(int timeInMilliseconds)
    {
        SubmitScore(
            timeInMilliseconds,
            LeaderboardGameMode.ContreLaMontre,
            LeaderboardCircuitMode.Reverse
        );
    }

    public void SubmitScoreAndRefresh(int timeInMilliseconds)
    {
        SubmitScore(timeInMilliseconds, currentGameMode, currentCircuitMode);
    }

    public void RefreshLeaderboard()
    {
        if (simulateOfflineMode)
        {
            isConnected = false;
            SyncProfilsOnlineState();
            BuildOfflineViewFromLocalSave(true);
            return;
        }

        if (!isConnected)
        {
            if (!isStartingSession)
                BuildOfflineViewFromLocalSave(true);

            return;
        }

        if (isLoading)
            return;

        string leaderboardKey = GetCurrentLeaderboardKey();

        if (string.IsNullOrWhiteSpace(leaderboardKey))
        {
            Debug.LogError($"Leaderboard key manquante pour {currentGameMode} / {currentCircuitMode}.");
            BuildOfflineViewFromLocalSave(true);
            return;
        }

        isLoading = true;

        LootLockerSDKManager.GetScoreList(leaderboardKey, maxResults, 0, response =>
        {
            if (!response.success)
            {
                Debug.LogWarning("Impossible de récupérer le leaderboard LootLocker.");

                isConnected = false;
                isLoading = false;
                SyncProfilsOnlineState();

                BuildOfflineViewFromLocalSave(true);

                return;
            }

            SyncEntriesFromServer(response.items ?? Array.Empty<LootLockerLeaderboardMember>());

            ApplyAllLocalScores(currentGameMode, currentCircuitMode, false);

            SortAndRankEntries();
            RenderLeaderboard();

            HideStatus();

            isLoading = false;
        });
    }

    // -------------------------------------------------------------------------
    // Leaderboard state
    // -------------------------------------------------------------------------

    private void SetDisplayedLeaderboard(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        currentGameMode = gameMode;
        currentCircuitMode = circuitMode;

        UpdateModeLabels();

        if (pendingServerRefresh != null)
        {
            StopCoroutine(pendingServerRefresh);
            pendingServerRefresh = null;
        }

        if (simulateOfflineMode)
        {
            isConnected = false;
            BuildOfflineViewFromLocalSave(true);
            return;
        }

        if (isConnected)
        {
            HideStatus();
            RefreshLeaderboard();
            return;
        }

        if (isStartingSession)
        {
            HideStatus();
            return;
        }

        BuildOfflineViewFromLocalSave(true);
        StartLootLockerSession();
    }

    private string GetCurrentLeaderboardKey()
    {
        return GetLeaderboardKey(currentGameMode, currentCircuitMode);
    }

    private string GetLeaderboardKey(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        return gameMode switch
        {
            LeaderboardGameMode.TimeAttack => circuitMode == LeaderboardCircuitMode.Normal
                ? timeAttackNormalKey
                : timeAttackReverseKey,

            LeaderboardGameMode.ContreLaMontre => circuitMode == LeaderboardCircuitMode.Normal
                ? contreLaMontreNormalKey
                : contreLaMontreReverseKey,

            _ => string.Empty
        };
    }

    private void UpdateModeLabels()
    {
        if (profilsManager != null && profilsManager.IsManagingProfiles)
        {
            if (leaderboardTitleText != null)
                leaderboardTitleText.text = "Gérer les joueurs";

            if (circuitModeText != null)
                circuitModeText.text = "Joueur actif : " + GetCurrentPlayerProfileName();

            return;
        }

        string gameModeLabel = currentGameMode == LeaderboardGameMode.TimeAttack
            ? "Time Attack"
            : "Contre la montre";

        string circuitModeLabel = currentCircuitMode == LeaderboardCircuitMode.Normal
            ? "Normal"
            : "Reverse";

        if (leaderboardTitleText != null)
            leaderboardTitleText.text = gameModeLabel;

        if (circuitModeText != null)
            circuitModeText.text = circuitModeLabel;

        if (gameModeToggleButtonText != null)
            gameModeToggleButtonText.text = gameModeLabel;

        if (circuitModeToggleButtonText != null)
            circuitModeToggleButtonText.text = circuitModeLabel;
    }

    // -------------------------------------------------------------------------
    // LootLocker session
    // -------------------------------------------------------------------------

    private void StartLootLockerSession()
    {
        if (simulateOfflineMode)
        {
            isConnected = false;
            isStartingSession = false;
            SyncProfilsOnlineState();

            Debug.LogWarning("Mode hors ligne simulé actif.");

            BuildOfflineViewFromLocalSave(true);
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
                Debug.LogWarning("Impossible de démarrer la session LootLocker.");

                isConnected = false;
                SyncProfilsOnlineState();
                BuildOfflineViewFromLocalSave(true);

                return;
            }

            isConnected = true;
            SyncProfilsOnlineState();
            localPlayerId = response.player_id;

            // Le nom affiché dans le jeu vient du profil local actif.
            // On ne dépend pas du nom LootLocker, car SetPlayerName peut être refusé selon la configuration.
            localPlayerName = GetCurrentPlayerProfileName();
            FinishConnectedSessionSetup();
        });
    }

    private void FinishConnectedSessionSetup()
    {
        HideStatus();

        TryUploadAllPendingLocalScores();
        RefreshLeaderboard();
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
                if (isConnected)
                {
                    isConnected = false;
                    SyncProfilsOnlineState();
                }

                BuildOfflineViewFromLocalSave(true);
                continue;
            }

            if (!isConnected && !isStartingSession)
            {
                StartLootLockerSession();
            }
            else if (isConnected && HasAnyPendingUpload())
            {
                TryUploadAllPendingLocalScores();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Score submit
    // -------------------------------------------------------------------------

    private void SubmitScore(
        int timeInMilliseconds,
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        bool improvedLocalBest = SaveLocalBestScoreIfBetter(timeInMilliseconds, gameMode, circuitMode);

        if (!improvedLocalBest)
        {
            Debug.Log(
                $"Score ignoré pour {gameMode} / {circuitMode} : " +
                $"{FormatTime(timeInMilliseconds)} n'est pas meilleur que le score local."
            );

            if (gameMode == currentGameMode && circuitMode == currentCircuitMode)
                RenderLeaderboard();

            return;
        }

        int localBestScore = GetLocalBestScore(gameMode, circuitMode);

        MarkPendingUpload(localBestScore, gameMode, circuitMode);

        if (gameMode == currentGameMode && circuitMode == currentCircuitMode)
            ApplyLocalScore(localBestScore);

        if (simulateOfflineMode)
        {
            Debug.LogWarning("Score sauvegardé localement. Mode hors ligne simulé actif.");

            isConnected = false;
            SyncProfilsOnlineState();

            if (gameMode == currentGameMode && circuitMode == currentCircuitMode)
                BuildOfflineViewFromLocalSave(true);

            return;
        }

        if (!isConnected)
        {
            Debug.LogWarning("Score sauvegardé localement, mais pas envoyé : pas de connexion LootLocker.");

            if (gameMode == currentGameMode && circuitMode == currentCircuitMode)
                BuildOfflineViewFromLocalSave(true);

            StartLootLockerSession();

            return;
        }

        TryUploadPendingLocalScore(gameMode, circuitMode);
    }

    private void TryUploadAllPendingLocalScores()
    {
        foreach (LeaderboardTarget target in GetAllLeaderboardTargets())
        {
            foreach (PlayerProfile profile in GetProfiles())
            {
                TryUploadPendingLocalScore(profile, target.GameMode, target.CircuitMode);
            }
        }
    }

    private void TryUploadPendingLocalScore(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        if (simulateOfflineMode)
        {
            Debug.LogWarning("Upload ignoré : mode hors ligne simulé actif.");
            return;
        }

        if (!isConnected)
            return;

        TryUploadPendingLocalScore(GetCurrentPlayerProfile(), gameMode, circuitMode);
    }

    private void TryUploadPendingLocalScore(
        PlayerProfile profile,
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        if (profile == null)
            return;

        if (simulateOfflineMode)
        {
            Debug.LogWarning("Upload ignoré : mode hors ligne simulé actif.");
            return;
        }

        if (!isConnected)
            return;

        if (!HasPendingUpload(gameMode, circuitMode, profile.Id))
            return;

        string leaderboardKey = GetLeaderboardKey(gameMode, circuitMode);

        if (string.IsNullOrWhiteSpace(leaderboardKey))
        {
            Debug.LogError($"Upload impossible : leaderboard key manquante pour {gameMode} / {circuitMode}.");
            return;
        }

        int scoreToUpload = GetPendingUploadScore(gameMode, circuitMode, profile.Id);
        string metadata = JsonUtility.ToJson(new ScoreMetadata(profile.Id, profile.Name));

        LootLockerSDKManager.SubmitScore(GetServerMemberIdForProfile(profile), scoreToUpload, leaderboardKey, metadata, scoreResponse =>
        {
            if (!scoreResponse.success)
            {
                Debug.LogWarning(
                    $"Impossible d'envoyer le score {gameMode} / {circuitMode} au leaderboard LootLocker. " +
                    "Il reste sauvegardé localement."
                );

                return;
            }

            Debug.Log(
                $"Score synchronisé avec LootLocker pour {profile.Name} / {gameMode} / {circuitMode} : " +
                FormatTime(scoreToUpload)
            );

            ClearPendingUpload(gameMode, circuitMode, profile.Id);

            if (gameMode == currentGameMode && circuitMode == currentCircuitMode)
            {
                if (pendingServerRefresh != null)
                    StopCoroutine(pendingServerRefresh);

                pendingServerRefresh = StartCoroutine(RefreshLeaderboardAfterDelay());
            }
        });
    }

    private IEnumerator RefreshLeaderboardAfterDelay()
    {
        yield return new WaitForSeconds(serverRefreshDelay);

        pendingServerRefresh = null;
        RefreshLeaderboard();
    }

    // -------------------------------------------------------------------------
    // Offline/local display
    // -------------------------------------------------------------------------

    private void BuildOfflineViewFromLocalSave(bool showOfflineStatus)
    {
        leaderboardEntries.Clear();

        bool hasLocalBestScore = HasAnyLocalBestScore(currentGameMode, currentCircuitMode);
        ApplyAllLocalScores(currentGameMode, currentCircuitMode, false);

        if (showOfflineStatus)
        {
            ShowStatus(hasLocalBestScore ? offlineWithScoreMessage : offlineWithoutScoreMessage);
        }
        else
        {
            HideStatus();
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
            string memberId = GetLeaderboardEntryIdentity(item);
            string displayName = GetDisplayName(item);
            bool isLocalPlayerEntry = IsCurrentProfileLeaderboardEntry(item);
            int existingIndex = leaderboardEntries.FindIndex(entry => entry.MemberId == memberId);

            if (existingIndex >= 0)
            {
                LeaderboardDisplayEntry existingEntry = leaderboardEntries[existingIndex];

                if (item.score < existingEntry.Score)
                {
                    existingEntry.PlayerId = playerId;
                    existingEntry.PlayerName = displayName;
                    existingEntry.Score = item.score;
                }

                existingEntry.IsLocalPlayer = existingEntry.IsLocalPlayer || isLocalPlayerEntry;
                leaderboardEntries[existingIndex] = existingEntry;
                continue;
            }

            leaderboardEntries.Add(new LeaderboardDisplayEntry(
                playerId,
                memberId,
                displayName,
                item.score,
                item.rank,
                isLocalPlayerEntry
            ));
        }
    }

    private void ApplyLocalScore(int timeInMilliseconds, bool renderAfterApply = true)
    {
        ApplyLocalProfileScore(GetCurrentPlayerProfile(), timeInMilliseconds, renderAfterApply);
    }

    private void ApplyAllLocalScores(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        bool renderAfterApply = true
    )
    {
        foreach (PlayerProfile profile in GetProfiles())
        {
            if (profile == null || !HasLocalBestScore(gameMode, circuitMode, profile.Id))
                continue;

            ApplyLocalProfileScore(profile, GetLocalBestScore(gameMode, circuitMode, profile.Id), false);
        }

        SortAndRankEntries();

        if (renderAfterApply)
            RenderLeaderboard();
    }

    private void ApplyLocalProfileScore(PlayerProfile profile, int timeInMilliseconds, bool renderAfterApply = true)
    {
        if (profile == null)
            return;

        string currentMemberId = GetServerMemberIdForProfile(profile);
        int existingIndex = leaderboardEntries.FindIndex(entry =>
            entry.MemberId == profile.Id ||
            ArePlayerNamesEquivalent(entry.MemberId, currentMemberId) ||
            (string.IsNullOrWhiteSpace(entry.MemberId) && entry.IsLocalPlayer && profile.Id == GetCurrentPlayerProfileId())
        );

        bool isCurrentProfile = profile.Id == GetCurrentPlayerProfileId();

        if (existingIndex >= 0)
        {
            LeaderboardDisplayEntry existingEntry = leaderboardEntries[existingIndex];

            if (existingEntry.Score <= timeInMilliseconds)
            {
                existingEntry.PlayerId = localPlayerId;
                existingEntry.MemberId = currentMemberId;
                existingEntry.PlayerName = profile.Name;
                existingEntry.IsLocalPlayer = isCurrentProfile;

                leaderboardEntries[existingIndex] = existingEntry;

                if (renderAfterApply)
                    RenderLeaderboard();

                return;
            }

            existingEntry.PlayerId = localPlayerId;
            existingEntry.MemberId = currentMemberId;
            existingEntry.Score = timeInMilliseconds;
            existingEntry.PlayerName = profile.Name;
            existingEntry.IsLocalPlayer = isCurrentProfile;

            leaderboardEntries[existingIndex] = existingEntry;
        }
        else
        {
            leaderboardEntries.Add(new LeaderboardDisplayEntry(
                localPlayerId,
                currentMemberId,
                profile.Name,
                timeInMilliseconds,
                0,
                isCurrentProfile
            ));
        }

        SortAndRankEntries();

        if (renderAfterApply)
            RenderLeaderboard();
    }

    // -------------------------------------------------------------------------
    // PlayerPrefs per leaderboard
    // -------------------------------------------------------------------------

    private bool SaveLocalBestScoreIfBetter(
        int timeInMilliseconds,
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        if (!HasLocalBestScore(gameMode, circuitMode))
        {
            SaveLocalBestScore(timeInMilliseconds, gameMode, circuitMode);
            return true;
        }

        int currentBest = GetLocalBestScore(gameMode, circuitMode);

        if (timeInMilliseconds >= currentBest)
            return false;

        SaveLocalBestScore(timeInMilliseconds, gameMode, circuitMode);
        return true;
    }

    private void SaveLocalBestScore(
        int timeInMilliseconds,
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        PlayerPrefs.SetInt(GetHasLocalBestScoreKey(gameMode, circuitMode), 1);
        PlayerPrefs.SetInt(GetLocalBestScoreKey(gameMode, circuitMode), timeInMilliseconds);
        PlayerPrefs.Save();

        Debug.Log(
            $"Meilleur score local sauvegardé pour {gameMode} / {circuitMode} : " +
            FormatTime(timeInMilliseconds)
        );
    }

    private bool HasLocalBestScore(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        return HasLocalBestScore(gameMode, circuitMode, GetCurrentPlayerProfileId());
    }

    private bool HasLocalBestScore(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        string playerProfileId
    )
    {
        return PlayerPrefs.GetInt(GetHasLocalBestScoreKey(gameMode, circuitMode, playerProfileId), 0) == 1;
    }

    private bool HasAnyLocalBestScore(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        foreach (PlayerProfile profile in GetProfiles())
        {
            if (profile != null && HasLocalBestScore(gameMode, circuitMode, profile.Id))
                return true;
        }

        return false;
    }

    private int GetLocalBestScore(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        return GetLocalBestScore(gameMode, circuitMode, GetCurrentPlayerProfileId());
    }

    private int GetLocalBestScore(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        string playerProfileId
    )
    {
        return PlayerPrefs.GetInt(GetLocalBestScoreKey(gameMode, circuitMode, playerProfileId), int.MaxValue);
    }

    private void MarkPendingUpload(
        int timeInMilliseconds,
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        MarkPendingUpload(timeInMilliseconds, gameMode, circuitMode, GetCurrentPlayerProfileId());
    }

    private void MarkPendingUpload(
        int timeInMilliseconds,
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        string playerProfileId
    )
    {
        PlayerPrefs.SetInt(GetHasPendingUploadKey(gameMode, circuitMode, playerProfileId), 1);
        PlayerPrefs.SetInt(GetPendingUploadScoreKey(gameMode, circuitMode, playerProfileId), timeInMilliseconds);
        PlayerPrefs.Save();

        Debug.Log(
            $"Score marqué comme en attente pour {gameMode} / {circuitMode} : " +
            FormatTime(timeInMilliseconds)
        );
    }

    private void MarkAllLocalScoresPendingUpload(PlayerProfile profile)
    {
        if (profile == null)
            return;

        foreach (LeaderboardTarget target in GetAllLeaderboardTargets())
        {
            if (!HasLocalBestScore(target.GameMode, target.CircuitMode, profile.Id))
                continue;

            MarkPendingUpload(
                GetLocalBestScore(target.GameMode, target.CircuitMode, profile.Id),
                target.GameMode,
                target.CircuitMode,
                profile.Id
            );
        }
    }

    private bool HasAnyPendingUpload()
    {
        foreach (LeaderboardTarget target in GetAllLeaderboardTargets())
        {
            if (HasPendingUpload(target.GameMode, target.CircuitMode))
                return true;
        }

        return false;
    }

    private bool HasPendingUpload(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        return HasPendingUpload(gameMode, circuitMode, GetCurrentPlayerProfileId());
    }

    private bool HasPendingUpload(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        string playerProfileId
    )
    {
        return PlayerPrefs.GetInt(GetHasPendingUploadKey(gameMode, circuitMode, playerProfileId), 0) == 1;
    }

    private int GetPendingUploadScore(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        return GetPendingUploadScore(gameMode, circuitMode, GetCurrentPlayerProfileId());
    }

    private int GetPendingUploadScore(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        string playerProfileId
    )
    {
        return PlayerPrefs.GetInt(GetPendingUploadScoreKey(gameMode, circuitMode, playerProfileId), int.MaxValue);
    }

    private void ClearPendingUpload(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        ClearPendingUpload(gameMode, circuitMode, GetCurrentPlayerProfileId());
    }

    private void ClearPendingUpload(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        string playerProfileId
    )
    {
        PlayerPrefs.DeleteKey(GetHasPendingUploadKey(gameMode, circuitMode, playerProfileId));
        PlayerPrefs.DeleteKey(GetPendingUploadScoreKey(gameMode, circuitMode, playerProfileId));
        PlayerPrefs.Save();
    }

    private string GetLocalBestScoreKey(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        return GetLocalBestScoreKey(gameMode, circuitMode, GetCurrentPlayerProfileId());
    }

    private string GetHasLocalBestScoreKey(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        return GetHasLocalBestScoreKey(gameMode, circuitMode, GetCurrentPlayerProfileId());
    }

    private string GetPendingUploadScoreKey(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        return GetPendingUploadScoreKey(gameMode, circuitMode, GetCurrentPlayerProfileId());
    }

    private string GetHasPendingUploadKey(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        return GetHasPendingUploadKey(gameMode, circuitMode, GetCurrentPlayerProfileId());
    }

    private IEnumerable<LeaderboardTarget> GetAllLeaderboardTargets()
    {
        yield return new LeaderboardTarget(LeaderboardGameMode.TimeAttack, LeaderboardCircuitMode.Normal);
        yield return new LeaderboardTarget(LeaderboardGameMode.TimeAttack, LeaderboardCircuitMode.Reverse);
        yield return new LeaderboardTarget(LeaderboardGameMode.ContreLaMontre, LeaderboardCircuitMode.Normal);
        yield return new LeaderboardTarget(LeaderboardGameMode.ContreLaMontre, LeaderboardCircuitMode.Reverse);
    }

    private IEnumerable<string> GetAllLeaderboardKeys()
    {
        foreach (LeaderboardTarget target in GetAllLeaderboardTargets())
        {
            string key = GetLeaderboardKey(target.GameMode, target.CircuitMode);

            if (!string.IsNullOrWhiteSpace(key))
                yield return key;
        }
    }

    private string GetLocalBestScoreKey(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        string playerProfileId
    )
    {
        return $"{LocalBestScoreKeyPrefix}_{playerProfileId}_{gameMode}_{circuitMode}";
    }

    private string GetHasLocalBestScoreKey(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        string playerProfileId
    )
    {
        return $"{HasLocalBestScoreKeyPrefix}_{playerProfileId}_{gameMode}_{circuitMode}";
    }

    private string GetPendingUploadScoreKey(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        string playerProfileId
    )
    {
        return $"{PendingUploadScoreKeyPrefix}_{playerProfileId}_{gameMode}_{circuitMode}";
    }

    private string GetHasPendingUploadKey(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        string playerProfileId
    )
    {
        return $"{HasPendingUploadKeyPrefix}_{playerProfileId}_{gameMode}_{circuitMode}";
    }

    // -------------------------------------------------------------------------
    // Profiles integration
    // -------------------------------------------------------------------------

    private void OnProfileManagementModeChanged(bool isManagingProfiles)
    {
        if (isManagingProfiles)
        {
            if (pendingServerRefresh != null)
            {
                StopCoroutine(pendingServerRefresh);
                pendingServerRefresh = null;
            }

            HideStatus();
            HideScorePages();
            HideNavigationButtons();

            if (hideModeButtonsWhileManagingPlayers)
            {
                if (gameModeToggleButton != null)
                    gameModeToggleButton.gameObject.SetActive(false);

                if (circuitModeToggleButton != null)
                    circuitModeToggleButton.gameObject.SetActive(false);
            }
        }
        else
        {
            if (gameModeToggleButton != null)
                gameModeToggleButton.gameObject.SetActive(true);

            if (circuitModeToggleButton != null)
                circuitModeToggleButton.gameObject.SetActive(true);

            RefreshLeaderboard();
        }

        UpdateModeLabels();
    }

    private void OnCurrentProfileChanged()
    {
        localPlayerName = GetCurrentPlayerProfileName();
        UpdateModeLabels();
        RefreshLeaderboard();
    }

    private void OnProfileRenamed(PlayerProfile profile)
    {
        MarkAllLocalScoresPendingUpload(profile);
        TryUploadAllPendingLocalScores();
        RefreshLeaderboard();
    }

    private void OnProfileDeleted(string profileId)
    {
        DeleteLocalSavesForProfile(profileId);
        RefreshLeaderboard();
    }

    private void HideScorePages()
    {
        foreach (LeaderboardPage page in pages)
        {
            if (page != null)
                page.gameObject.SetActive(false);
        }

        if (firstPage != null)
            firstPage.gameObject.SetActive(false);

        foreach (LeaderboardPage page in spawnedNormalPages)
        {
            if (page != null)
                page.gameObject.SetActive(false);
        }
    }

    private IEnumerable<PlayerProfile> GetProfiles()
    {
        return profilsManager != null
            ? profilsManager.Profiles
            : Array.Empty<PlayerProfile>();
    }

    private PlayerProfile GetCurrentPlayerProfile()
    {
        return profilsManager != null
            ? profilsManager.GetProfile(profilsManager.CurrentProfileId)
            : null;
    }

    private string GetCurrentPlayerProfileId()
    {
        return profilsManager != null ? profilsManager.CurrentProfileId : string.Empty;
    }

    private string GetCurrentPlayerProfileName()
    {
        return profilsManager != null ? profilsManager.CurrentProfileName : localPlayerName;
    }

    private string GetServerMemberIdForCurrentProfile()
    {
        return profilsManager != null ? profilsManager.GetServerMemberIdForCurrentProfile() : GetCurrentPlayerProfileId();
    }

    private string GetServerMemberIdForProfile(PlayerProfile profile)
    {
        if (profilsManager == null)
            return profile != null ? profile.Id : string.Empty;

        return profilsManager.GetServerMemberIdForProfile(profile);
    }

    private void DeleteLocalSavesForProfile(string playerProfileId)
    {
        foreach (LeaderboardTarget target in GetAllLeaderboardTargets())
        {
            PlayerPrefs.DeleteKey(GetLocalBestScoreKey(target.GameMode, target.CircuitMode, playerProfileId));
            PlayerPrefs.DeleteKey(GetHasLocalBestScoreKey(target.GameMode, target.CircuitMode, playerProfileId));
            PlayerPrefs.DeleteKey(GetPendingUploadScoreKey(target.GameMode, target.CircuitMode, playerProfileId));
            PlayerPrefs.DeleteKey(GetHasPendingUploadKey(target.GameMode, target.CircuitMode, playerProfileId));
        }

        PlayerPrefs.Save();
    }

    // -------------------------------------------------------------------------
    // Render
    // -------------------------------------------------------------------------

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
        if (profilsManager != null && profilsManager.IsManagingProfiles)
            return;

        if (firstPage == null)
        {
            Debug.LogError("FirstPage n'est pas assignée dans le LeaderboardManager.");
            return;
        }

        ClearSpawnedNormalPages();

        pages.Clear();
        pages.Add(firstPage);

        int totalEntries = leaderboardEntries.Count;
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(totalEntries / (float)EntriesPerPage));

        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            LeaderboardPage page;

            if (pageIndex == 0)
            {
                page = firstPage;
            }
            else
            {
                page = CreateNormalPage();
            }

            if (page == null)
                continue;

            if (pageIndex > 0)
                pages.Add(page);

            FillPage(page, pageIndex);
        }

        if (startOnFirstPageAfterRefresh)
            currentPageIndex = 0;
        else
            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, pages.Count - 1);

        ShowPage(currentPageIndex);
    }

    private LeaderboardPage CreateNormalPage()
    {
        if (normalPagePrefab == null)
        {
            Debug.LogError("NormalPagePrefab n'est pas assigné dans le LeaderboardManager.");
            return null;
        }

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

    private void ShowPreviousPage()
    {
        if (currentPageIndex <= 0)
            return;

        ShowPage(currentPageIndex - 1);
    }

    private void ShowNextPage()
    {
        if (currentPageIndex >= pages.Count - 1)
            return;

        ShowPage(currentPageIndex + 1);
    }

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
    }

    private void UpdateNavigationButtons()
    {
        bool hasPreviousPage = currentPageIndex > 0;
        bool hasNextPage = currentPageIndex < pages.Count - 1;

        if (previousPageButton != null)
            previousPageButton.gameObject.SetActive(hasPreviousPage);

        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(hasNextPage);

        ConfigureControllerNavigation();
    }

    private void HideNavigationButtons()
    {
        if (previousPageButton != null)
            previousPageButton.gameObject.SetActive(false);

        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(false);

        ConfigureControllerNavigation();
    }

    public void ForceFocusOnNextButton()
    {
        ConfigureControllerNavigation();

        if (nextPageButton != null && nextPageButton.gameObject.activeInHierarchy)
        {
            nextPageButton.Select();
            return;
        }

        if (previousPageButton != null && previousPageButton.gameObject.activeInHierarchy)
        {
            previousPageButton.Select();
            return;
        }

        if (circuitModeToggleButton != null && circuitModeToggleButton.gameObject.activeInHierarchy)
        {
            circuitModeToggleButton.Select();
            return;
        }

        if (gameModeToggleButton != null && gameModeToggleButton.gameObject.activeInHierarchy)
        {
            gameModeToggleButton.Select();
        }
    }

    private void ConfigureControllerNavigation()
    {
        SetAutomaticNavigation(previousPageButton);
        SetAutomaticNavigation(nextPageButton);
        SetAutomaticNavigation(gameModeToggleButton);
        SetAutomaticNavigation(circuitModeToggleButton);
    }

    private static void SetAutomaticNavigation(Selectable selectable)
    {
        if (selectable == null)
            return;

        Navigation navigation = selectable.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        selectable.navigation = navigation;
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

    // -------------------------------------------------------------------------
    // Utils
    // -------------------------------------------------------------------------

    private string GetDisplayName(LootLockerLeaderboardMember item)
    {
        ScoreMetadata metadata = GetScoreMetadata(item);

        if (!string.IsNullOrWhiteSpace(metadata?.ProfileName))
            return metadata.ProfileName;

        PlayerProfile localProfile = GetPlayerProfileForLeaderboardEntry(item);

        if (localProfile != null)
            return localProfile.Name;

        string memberId = GetLeaderboardMemberId(item);

        if (!string.IsNullOrWhiteSpace(memberId) && !IsLikelyLootLockerNumericId(memberId))
            return memberId.Replace("_", " ");

        if (item.player == null)
            return "Unknown";

        if (!string.IsNullOrWhiteSpace(item.player.name))
            return item.player.name;

        string publicUid = TryGetStringMember(item.player, "public_uid");

        if (!string.IsNullOrWhiteSpace(publicUid))
            return GeneratePlayerName(publicUid, item.player.id);

        return GeneratePlayerName(null, item.player.id);
    }

    private string GetLeaderboardMemberId(LootLockerLeaderboardMember item)
    {
        return TryGetStringMember(item, "member_id");
    }

    private string GetLeaderboardEntryIdentity(LootLockerLeaderboardMember item)
    {
        ScoreMetadata metadata = GetScoreMetadata(item);

        if (!string.IsNullOrWhiteSpace(metadata?.ProfileId))
            return metadata.ProfileId;

        PlayerProfile localProfile = GetPlayerProfileForLeaderboardEntry(item);

        if (localProfile != null)
            return localProfile.Id;

        return GetLeaderboardMemberId(item);
    }

    private bool IsCurrentProfileLeaderboardEntry(LootLockerLeaderboardMember item)
    {
        ScoreMetadata metadata = GetScoreMetadata(item);

        if (!string.IsNullOrWhiteSpace(metadata?.ProfileId))
            return metadata.ProfileId == GetCurrentPlayerProfileId();

        PlayerProfile localProfile = GetPlayerProfileForLeaderboardEntry(item);

        if (localProfile != null)
            return localProfile.Id == GetCurrentPlayerProfileId();

        return ArePlayerNamesEquivalent(GetLeaderboardMemberId(item), GetServerMemberIdForCurrentProfile());
    }

    private PlayerProfile GetPlayerProfileForLeaderboardEntry(LootLockerLeaderboardMember item)
    {
        if (item == null)
            return null;

        ScoreMetadata metadata = GetScoreMetadata(item);

        if (!string.IsNullOrWhiteSpace(metadata?.ProfileId))
            return profilsManager != null ? profilsManager.GetProfile(metadata.ProfileId) : null;

        return profilsManager != null ? profilsManager.GetProfileByMemberId(GetLeaderboardMemberId(item)) : null;
    }

    private ScoreMetadata GetScoreMetadata(LootLockerLeaderboardMember item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.metadata))
            return null;

        try
        {
            return JsonUtility.FromJson<ScoreMetadata>(item.metadata);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private bool IsLikelyLootLockerNumericId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]))
                return false;
        }

        return true;
    }

    private bool ArePlayerNamesEquivalent(string left, string right)
    {
        return profilsManager != null
            ? profilsManager.ArePlayerNamesEquivalent(left, right)
            : string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private string GeneratePlayerName(string uniqueSource, int playerId)
    {
        string source = string.IsNullOrWhiteSpace(uniqueSource)
            ? playerId.ToString()
            : uniqueSource;

        return "Player_" + GetShortCode(source);
    }

    private string GetShortCode(string source)
    {
        const int CodeLength = 5;

        if (string.IsNullOrWhiteSpace(source))
            return new string('0', CodeLength);

        string cleanSource = source
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Trim();

        if (cleanSource.Length == 0)
            return new string('0', CodeLength);

        if (cleanSource.Length >= CodeLength)
            return cleanSource.Substring(0, CodeLength).ToUpperInvariant();

        return cleanSource.PadRight(CodeLength, '0').ToUpperInvariant();
    }

    private string TryGetStringMember(object target, string memberName)
    {
        if (target == null)
            return string.Empty;

        Type targetType = target.GetType();

        System.Reflection.PropertyInfo propertyInfo = targetType.GetProperty(memberName);
        if (propertyInfo != null && propertyInfo.PropertyType == typeof(string))
            return propertyInfo.GetValue(target) as string ?? string.Empty;

        System.Reflection.FieldInfo fieldInfo = targetType.GetField(memberName);
        if (fieldInfo != null && fieldInfo.FieldType == typeof(string))
            return fieldInfo.GetValue(target) as string ?? string.Empty;

        return string.Empty;
    }

    private void ShowStatus(string message)
    {
        if (statusMessageRoot != null)
            statusMessageRoot.SetActive(true);

        if (statusMessageText != null)
            statusMessageText.text = message;
    }

    private void HideStatus()
    {
        if (statusMessageRoot != null)
            statusMessageRoot.SetActive(false);

        if (statusMessageText != null)
            statusMessageText.text = string.Empty;
    }

    private string FormatTime(int milliseconds)
    {
        int min = milliseconds / 60000;
        int sec = milliseconds / 1000 % 60;
        int ms = milliseconds % 1000;

        return $"{min:00}:{sec:00}.{ms:000}";
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Clear All Local Leaderboard Saves")]
    private void ClearAllLocalLeaderboardSaves()
    {
        foreach (LeaderboardTarget target in GetAllLeaderboardTargets())
        {
            foreach (PlayerProfile profile in GetProfiles())
            {
                PlayerPrefs.DeleteKey(GetLocalBestScoreKey(target.GameMode, target.CircuitMode, profile.Id));
                PlayerPrefs.DeleteKey(GetHasLocalBestScoreKey(target.GameMode, target.CircuitMode, profile.Id));
                PlayerPrefs.DeleteKey(GetPendingUploadScoreKey(target.GameMode, target.CircuitMode, profile.Id));
                PlayerPrefs.DeleteKey(GetHasPendingUploadKey(target.GameMode, target.CircuitMode, profile.Id));
            }
        }

        PlayerPrefs.Save();

        Debug.Log("Toutes les sauvegardes locales du leaderboard ont été supprimées.");

        BuildOfflineViewFromLocalSave(true);
    }
#endif
}
