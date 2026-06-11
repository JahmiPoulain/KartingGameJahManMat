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
    private const int MaxPlayerProfiles = 10;
    private const int MaxPlayerNameLength = 12;
    private const int RemoteNameCheckBatchSize = 100;

    private const string PlayerProfilesJsonKey = "Leaderboard_PlayerProfilesJson";
    private const string CurrentPlayerProfileIdKey = "Leaderboard_CurrentPlayerProfileId";

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

    [Header("Player Management")]
    [SerializeField] private Button managePlayersButton;
    [SerializeField] private TextMeshProUGUI managePlayersButtonText;
    [SerializeField] private PlayerManagementPage playerManagementPage;
    [SerializeField] private bool hideModeButtonsWhileManagingPlayers = true;
    [SerializeField] private string managePlayersButtonLabel = "Gérer les joueurs";
    [SerializeField] private string backToLeaderboardButtonLabel = "Retour classement";

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

    private readonly List<PlayerProfile> playerProfiles = new();
    private string currentPlayerProfileId;
    private string selectedPlayerProfileId;
    private bool isPlayerManagementMode;
    private bool isWaitingDeleteConfirmation;

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

    [Serializable]
    private class PlayerProfile
    {
        public string Id;
        public string Name;
        public List<string> PreviousMemberIds = new();

        public PlayerProfile(string id, string name)
        {
            Id = id;
            Name = name;
            PreviousMemberIds = new List<string>();
        }
    }

    [Serializable]
    private class PlayerProfileCollection
    {
        public List<PlayerProfile> Players = new();
        public string CurrentPlayerId;
    }

    [Serializable]
    private class ScoreMetadata
    {
        public string ProfileId;
        public string ProfileName;

        public ScoreMetadata(string profileId, string profileName)
        {
            ProfileId = profileId;
            ProfileName = profileName;
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
        localPlayerName = fallbackLocalPlayerName;
    }

    private void OnEnable()
    {
        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(ShowPreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(ShowNextPage);

        if (gameModeToggleButton != null)
            gameModeToggleButton.onClick.AddListener(ToggleGameMode);

        if (circuitModeToggleButton != null)
            circuitModeToggleButton.onClick.AddListener(ToggleCircuitMode);

        if (managePlayersButton != null)
            managePlayersButton.onClick.AddListener(TogglePlayerManagementMode);

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

        if (managePlayersButton != null)
            managePlayersButton.onClick.RemoveListener(TogglePlayerManagementMode);

    }

    private void Start()
    {
        InitializePlayerProfiles();
        ConfigurePlayerManagementPage();
        HidePlayerManagementObjects();
        HideNavigationButtons();
        UpdateModeLabels();
        UpdatePlayerManagementControls();
        HideStatus();

        StartLootLockerSession();
        StartReconnectLoop();
    }

    private void Update()
    {
        if (isPlayerManagementMode)
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

    public void TogglePlayerManagementMode()
    {
        if (isPlayerManagementMode)
            HidePlayerManagement();
        else
            ShowPlayerManagement();
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
        if (isPlayerManagementMode)
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
                BuildOfflineViewFromLocalSave(true);

                return;
            }

            isConnected = true;
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
                    isConnected = false;

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
            foreach (PlayerProfile profile in playerProfiles)
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

        TryUploadPendingLocalScore(GetPlayerProfile(GetCurrentPlayerProfileId()), gameMode, circuitMode);
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
        ApplyLocalProfileScore(GetPlayerProfile(GetCurrentPlayerProfileId()), timeInMilliseconds, renderAfterApply);
    }

    private void ApplyAllLocalScores(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        bool renderAfterApply = true
    )
    {
        foreach (PlayerProfile profile in playerProfiles)
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
        foreach (PlayerProfile profile in playerProfiles)
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
    // Player profiles
    // -------------------------------------------------------------------------

    private void InitializePlayerProfiles()
    {
        playerProfiles.Clear();

        string json = PlayerPrefs.GetString(PlayerProfilesJsonKey, string.Empty);

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                PlayerProfileCollection collection = JsonUtility.FromJson<PlayerProfileCollection>(json);

                if (collection?.Players != null)
                {
                    foreach (PlayerProfile profile in collection.Players)
                    {
                        if (profile == null)
                            continue;

                        profile.Id = string.IsNullOrWhiteSpace(profile.Id) ? GeneratePlayerProfileId() : profile.Id;
                        profile.Name = SanitizePlayerName(profile.Name);
                        profile.PreviousMemberIds ??= new List<string>();

                        if (!string.IsNullOrWhiteSpace(profile.Name) && playerProfiles.Count < MaxPlayerProfiles)
                            playerProfiles.Add(profile);
                    }

                    if (!string.IsNullOrWhiteSpace(collection.CurrentPlayerId))
                        currentPlayerProfileId = collection.CurrentPlayerId;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Impossible de charger les profils joueurs : " + exception.Message);
            }
        }

        if (playerProfiles.Count == 0)
            playerProfiles.Add(new PlayerProfile(GeneratePlayerProfileId(), CreateUniqueDefaultPlayerName()));

        string savedCurrentId = PlayerPrefs.GetString(CurrentPlayerProfileIdKey, currentPlayerProfileId);

        if (HasPlayerProfile(savedCurrentId))
            currentPlayerProfileId = savedCurrentId;

        if (!HasPlayerProfile(currentPlayerProfileId))
            currentPlayerProfileId = playerProfiles[0].Id;

        selectedPlayerProfileId = currentPlayerProfileId;
        localPlayerName = GetCurrentPlayerProfileName();

        SavePlayerProfiles();

    }

    private void ShowPlayerManagement()
    {
        isPlayerManagementMode = true;
        isWaitingDeleteConfirmation = false;
        selectedPlayerProfileId = currentPlayerProfileId;

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

        ConfigurePlayerManagementPage();

        if (playerManagementPage != null)
        {
            playerManagementPage.gameObject.SetActive(true);
            playerManagementPage.CloseAllPopups();
        }


        UpdateModeLabels();
        UpdatePlayerManagementControls();
        RenderPlayerManagement();
    }

    private void HidePlayerManagement(bool refreshLeaderboard = true)
    {
        isPlayerManagementMode = false;
        isWaitingDeleteConfirmation = false;

        HidePlayerManagementObjects();

        if (gameModeToggleButton != null)
            gameModeToggleButton.gameObject.SetActive(true);

        if (circuitModeToggleButton != null)
            circuitModeToggleButton.gameObject.SetActive(true);

        SetPlayerManagementStatus(string.Empty);
        UpdateModeLabels();
        UpdatePlayerManagementControls();

        if (refreshLeaderboard)
            RefreshLeaderboard();
    }

    private void RenderPlayerManagement()
    {
        if (!isPlayerManagementMode)
            return;

        if (playerManagementPage == null)
        {
            Debug.LogError("PlayerManagementPage n'est pas assignée dans le LeaderboardManager.");
            return;
        }

        playerManagementPage.gameObject.SetActive(true);
        playerManagementPage.ClearPage();
        playerManagementPage.SetCanAddPlayer(playerProfiles.Count < MaxPlayerProfiles);

        for (int i = 0; i < MaxPlayerProfiles; i++)
        {
            if (i >= playerProfiles.Count)
                continue;

            PlayerProfile profile = playerProfiles[i];
            bool isCurrent = profile.Id == currentPlayerProfileId;

            playerManagementPage.SetPlayer(
                i,
                profile.Id,
                profile.Name,
                isCurrent
            );
        }
    }

    private void ConfigurePlayerManagementPage()
    {
        if (playerManagementPage == null)
            return;

        playerManagementPage.SetMaxPlayerNameLength(MaxPlayerNameLength);
        playerManagementPage.SetCallbacks(
            AddPlayerFromPlayerPage,
            RenamePlayerFromPlayerPage,
            DeletePlayerFromPlayerPage,
            SelectPlayerProfile,
            ValidatePlayerNameFromPlayerPage
        );
    }

    private void AddPlayerFromPlayerPage(string rawPlayerName)
    {
        string playerName = SanitizePlayerName(rawPlayerName);

        if (string.IsNullOrWhiteSpace(playerName))
        {
            SetPlayerManagementStatus("Nom invalide.");
            return;
        }

        if (playerProfiles.Count >= MaxPlayerProfiles)
        {
            SetPlayerManagementStatus($"Maximum {MaxPlayerProfiles} joueurs.");
            return;
        }

        if (IsPlayerNameAlreadyUsed(playerName))
        {
            SetPlayerManagementStatus("Ce nom existe déjà.");
            return;
        }

        PlayerProfile profile = new PlayerProfile(GeneratePlayerProfileId(), playerName);
        playerProfiles.Add(profile);

        SetCurrentPlayerProfile(profile.Id);
        selectedPlayerProfileId = profile.Id;
        isWaitingDeleteConfirmation = false;

        SavePlayerProfiles();
        RenderPlayerManagement();
        UpdatePlayerManagementControls();
        SetPlayerManagementStatus("Joueur ajouté.");
    }

    private void RenamePlayerFromPlayerPage(string profileId, string rawPlayerName)
    {
        PlayerProfile profile = GetPlayerProfile(profileId);

        if (profile == null)
        {
            SetPlayerManagementStatus("Joueur introuvable.");
            return;
        }

        string playerName = SanitizePlayerName(rawPlayerName);

        if (string.IsNullOrWhiteSpace(playerName))
        {
            SetPlayerManagementStatus("Nom invalide.");
            return;
        }

        if (IsPlayerNameAlreadyUsed(playerName, profile.Id))
        {
            SetPlayerManagementStatus("Ce nom existe déjà.");
            return;
        }

        string previousMemberId = GetServerMemberIdForProfile(profile);
        AddPreviousMemberId(profile, previousMemberId);

        profile.Name = playerName;

        if (profile.Id == currentPlayerProfileId)
            localPlayerName = profile.Name;

        selectedPlayerProfileId = profile.Id;
        isWaitingDeleteConfirmation = false;

        MarkAllLocalScoresPendingUpload(profile);

        SavePlayerProfiles();
        RenderPlayerManagement();
        TryUploadAllPendingLocalScores();
        UpdatePlayerManagementControls();
        SetPlayerManagementStatus("Nom modifié.");
    }

    private void ValidatePlayerNameFromPlayerPage(string rawPlayerName, string ignoredProfileId, Action<bool> onValidated)
    {
        string playerName = SanitizePlayerName(rawPlayerName);

        if (string.IsNullOrWhiteSpace(playerName) || IsPlayerNameAlreadyUsed(playerName, ignoredProfileId))
        {
            onValidated?.Invoke(false);
            return;
        }

        if (!string.IsNullOrWhiteSpace(ignoredProfileId) &&
            ArePlayerNamesEquivalent(playerName, GetPlayerProfileName(ignoredProfileId)))
        {
            onValidated?.Invoke(true);
            return;
        }

        StartCoroutine(ValidatePlayerNameRemotely(playerName, onValidated));
    }

    private IEnumerator ValidatePlayerNameRemotely(string playerName, Action<bool> onValidated)
    {
        if (simulateOfflineMode || !isConnected)
        {
            onValidated?.Invoke(true);
            yield break;
        }

        foreach (LeaderboardTarget target in GetAllLeaderboardTargets())
        {
            string leaderboardKey = GetLeaderboardKey(target.GameMode, target.CircuitMode);

            if (string.IsNullOrWhiteSpace(leaderboardKey))
                continue;

            int offset = 0;
            bool shouldContinue = true;

            while (shouldContinue)
            {
                bool requestDone = false;
                bool requestSucceeded = false;
                bool nameExists = false;
                int itemCount = 0;

                LootLockerSDKManager.GetScoreList(leaderboardKey, RemoteNameCheckBatchSize, offset, response =>
                {
                    requestDone = true;
                    requestSucceeded = response.success;

                    LootLockerLeaderboardMember[] items = response.items ?? Array.Empty<LootLockerLeaderboardMember>();
                    itemCount = items.Length;

                    foreach (LootLockerLeaderboardMember item in items)
                    {
                        if (IsRemotePlayerNameMatch(item, playerName))
                        {
                            nameExists = true;
                            break;
                        }
                    }
                });

                yield return new WaitUntil(() => requestDone);

                if (!requestSucceeded)
                {
                    Debug.LogWarning("Impossible de vérifier les profils distants LootLocker.");
                    onValidated?.Invoke(true);
                    yield break;
                }

                if (nameExists)
                {
                    onValidated?.Invoke(false);
                    yield break;
                }

                shouldContinue = itemCount == RemoteNameCheckBatchSize;
                offset += itemCount;
            }
        }

        onValidated?.Invoke(true);
    }

    private void DeletePlayerFromPlayerPage(string profileId)
    {
        PlayerProfile profile = GetPlayerProfile(profileId);

        if (profile == null)
        {
            SetPlayerManagementStatus("Joueur introuvable.");
            return;
        }

        if (playerProfiles.Count <= 1)
        {
            SetPlayerManagementStatus("Tu dois garder au moins un joueur.");
            return;
        }

        string deletedProfileId = profile.Id;
        DeleteLocalSavesForProfile(deletedProfileId);
        playerProfiles.Remove(profile);

        if (currentPlayerProfileId == deletedProfileId)
            SetCurrentPlayerProfile(playerProfiles[0].Id);

        selectedPlayerProfileId = currentPlayerProfileId;
        isWaitingDeleteConfirmation = false;

        SavePlayerProfiles();
        RenderPlayerManagement();
        UpdatePlayerManagementControls();
        SetPlayerManagementStatus("Joueur supprimé.");
    }

    private void SelectPlayerProfile(string profileId)
    {
        if (!HasPlayerProfile(profileId))
            return;

        selectedPlayerProfileId = profileId;
        SetCurrentPlayerProfile(profileId);
        isWaitingDeleteConfirmation = false;


        SavePlayerProfiles();
        RenderPlayerManagement();
        UpdatePlayerManagementControls();
        SetPlayerManagementStatus("Joueur actif : " + GetPlayerProfileName(profileId));
    }

    private void SetCurrentPlayerProfile(string profileId)
    {
        if (!HasPlayerProfile(profileId))
            return;

        currentPlayerProfileId = profileId;
        localPlayerName = GetCurrentPlayerProfileName();

        PlayerPrefs.SetString(CurrentPlayerProfileIdKey, currentPlayerProfileId);
        PlayerPrefs.Save();

        UpdateModeLabels();
    }

    private void UpdatePlayerManagementControls()
    {
        if (managePlayersButtonText != null)
            managePlayersButtonText.text = isPlayerManagementMode ? backToLeaderboardButtonLabel : managePlayersButtonLabel;
    }

    private void HidePlayerManagementObjects()
    {
        if (playerManagementPage != null)
        {
            playerManagementPage.CloseAllPopups();
            playerManagementPage.gameObject.SetActive(false);
        }
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

    private string SanitizePlayerName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        string sanitizedName = rawName.Trim();

        if (sanitizedName.Length > MaxPlayerNameLength)
            sanitizedName = sanitizedName.Substring(0, MaxPlayerNameLength);

        return sanitizedName;
    }

    private bool IsPlayerNameAlreadyUsed(string playerName, string ignoredProfileId = null)
    {
        foreach (PlayerProfile profile in playerProfiles)
        {
            if (profile.Id == ignoredProfileId)
                continue;

            if (string.Equals(profile.Name, playerName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool HasPlayerProfile(string profileId)
    {
        return GetPlayerProfile(profileId) != null;
    }

    private PlayerProfile GetPlayerProfile(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return null;

        return playerProfiles.Find(profile => profile.Id == profileId);
    }

    private PlayerProfile GetSelectedPlayerProfile()
    {
        return GetPlayerProfile(selectedPlayerProfileId);
    }

    private string GetCurrentPlayerProfileId()
    {
        if (!HasPlayerProfile(currentPlayerProfileId))
        {
            if (playerProfiles.Count == 0)
                InitializePlayerProfiles();

            currentPlayerProfileId = playerProfiles[0].Id;
        }

        return currentPlayerProfileId;
    }

    private string GetCurrentPlayerProfileName()
    {
        return GetPlayerProfileName(GetCurrentPlayerProfileId());
    }

    private string GetPlayerProfileName(string profileId)
    {
        PlayerProfile profile = GetPlayerProfile(profileId);
        return profile != null ? profile.Name : fallbackLocalPlayerName;
    }

    private string GetServerMemberIdForCurrentProfile()
    {
        return GetServerMemberIdForProfile(GetPlayerProfile(GetCurrentPlayerProfileId()));
    }

    private string GetServerMemberIdForProfile(PlayerProfile profile)
    {
        if (profile == null)
            return GetCurrentPlayerProfileId();

        string playerName = SanitizePlayerName(profile.Name).Replace(" ", "_");
        return string.IsNullOrWhiteSpace(playerName) ? profile.Id : playerName;
    }

    private void AddPreviousMemberId(PlayerProfile profile, string memberId)
    {
        if (profile == null || string.IsNullOrWhiteSpace(memberId))
            return;

        profile.PreviousMemberIds ??= new List<string>();

        if (ArePlayerNamesEquivalent(memberId, GetServerMemberIdForProfile(profile)))
        {
            foreach (string previousMemberId in profile.PreviousMemberIds)
            {
                if (ArePlayerNamesEquivalent(previousMemberId, memberId))
                    return;
            }

            profile.PreviousMemberIds.Add(memberId);
        }
    }

    private string GeneratePlayerProfileId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private string CreateUniqueDefaultPlayerName()
    {
        for (int i = 0; i < 50; i++)
        {
            string candidate = "Player_" + GetShortCode(Guid.NewGuid().ToString("N"));

            if (!IsPlayerNameAlreadyUsed(candidate))
                return candidate;
        }

        return "Player_" + UnityEngine.Random.Range(10000, 99999);
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

    private void SavePlayerProfiles()
    {
        PlayerProfileCollection collection = new PlayerProfileCollection
        {
            Players = playerProfiles,
            CurrentPlayerId = currentPlayerProfileId
        };

        PlayerPrefs.SetString(PlayerProfilesJsonKey, JsonUtility.ToJson(collection));
        PlayerPrefs.SetString(CurrentPlayerProfileIdKey, currentPlayerProfileId);
        PlayerPrefs.Save();
    }

    private void SetPlayerManagementStatus(string message)
    {
        if (playerManagementPage != null)
            playerManagementPage.SetStatus(message);
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
        if (isPlayerManagementMode)
        {
            RenderPlayerManagement();
            return;
        }

        HidePlayerManagementObjects();

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
        if (!isPlayerManagementMode && playerManagementPage != null)
            playerManagementPage.gameObject.SetActive(false);

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
    }

    private void HideNavigationButtons()
    {
        if (previousPageButton != null)
            previousPageButton.gameObject.SetActive(false);

        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(false);
    }

    public void ForceFocusOnNextButton()
    {
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

        if (managePlayersButton != null && managePlayersButton.gameObject.activeInHierarchy)
        {
            managePlayersButton.Select();
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

    private bool IsRemotePlayerNameMatch(LootLockerLeaderboardMember item, string playerName)
    {
        ScoreMetadata metadata = GetScoreMetadata(item);

        if (ArePlayerNamesEquivalent(metadata?.ProfileName, playerName))
            return true;

        if (item.player != null && ArePlayerNamesEquivalent(item.player.name, playerName))
            return true;

        string memberId = GetLeaderboardMemberId(item);
        return ArePlayerNamesEquivalent(memberId, playerName);
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
            return GetPlayerProfile(metadata.ProfileId);

        return GetPlayerProfileByMemberId(GetLeaderboardMemberId(item));
    }

    private PlayerProfile GetPlayerProfileByMemberId(string memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
            return null;

        foreach (PlayerProfile profile in playerProfiles)
        {
            if (profile == null)
                continue;

            if (ArePlayerNamesEquivalent(GetServerMemberIdForProfile(profile), memberId))
                return profile;

            if (profile.PreviousMemberIds == null)
                continue;

            foreach (string previousMemberId in profile.PreviousMemberIds)
            {
                if (ArePlayerNamesEquivalent(previousMemberId, memberId))
                    return profile;
            }
        }

        return null;
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
        return string.Equals(
            NormalizePlayerNameForComparison(left),
            NormalizePlayerNameForComparison(right),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private string NormalizePlayerNameForComparison(string playerName)
    {
        return SanitizePlayerName(playerName).Replace("_", " ").Trim();
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
            foreach (PlayerProfile profile in playerProfiles)
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
