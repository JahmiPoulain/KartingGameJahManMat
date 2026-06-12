using System;
using System.Collections;
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

    private const int MaxPlayerProfiles = 10;
    private const int MaxPlayerNameLength = 12;
    private const int RemoteNameCheckBatchSize = 100;

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
        playerManagementPage?.ForceFocusOnFirstButton();
        ManagementModeChanged?.Invoke(true);
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

        playerManagementPage.RefreshControllerNavigation();
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

    private void ValidatePlayerNameFromPlayerPage(string rawPlayerName, string ignoredProfileId, Action<bool> onValidated)
    {
        string playerName = SanitizePlayerName(rawPlayerName);

        if (string.IsNullOrWhiteSpace(playerName) || IsPlayerNameAlreadyUsed(playerName, ignoredProfileId))
        {
            onValidated?.Invoke(false);
            return;
        }

        if (!string.IsNullOrWhiteSpace(ignoredProfileId) &&
            ArePlayerNamesEquivalent(playerName, GetProfileName(ignoredProfileId)))
        {
            onValidated?.Invoke(true);
            return;
        }

        StartCoroutine(ValidatePlayerNameRemotely(playerName, onValidated));
    }

    private IEnumerator ValidatePlayerNameRemotely(string playerName, Action<bool> onValidated)
    {
        if (simulateOfflineMode || !isConnected || leaderboardKeys.Count == 0)
        {
            onValidated?.Invoke(true);
            yield break;
        }

        foreach (string leaderboardKey in leaderboardKeys)
        {
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
        if (!HasPlayerProfile(profileId))
            return;

        currentPlayerProfileId = profileId;
        PlayerPrefs.SetString(CurrentPlayerProfileIdKey, currentPlayerProfileId);
        PlayerPrefs.Save();

        CurrentProfileChanged?.Invoke();
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

    private string GetLeaderboardMemberId(LootLockerLeaderboardMember item)
    {
        return TryGetStringMember(item, "member_id");
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
            string candidate = "Player_" + GetShortCode(Guid.NewGuid().ToString("N"));

            if (!IsPlayerNameAlreadyUsed(candidate))
                return candidate;
        }

        return "Player_" + UnityEngine.Random.Range(10000, 99999);
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
}
