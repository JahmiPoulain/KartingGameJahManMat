using System;
using System.Collections.Generic;
using LootLocker.Requests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfilsManager : MonoBehaviour
{
    public event Action<bool> ManagementModeChanged;
    public event Action CurrentProfileChanged;
    public event Action<PlayerProfile> ProfileRenamed;
    public event Action<string> ProfileDeleted;

    private const int MaxPlayerProfiles = 6;
    private const int MaxPlayerNameLength = 12;
    private const string PlayerProfilesJsonKey = "Leaderboard_PlayerProfilesJson";
    private const string CurrentPlayerProfileIdKey = "Leaderboard_CurrentPlayerProfileId";

    [Header("Player Management")]
    [SerializeField] private Button managePlayersButton;
    [SerializeField] private TextMeshProUGUI managePlayersButtonText;
    [SerializeField] private PlayerManagementPage playerManagementPage;
    [SerializeField] private string managePlayersButtonLabel = "Gérer les joueurs";
    [SerializeField] private string backToLeaderboardButtonLabel = "Retour classement";
    [SerializeField] private string fallbackLocalPlayerName = "Player";

    private readonly List<PlayerProfile> playerProfiles = new();
    private readonly List<string> leaderboardKeys = new();

    private string currentPlayerProfileId;
    private string selectedPlayerProfileId;
    private string reservedNewProfileId;
    private string reservedNewProfileName;
    private bool isPlayerManagementMode;
    private bool simulateOfflineMode;
    private bool isConnected;

    [Serializable]
    private class PlayerProfileCollection
    {
        public List<PlayerProfile> Players = new();
        public string CurrentPlayerId;
    }

    public IReadOnlyList<PlayerProfile> Profiles => playerProfiles;
    public bool IsManagingProfiles => isPlayerManagementMode;
    public string CurrentProfileId => GetCurrentPlayerProfileId();
    public string CurrentProfileName => GetCurrentPlayerProfileName();

    private void Awake()
    {
        InitializePlayerProfiles();
        ConfigurePlayerManagementPage();
        HidePlayerManagementObjects();
        UpdatePlayerManagementControls();
    }

    private void OnEnable()
    {
        if (managePlayersButton != null)
            managePlayersButton.onClick.AddListener(TogglePlayerManagementMode);
    }

    private void OnDisable()
    {
        if (managePlayersButton != null)
            managePlayersButton.onClick.RemoveListener(TogglePlayerManagementMode);
    }

    public void SetLeaderboardKeys(IEnumerable<string> keys)
    {
        leaderboardKeys.Clear();

        if (keys == null)
            return;

        foreach (string key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key))
                leaderboardKeys.Add(key);
        }
    }

    public void SetOnlineState(bool simulateOffline, bool connected)
    {
        simulateOfflineMode = simulateOffline;
        isConnected = connected;
    }

    public void TogglePlayerManagementMode()
    {
        if (isPlayerManagementMode)
            HidePlayerManagement();
        else
            OpenPlayerManagement();
    }

    public void OpenPlayerManagement()
    {
        ShowPlayerManagement();
    }

    public void HidePlayerManagement(bool notify = true)
    {
        isPlayerManagementMode = false;

        HidePlayerManagementObjects();
        SetPlayerManagementStatus(string.Empty);
        UpdatePlayerManagementControls();

        if (notify)
            ManagementModeChanged?.Invoke(false);
    }

    public PlayerProfile GetProfile(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return null;

        return playerProfiles.Find(profile => profile.Id == profileId);
    }

    public PlayerProfile GetProfileByMemberId(string memberId)
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

    public string GetProfileName(string profileId)
    {
        PlayerProfile profile = GetProfile(profileId);
        return profile != null ? profile.Name : fallbackLocalPlayerName;
    }

    public string GetServerMemberIdForCurrentProfile()
    {
        return GetServerMemberIdForProfile(GetProfile(GetCurrentPlayerProfileId()));
    }

    public string GetServerMemberIdForProfile(PlayerProfile profile)
    {
        if (profile == null)
            return GetCurrentPlayerProfileId();

        string playerName = SanitizePlayerName(profile.Name).Replace(" ", "_");
        return string.IsNullOrWhiteSpace(playerName) ? profile.Id : playerName;
    }

    public string GetLootLockerGuestIdentifierForProfile(PlayerProfile profile)
    {
        string profileId = profile != null ? profile.Id : GetCurrentPlayerProfileId();
        return GetLootLockerGuestIdentifierForProfileId(profileId);
    }

    public string GetLootLockerGuestIdentifierForProfileId(string profileId)
    {
        return string.IsNullOrWhiteSpace(profileId)
            ? "local_profile_default"
            : "local_profile_" + profileId;
    }

    public bool ArePlayerNamesEquivalent(string left, string right)
    {
        return string.Equals(
            NormalizePlayerNameForComparison(left),
            NormalizePlayerNameForComparison(right),
            StringComparison.OrdinalIgnoreCase
        );
    }

    public string SanitizePlayerName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        string sanitizedName = rawName.Trim();

        if (sanitizedName.Length > MaxPlayerNameLength)
            sanitizedName = sanitizedName.Substring(0, MaxPlayerNameLength);

        return sanitizedName;
    }

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

        ActiveProfileBridge.Instance?.SetProfile(currentPlayerProfileId);
        SavePlayerProfiles();
    }

    private void ShowPlayerManagement()
    {
        isPlayerManagementMode = true;
        selectedPlayerProfileId = currentPlayerProfileId;

        ConfigurePlayerManagementPage();

        if (playerManagementPage != null)
        {
            playerManagementPage.gameObject.SetActive(true);
            playerManagementPage.CloseAllPopups();
        }

        UpdatePlayerManagementControls();
        RenderPlayerManagement();
        ManagementModeChanged?.Invoke(true);
        playerManagementPage?.FocusFirstSelectable();
    }

    private void RenderPlayerManagement()
    {
        if (!isPlayerManagementMode)
            return;

        if (playerManagementPage == null)
        {
            Debug.LogError("PlayerManagementPage n'est pas assignée dans le ProfilsManager.");
            return;
        }

        playerManagementPage.gameObject.SetActive(true);
        playerManagementPage.ClearPage();
        playerManagementPage.SetCanAddPlayer(playerProfiles.Count < MaxPlayerProfiles);
        playerManagementPage.SetCanDeletePlayers(playerProfiles.Count > 1);

        for (int i = 0; i < MaxPlayerProfiles; i++)
        {
            if (i >= playerProfiles.Count)
                continue;

            PlayerProfile profile = playerProfiles[i];

            playerManagementPage.SetPlayer(
                i,
                profile.Id,
                profile.Name,
                profile.Id == currentPlayerProfileId
            );
        }

        playerManagementPage.FocusFirstSelectable();
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
            ClearReservedNewProfile();
            return;
        }

        if (playerProfiles.Count >= MaxPlayerProfiles)
        {
            SetPlayerManagementStatus($"Maximum {MaxPlayerProfiles} joueurs.");
            ClearReservedNewProfile();
            return;
        }

        if (IsPlayerNameAlreadyUsed(playerName))
        {
            SetPlayerManagementStatus("Ce nom existe déjà.");
            ClearReservedNewProfile();
            return;
        }

        string profileId = GetReservedNewProfileId(playerName);
        PlayerProfile profile = new PlayerProfile(profileId, playerName);
        playerProfiles.Add(profile);
        ClearReservedNewProfile();

        SetCurrentPlayerProfile(profile.Id);
        selectedPlayerProfileId = profile.Id;

        SavePlayerProfiles();
        RenderPlayerManagement();
        UpdatePlayerManagementControls();
        SetPlayerManagementStatus("Joueur ajouté.");
    }

    private void RenamePlayerFromPlayerPage(string profileId, string rawPlayerName)
    {
        PlayerProfile profile = GetProfile(profileId);

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
        selectedPlayerProfileId = profile.Id;

        SavePlayerProfiles();
        RenderPlayerManagement();
        UpdatePlayerManagementControls();
        SetPlayerManagementStatus("Nom modifié.");

        ProfileRenamed?.Invoke(profile);

        if (profile.Id == currentPlayerProfileId)
            CurrentProfileChanged?.Invoke();
    }

    private void ValidatePlayerNameFromPlayerPage(
        string rawPlayerName,
        string ignoredProfileId,
        bool isRename,
        Action<bool, string> onValidated
    )
    {
        string playerName = SanitizePlayerName(rawPlayerName);

        if (string.IsNullOrWhiteSpace(playerName) || IsPlayerNameAlreadyUsed(playerName, ignoredProfileId))
        {
            onValidated?.Invoke(false, "Ce nom existe déjà.");
            return;
        }

        if (simulateOfflineMode)
        {
            onValidated?.Invoke(false, "Connexion requise pour vérifier ce nom.");
            return;
        }

        string targetProfileId = isRename ? ignoredProfileId : GeneratePlayerProfileId();

        if (string.IsNullOrWhiteSpace(targetProfileId))
        {
            onValidated?.Invoke(false, "Joueur introuvable.");
            return;
        }

        ReserveLootLockerPlayerName(targetProfileId, playerName, (isAvailable, errorMessage) =>
        {
            if (!isAvailable)
            {
                onValidated?.Invoke(false, errorMessage);
                return;
            }

            if (!isRename)
            {
                reservedNewProfileId = targetProfileId;
                reservedNewProfileName = playerName;
            }

            onValidated?.Invoke(true, string.Empty);
        });
    }

    private void DeletePlayerFromPlayerPage(string profileId)
    {
        PlayerProfile profile = GetProfile(profileId);

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

        if (simulateOfflineMode)
        {
            SetPlayerManagementStatus("Connexion requise pour supprimer ce joueur.");
            return;
        }

        SetPlayerManagementStatus("Suppression du joueur...");

        ReleaseLootLockerPlayerName(profile, (success, errorMessage) =>
        {
            if (!success)
            {
                SetPlayerManagementStatus(errorMessage);
                return;
            }

            CompleteDeletePlayer(profile);
        });
    }

    private void CompleteDeletePlayer(PlayerProfile profile)
    {
        if (profile == null || !playerProfiles.Contains(profile))
        {
            SetPlayerManagementStatus("Joueur introuvable.");
            return;
        }

        string deletedProfileId = profile.Id;
        playerProfiles.Remove(profile);

        if (currentPlayerProfileId == deletedProfileId)
            SetCurrentPlayerProfile(playerProfiles[0].Id);

        selectedPlayerProfileId = currentPlayerProfileId;

        SavePlayerProfiles();
        RenderPlayerManagement();
        UpdatePlayerManagementControls();
        SetPlayerManagementStatus("Joueur supprimé.");

        ProfileDeleted?.Invoke(deletedProfileId);
    }

    private void SelectPlayerProfile(string profileId)
    {
        if (!HasPlayerProfile(profileId))
            return;

        selectedPlayerProfileId = profileId;
        SetCurrentPlayerProfile(profileId);

        SavePlayerProfiles();
        RenderPlayerManagement();
        UpdatePlayerManagementControls();
        SetPlayerManagementStatus("Joueur actif : " + GetProfileName(profileId));
    }

    private void SetCurrentPlayerProfile(string profileId)
    {
        if (!HasPlayerProfile(profileId)) return;

        currentPlayerProfileId = profileId;
        PlayerPrefs.SetString(CurrentPlayerProfileIdKey, currentPlayerProfileId);
        PlayerPrefs.Save();

        ActiveProfileBridge.Instance?.SetProfile(currentPlayerProfileId);

        CurrentProfileChanged?.Invoke();
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

    private void ReserveLootLockerPlayerName(string profileId, string playerName, Action<bool, string> onComplete)
    {
        LootLockerConfigSanitizer.SanitizeApiKey();

        string guestIdentifier = GetLootLockerGuestIdentifierForProfileId(profileId);

        LootLockerSDKManager.StartGuestSession(guestIdentifier, sessionResponse =>
        {
            if (!sessionResponse.success)
            {
                Debug.LogWarning("Impossible de vérifier le nom LootLocker : session indisponible.");
                onComplete?.Invoke(false, "Impossible de vérifier ce nom en ligne.");
                return;
            }

            LootLockerSDKManager.LookupPlayerNamesByPlayerNames(new[] { playerName }, lookupResponse =>
            {
                if (!lookupResponse.success)
                {
                    string message = lookupResponse.errorData != null
                        ? lookupResponse.errorData.message
                        : "erreur inconnue";

                    Debug.LogWarning("Impossible de vérifier le nom LootLocker : " + message);
                    onComplete?.Invoke(false, "Impossible de vérifier ce nom en ligne.");
                    return;
                }

                bool nameIsTaken = IsLootLockerPlayerNameTakenByAnotherPlayer(
                    lookupResponse.players,
                    playerName,
                    sessionResponse.player_ulid
                );

                if (nameIsTaken)
                {
                    onComplete?.Invoke(false, "Ce nom est déjà pris en ligne.");
                    return;
                }

                LootLockerSDKManager.SetPlayerName(playerName, nameResponse =>
                {
                    if (!nameResponse.success)
                    {
                        string message = nameResponse.errorData != null
                            ? nameResponse.errorData.message
                            : "erreur inconnue";

                        Debug.LogWarning("Impossible de réserver le nom LootLocker : " + message);
                        onComplete?.Invoke(
                            false,
                            nameResponse.statusCode == 409
                                ? "Ce nom est déjà pris en ligne."
                                : "Impossible de réserver ce nom en ligne."
                        );
                        return;
                    }

                    onComplete?.Invoke(true, string.Empty);
                }, sessionResponse.player_ulid);
            }, sessionResponse.player_ulid);
        });
    }

    private bool IsLootLockerPlayerNameTakenByAnotherPlayer(
        PlayerNameWithIDs[] players,
        string playerName,
        string currentPlayerUlid
    )
    {
        if (players == null)
            return false;

        foreach (PlayerNameWithIDs player in players)
        {
            if (player == null || !ArePlayerNamesEquivalent(player.name, playerName))
                continue;

            if (string.Equals(player.ulid, currentPlayerUlid, StringComparison.OrdinalIgnoreCase))
                continue;

            return true;
        }

        return false;
    }

    private void ReleaseLootLockerPlayerName(PlayerProfile profile, Action<bool, string> onComplete)
    {
        if (profile == null)
        {
            onComplete?.Invoke(false, "Joueur introuvable.");
            return;
        }

        LootLockerConfigSanitizer.SanitizeApiKey();

        string guestIdentifier = GetLootLockerGuestIdentifierForProfile(profile);

        LootLockerSDKManager.StartGuestSession(guestIdentifier, sessionResponse =>
        {
            if (!sessionResponse.success)
            {
                Debug.LogWarning("Impossible de libérer le nom LootLocker : session indisponible.");
                onComplete?.Invoke(false, "Impossible de supprimer ce joueur en ligne.");
                return;
            }

            string releasedName = GenerateReleasedLootLockerName(profile);

            LootLockerSDKManager.SetPlayerName(releasedName, nameResponse =>
            {
                if (!nameResponse.success)
                {
                    string message = nameResponse.errorData != null
                        ? nameResponse.errorData.message
                        : "erreur inconnue";

                    Debug.LogWarning("Impossible de libérer le nom LootLocker : " + message);
                    onComplete?.Invoke(false, "Impossible de libérer ce nom en ligne.");
                    return;
                }

                onComplete?.Invoke(true, string.Empty);
            }, sessionResponse.player_ulid);
        });
    }

    private string GenerateReleasedLootLockerName(PlayerProfile profile)
    {
        string profileId = profile != null && !string.IsNullOrWhiteSpace(profile.Id)
            ? profile.Id
            : GeneratePlayerProfileId();

        return "deleted_" + profileId;
    }

    private string GetReservedNewProfileId(string playerName)
    {
        if (!string.IsNullOrWhiteSpace(reservedNewProfileId) &&
            ArePlayerNamesEquivalent(reservedNewProfileName, playerName))
        {
            return reservedNewProfileId;
        }

        return GeneratePlayerProfileId();
    }

    private void ClearReservedNewProfile()
    {
        reservedNewProfileId = string.Empty;
        reservedNewProfileName = string.Empty;
    }

    private bool HasPlayerProfile(string profileId)
    {
        return GetProfile(profileId) != null;
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
        return GetProfileName(GetCurrentPlayerProfileId());
    }

    private void AddPreviousMemberId(PlayerProfile profile, string memberId)
    {
        if (profile == null || string.IsNullOrWhiteSpace(memberId))
            return;

        profile.PreviousMemberIds ??= new List<string>();

        if (!ArePlayerNamesEquivalent(memberId, GetServerMemberIdForProfile(profile)))
            return;

        foreach (string previousMemberId in profile.PreviousMemberIds)
        {
            if (ArePlayerNamesEquivalent(previousMemberId, memberId))
                return;
        }

        profile.PreviousMemberIds.Add(memberId);
    }

    private string NormalizePlayerNameForComparison(string playerName)
    {
        return SanitizePlayerName(playerName).Replace("_", " ").Trim();
    }

    private string GeneratePlayerProfileId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private string CreateUniqueDefaultPlayerName()
    {
        for (int i = 0; i < 50; i++)
        {
            string candidate = "Joueur_" + GetShortCode(Guid.NewGuid().ToString("N"));

            if (!IsPlayerNameAlreadyUsed(candidate))
                return candidate;
        }

        return "Joueur_" + UnityEngine.Random.Range(10000, 99999);
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

}
