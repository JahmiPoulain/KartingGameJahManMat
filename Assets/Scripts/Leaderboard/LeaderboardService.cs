using LootLocker.Requests;
using UnityEngine;

public class LeaderboardService : MonoBehaviour
{
    public static LeaderboardService Instance { get; private set; }

    private const string LocalBestScoreKeyPrefix = "Leaderboard_LocalBestScore";
    private const string HasLocalBestScoreKeyPrefix = "Leaderboard_HasLocalBestScore";
    private const string PendingUploadScoreKeyPrefix = "Leaderboard_PendingUploadScore";
    private const string HasPendingUploadKeyPrefix = "Leaderboard_HasPendingUpload";

    private string timeAttackNormalKey = "33908";
    private string timeAttackReverseKey = "34772";
    private string contreLaMontreNormalKey = "34773";
    private string contreLaMontreReverseKey = "34774";

    private string currentProfileId = "local";
    private string currentProfileName = "Player";
    private string connectedProfileId;

    private bool simulateOfflineMode;
    private bool isConnected;
    private bool isStartingSession;

    public static LeaderboardService EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameObject serviceObject = new GameObject("LeaderboardService");
        return serviceObject.AddComponent<LeaderboardService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Configure(
        string timeAttackNormal,
        string timeAttackReverse,
        string contreLaMontreNormal,
        string contreLaMontreReverse,
        PlayerProfile currentProfile,
        bool simulateOffline
    )
    {
        timeAttackNormalKey = timeAttackNormal;
        timeAttackReverseKey = timeAttackReverse;
        contreLaMontreNormalKey = contreLaMontreNormal;
        contreLaMontreReverseKey = contreLaMontreReverse;
        simulateOfflineMode = simulateOffline;

        SetCurrentProfile(currentProfile);
    }

    public void SetCurrentProfile(PlayerProfile profile)
    {
        if (profile == null)
            return;

        currentProfileId = string.IsNullOrWhiteSpace(profile.Id) ? "local" : profile.Id;
        currentProfileName = string.IsNullOrWhiteSpace(profile.Name) ? "Player" : profile.Name;

        if (connectedProfileId != currentProfileId)
            isConnected = false;
    }

    public bool SubmitTimeAttackNormal(int timeInMilliseconds)
    {
        return SubmitScore(timeInMilliseconds, LeaderboardGameMode.TimeAttack, LeaderboardCircuitMode.Normal);
    }

    public bool SubmitTimeAttackReverse(int timeInMilliseconds)
    {
        return SubmitScore(timeInMilliseconds, LeaderboardGameMode.TimeAttack, LeaderboardCircuitMode.Reverse);
    }

    public bool SubmitContreLaMontreNormal(int timeInMilliseconds)
    {
        return SubmitScore(timeInMilliseconds, LeaderboardGameMode.ContreLaMontre, LeaderboardCircuitMode.Normal);
    }

    public bool SubmitContreLaMontreReverse(int timeInMilliseconds)
    {
        return SubmitScore(timeInMilliseconds, LeaderboardGameMode.ContreLaMontre, LeaderboardCircuitMode.Reverse);
    }

    public bool TryGetLocalBestScore(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode,
        out int timeInMilliseconds
    )
    {
        if (!HasLocalBestScore(gameMode, circuitMode))
        {
            timeInMilliseconds = int.MaxValue;
            return false;
        }

        timeInMilliseconds = GetLocalBestScore(gameMode, circuitMode);
        return true;
    }

    private bool SubmitScore(
        int timeInMilliseconds,
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        if (!SaveLocalBestScoreIfBetter(timeInMilliseconds, gameMode, circuitMode))
        {
            Debug.Log(
                $"Score ignoré pour {currentProfileName} / {gameMode} / {circuitMode} : " +
                $"{FormatTime(timeInMilliseconds)} n'est pas meilleur que le score local."
            );
            return false;
        }

        int localBestScore = GetLocalBestScore(gameMode, circuitMode);
        MarkPendingUpload(localBestScore, gameMode, circuitMode);

        if (simulateOfflineMode)
        {
            isConnected = false;
            Debug.LogWarning("Score sauvegardé localement. Mode hors ligne simulé actif.");
            return true;
        }

        if (!isConnected)
        {
            Debug.LogWarning("Score sauvegardé localement, mais pas encore envoyé : pas de connexion LootLocker.");
            StartLootLockerSession();
            return true;
        }

        TryUploadPendingLocalScore(gameMode, circuitMode);
        return true;
    }

    private void StartLootLockerSession()
    {
        if (simulateOfflineMode || isStartingSession)
            return;

        if (isConnected && connectedProfileId == currentProfileId)
            return;

        LootLockerConfigSanitizer.SanitizeApiKey();
        isStartingSession = true;

        string requestedProfileId = currentProfileId;
        string guestIdentifier = GetLootLockerGuestIdentifierForCurrentProfile();

        LootLockerSDKManager.StartGuestSession(guestIdentifier, response =>
        {
            isStartingSession = false;

            if (!response.success)
            {
                isConnected = false;
                connectedProfileId = string.Empty;
                Debug.LogWarning("LeaderboardService : impossible de démarrer la session LootLocker.");
                return;
            }

            if (requestedProfileId != currentProfileId)
            {
                isConnected = false;
                connectedProfileId = string.Empty;
                StartLootLockerSession();
                return;
            }

            isConnected = true;
            connectedProfileId = currentProfileId;

            LootLockerSDKManager.SetPlayerName(currentProfileName, nameResponse =>
            {
                if (!nameResponse.success)
                    Debug.LogWarning("LeaderboardService : impossible de mettre à jour le nom LootLocker : " + nameResponse.errorData.message);

                TryUploadAllPendingLocalScores();
            });
        });
    }

    private void TryUploadAllPendingLocalScores()
    {
        TryUploadPendingLocalScore(LeaderboardGameMode.TimeAttack, LeaderboardCircuitMode.Normal);
        TryUploadPendingLocalScore(LeaderboardGameMode.TimeAttack, LeaderboardCircuitMode.Reverse);
        TryUploadPendingLocalScore(LeaderboardGameMode.ContreLaMontre, LeaderboardCircuitMode.Normal);
        TryUploadPendingLocalScore(LeaderboardGameMode.ContreLaMontre, LeaderboardCircuitMode.Reverse);
    }

    private void TryUploadPendingLocalScore(
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        if (simulateOfflineMode ||
            !isConnected ||
            connectedProfileId != currentProfileId ||
            !HasPendingUpload(gameMode, circuitMode))
        {
            return;
        }

        string leaderboardKey = GetLeaderboardKey(gameMode, circuitMode);

        if (string.IsNullOrWhiteSpace(leaderboardKey))
        {
            Debug.LogError($"Upload impossible : leaderboard key manquante pour {gameMode} / {circuitMode}.");
            return;
        }

        int scoreToUpload = GetPendingUploadScore(gameMode, circuitMode);
        string metadata = JsonUtility.ToJson(new ScoreMetadata(currentProfileId, currentProfileName));

        LootLockerSDKManager.SubmitScore(string.Empty, scoreToUpload, leaderboardKey, metadata, response =>
        {
            if (!response.success)
            {
                Debug.LogWarning(
                    $"Impossible d'envoyer le score {gameMode} / {circuitMode}. " +
                    "Il reste sauvegardé localement."
                );
                return;
            }

            ClearPendingUpload(gameMode, circuitMode);

            Debug.Log(
                $"Score synchronisé avec LootLocker pour {currentProfileName} / {gameMode} / {circuitMode} : " +
                FormatTime(scoreToUpload)
            );
        });
    }

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
    }

    private bool HasLocalBestScore(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
    {
        return PlayerPrefs.GetInt(GetHasLocalBestScoreKey(gameMode, circuitMode), 0) == 1;
    }

    private int GetLocalBestScore(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
    {
        return PlayerPrefs.GetInt(GetLocalBestScoreKey(gameMode, circuitMode), int.MaxValue);
    }

    private void MarkPendingUpload(
        int timeInMilliseconds,
        LeaderboardGameMode gameMode,
        LeaderboardCircuitMode circuitMode
    )
    {
        PlayerPrefs.SetInt(GetHasPendingUploadKey(gameMode, circuitMode), 1);
        PlayerPrefs.SetInt(GetPendingUploadScoreKey(gameMode, circuitMode), timeInMilliseconds);
        PlayerPrefs.Save();
    }

    private bool HasPendingUpload(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
    {
        return PlayerPrefs.GetInt(GetHasPendingUploadKey(gameMode, circuitMode), 0) == 1;
    }

    private int GetPendingUploadScore(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
    {
        return PlayerPrefs.GetInt(GetPendingUploadScoreKey(gameMode, circuitMode), int.MaxValue);
    }

    private void ClearPendingUpload(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
    {
        PlayerPrefs.DeleteKey(GetHasPendingUploadKey(gameMode, circuitMode));
        PlayerPrefs.DeleteKey(GetPendingUploadScoreKey(gameMode, circuitMode));
        PlayerPrefs.Save();
    }

    private string GetLeaderboardKey(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
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

    private string GetLocalBestScoreKey(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
    {
        return $"{LocalBestScoreKeyPrefix}_{currentProfileId}_{gameMode}_{circuitMode}";
    }

    private string GetHasLocalBestScoreKey(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
    {
        return $"{HasLocalBestScoreKeyPrefix}_{currentProfileId}_{gameMode}_{circuitMode}";
    }

    private string GetPendingUploadScoreKey(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
    {
        return $"{PendingUploadScoreKeyPrefix}_{currentProfileId}_{gameMode}_{circuitMode}";
    }

    private string GetHasPendingUploadKey(LeaderboardGameMode gameMode, LeaderboardCircuitMode circuitMode)
    {
        return $"{HasPendingUploadKeyPrefix}_{currentProfileId}_{gameMode}_{circuitMode}";
    }

    private string GetServerMemberIdForCurrentProfile()
    {
        string memberId = currentProfileName.Trim().Replace(" ", "_");
        return string.IsNullOrWhiteSpace(memberId) ? currentProfileId : memberId;
    }

    private string GetLootLockerGuestIdentifierForCurrentProfile()
    {
        return string.IsNullOrWhiteSpace(currentProfileId)
            ? "local_profile_default"
            : "local_profile_" + currentProfileId;
    }

    private string FormatTime(int milliseconds)
    {
        int min = milliseconds / 60000;
        int sec = milliseconds / 1000 % 60;
        int ms = milliseconds % 1000;

        return $"{min:00}:{sec:00}.{ms:000}";
    }
}
