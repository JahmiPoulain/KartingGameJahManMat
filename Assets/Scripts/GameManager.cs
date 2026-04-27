using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestionnaire principal du jeu Hero Quest.
/// Gère l'interface utilisateur et appelle les repositories pour toutes les opérations.
/// Inclut :
/// - Tâche 2.A : Système d'Achievements
/// - Tâche 2.B : Historique des quêtes terminées
/// - Tâche 2.C : Vue de statistiques + classement amélioré
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Références Base de Données")]
    [SerializeField] private DatabaseManager dbManager;

    // Ajoute ces champs dans la classe GameManager
    [Header("UI - Choix au démarrage")]
    [SerializeField] private GameObject panelChoice;           // Panel avec les 2 boutons
    [SerializeField] private Button loadLastPlayerButton;
    [SerializeField] private Button createNewPlayerButton;

    [Header("Références UI - Création")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Dropdown classDropdown;
    [SerializeField] private Button createButton;

    [Header("Références UI - Quêtes")]
    [SerializeField] private TMP_Dropdown questDropdown;
    [SerializeField] private Button startQuestButton;
    [SerializeField] private Button progressQuestButton;
    [SerializeField] private Button generateQuestButton;

    [Header("Références UI - Actions")]
    [SerializeField] private Button questButton;
    [SerializeField] private Button leaderboardButton;

    [Header("Références Textes")]
    [SerializeField] private TextMeshProUGUI playerInfoText;
    [SerializeField] private TextMeshProUGUI leaderboardText;
    [SerializeField] private TextMeshProUGUI questInfoText;

    [Header("Achievements - Tâche 2.A")]
    [SerializeField] private TextMeshProUGUI achievementsText;

    [Header("Historique des Quêtes - Tâche 2.B")]
    [SerializeField] private TextMeshProUGUI historyText;

    [Header("Statistiques Joueur - Tâche 2.C")]
    [SerializeField] private TextMeshProUGUI playerStatsText;     // ← pour la vue vw_PlayerStats

    [Header("Tâche 3 - Optimisations")]
    [SerializeField] private Button deletePlayerButton;
    [SerializeField] private Button exportJsonButton; 

    private PlayerData currentPlayer;
    private List<int> questIds = new List<int>();

    void Start()
    {
        // Vérification des références
        // Vérification optimisée des références (Solution élégante)
        if (!CheckReferences())
            return;

        // Au démarrage, on affiche uniquement le panel de choix
        panelChoice.SetActive(true);


        // Ajout des listeners
        createButton.onClick.AddListener(CreatePlayer);
        questButton.onClick.AddListener(DoQuest);
        leaderboardButton.onClick.AddListener(ShowLeaderboard);
        startQuestButton.onClick.AddListener(StartQuest);
        progressQuestButton.onClick.AddListener(ProgressQuest);
        generateQuestButton.onClick.AddListener(GenerateDynamicQuest);
        deletePlayerButton.onClick.AddListener(DeleteCurrentPlayer);
        exportJsonButton.onClick.AddListener(ExportLeaderboard);
        
        // Ajout des listeners pour le panel de choix
        loadLastPlayerButton.onClick.AddListener(LoadLastPlayedPlayer);
        createNewPlayerButton.onClick.AddListener(ShowCreatePlayerPanel);
        
        // Désactivation initiale
        questButton.interactable = false;
        startQuestButton.interactable = false;
        progressQuestButton.interactable = false;
        generateQuestButton.interactable = false;
        deletePlayerButton.interactable = false;

        PopulateQuestDropdown();
    }

    /// <summary>
    /// Vérification propre et maintenable de toutes les références UI.
    /// Ajouter un nouveau champ ? Il suffit de l'ajouter dans le tableau.
    /// </summary>
    private bool CheckReferences()
    {
        var fields = new (UnityEngine.Object obj, string name)[]
        {
            (dbManager,          "dbManager"),
            (panelChoice,        "panelChoice"),
            (loadLastPlayerButton, "loadLastPlayerButton"),
            (createNewPlayerButton, "createNewPlayerButton"),
            (nameInput,          "nameInput"),
            (classDropdown,      "classDropdown"),
            (questDropdown,      "questDropdown"),
            (createButton,       "createButton"),
            (questButton,        "questButton"),
            (leaderboardButton,  "leaderboardButton"),
            (startQuestButton,   "startQuestButton"),
            (progressQuestButton,"progressQuestButton"),
            (generateQuestButton,"generateQuestButton"),
            (playerInfoText,     "playerInfoText"),
            (leaderboardText,    "leaderboardText"),
            (questInfoText,      "questInfoText"),
            (achievementsText,   "achievementsText"),
            (historyText,        "historyText"),
            (playerStatsText,    "playerStatsText"),
            (deletePlayerButton, "deletePlayerButton")
        };

        // Récupère la liste des champs manquants
        var missing = fields
            .Where(f => f.obj == null)
            .Select(f => f.name)
            .ToList();

        if (missing.Count > 0)
        {
            Debug.LogError($"Références manquantes dans GameManager : {string.Join(", ", missing)}.\n" +
                           "Vérifiez l'Inspector et assignez tous les champs.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Crée ou charge un joueur.
    /// </summary>
    void CreatePlayer()
    {
        string name = nameInput.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            playerInfoText.text = "Entrez un nom valide !";
            return;
        }

        currentPlayer = dbManager.PlayerRepo.GetPlayerByName(name);

        if (currentPlayer == null)
        {
            dbManager.PlayerRepo.CreatePlayer(name, classDropdown.options[classDropdown.value].text);
            currentPlayer = dbManager.PlayerRepo.GetPlayerByName(name);
        }

        playerInfoText.text = $"Joueur chargé : {currentPlayer.Name} ({currentPlayer.Class}, " +
                              $"Niv. {currentPlayer.Level}, Exp. {currentPlayer.Experience})";

        // Activation des boutons
        questButton.interactable = startQuestButton.interactable =
        progressQuestButton.interactable = generateQuestButton.interactable =
        deletePlayerButton.interactable = true;

        createButton.interactable = nameInput.interactable = classDropdown.interactable = false;

        UpdateQuestInfo();
        UpdateAchievementsDisplay();
        UpdateHistoryDisplay();
        UpdatePlayerStatsDisplay();        // Tâche 2.C
    }

    /// <summary>
    /// Gagne de l'expérience via une quête simple.
    /// </summary>
    void DoQuest()
    {
        if (currentPlayer == null) return;

        int expGain = UnityEngine.Random.Range(10, 50);
        dbManager.PlayerRepo.UpdatePlayerExperience(currentPlayer.Id, expGain);
        currentPlayer = dbManager.PlayerRepo.GetPlayerById(currentPlayer.Id);

        playerInfoText.text = $"Quête terminée ! +{expGain} exp.\n" +
            $"{currentPlayer.Name} (Niv. {currentPlayer.Level}, Exp. {currentPlayer.Experience})";

        UpdateAchievementsDisplay();
        UpdateHistoryDisplay();
        UpdatePlayerStatsDisplay();        // Tâche 2.C
    }

    /// <summary>
    /// Remplit le dropdown des quêtes de façon sécurisée.
    /// </summary>
    void PopulateQuestDropdown()
    {
        questDropdown.ClearOptions();
        questIds.Clear();

        var quests = dbManager.QuestRepo.GetAllQuests();

        foreach (var quest in quests)
        {
            string prefix = quest.isDynamic ? "[Dynamique] " : "";
            questDropdown.options.Add(new TMP_Dropdown.OptionData($"{quest.questId}: {prefix}{quest.name}"));
            questIds.Add(quest.questId);
        }
    }

    /// <summary>
    /// Assigne la quête sélectionnée au joueur.
    /// </summary>
    void StartQuest()
    {
        if (currentPlayer == null || questIds.Count == 0) return;

        int questId = questIds[questDropdown.value];
        dbManager.QuestRepo.AssignQuest(currentPlayer.Id, questId);
        UpdateQuestInfo();
    }

    /// <summary>
    /// Fait progresser la quête sélectionnée et met à jour toutes les statistiques.
    /// </summary>
    void ProgressQuest()
    {
        if (currentPlayer == null || questIds.Count == 0) return;

        int currentIndex = questDropdown.value;
        int questId = questIds[currentIndex];

        dbManager.QuestRepo.UpdateQuestProgress(currentPlayer.Id, questId, 1);

        currentPlayer = dbManager.PlayerRepo.GetPlayerById(currentPlayer.Id);
        dbManager.PlayerRepo.CheckAndUnlockAchievements(currentPlayer.Id);

        dbManager.QuestRepo.CleanDynamicQuests(currentPlayer.Id);

        PopulateQuestDropdown();

        if (questIds.Count > 0)
        {
            int newIndex = Mathf.Min(currentIndex, questIds.Count - 1);
            questDropdown.value = newIndex;
            questDropdown.RefreshShownValue();
        }

        UpdateQuestInfo();
        UpdateAchievementsDisplay();
        UpdateHistoryDisplay();
        UpdatePlayerStatsDisplay();        // Tâche 2.C
    }

    /// <summary>
    /// Génère une nouvelle quête dynamique.
    /// </summary>
    void GenerateDynamicQuest()
    {
        if (currentPlayer == null) return;

        if (dbManager.QuestRepo.GenerateDynamicQuest(currentPlayer.Id))
        {
            PopulateQuestDropdown();
            UpdateQuestInfo();
            UpdateAchievementsDisplay();
            UpdateHistoryDisplay();
            UpdatePlayerStatsDisplay();
        }
        else
        {
            questInfoText.text = "Impossible de générer une nouvelle quête dynamique (limite de 3 atteinte).";
        }
    }

    /// <summary>
    /// Affiche le classement amélioré (Tâche 2.C).
    /// </summary>
    void ShowLeaderboard()
    {
        PlayerData[] leaderboard = dbManager.PlayerRepo.GetLeaderboard();
        string leaderboardString = "Classement :\n";

        for (int i = 0; i < leaderboard.Length; i++)
        {
            if (leaderboard[i] != null)
            {
                leaderboardString += $"{i + 1}. {leaderboard[i].Name} ({leaderboard[i].Class}, " +
                    $"Niv. {leaderboard[i].Level}, Exp. {leaderboard[i].Experience})\n";
            }
        }

        leaderboardText.text = leaderboardString;
    }

    /// <summary>
    /// Met à jour l'affichage des quêtes en cours.
    /// </summary>
    void UpdateQuestInfo()
    {
        if (currentPlayer == null) return;

        var quests = dbManager.QuestRepo.GetPlayerQuests(currentPlayer.Id);
        string questInfo = "Quêtes en cours :\n";

        foreach (var quest in quests)
        {
            questInfo += $"{quest.Name} ({quest.Status}, {quest.Progress}/{quest.TargetProgress}, " +
                $"Récompense: {quest.RewardXp} XP)\n";
        }

        questInfoText.text = questInfo;
    }

    /// <summary>
    /// Met à jour l'affichage des achievements (Tâche 2.A).
    /// </summary>
    void UpdateAchievementsDisplay()
    {
        if (currentPlayer == null || achievementsText == null) return;

        var unlocked = dbManager.PlayerRepo.GetUnlockedAchievements(currentPlayer.Id);

        if (unlocked.Count == 0)
        {
            achievementsText.text = "Aucun achievement débloqué pour le moment.";
            return;
        }

        string text = "<b>Achievements débloqués :</b>\n";
        foreach (var ach in unlocked)
        {
            text += $"• {ach.Name} (+{ach.RewardXp} XP) - {ach.UnlockedDate}\n";
        }

        achievementsText.text = text;
        // Force le scroll en haut après mise à jour
        Canvas.ForceUpdateCanvases();
        ScrollRect scrollRect = achievementsText.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;   // 1 = haut
    }

    /// <summary>
    /// Tâche 2.B - Met à jour l'affichage de l'historique avec Scroll automatique
    /// </summary>
    void UpdateHistoryDisplay()
    {
        if (currentPlayer == null || historyText == null) return;

        var history = dbManager.QuestRepo.GetQuestHistory(currentPlayer.Id, 8);

        if (history.Count == 0)
        {
            historyText.text = "Aucune quête terminée pour le moment.";
            return;
        }

        string text = "<b>Historique des quêtes terminées :</b>\n";
        foreach (var entry in history)
        {
            text += $"• {entry.QuestName} (+{entry.XpGained} XP) - {entry.CompletionDate}\n";
        }

        historyText.text = text;

        // Force le scroll en haut après mise à jour
        Canvas.ForceUpdateCanvases();
        ScrollRect scrollRect = historyText.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;   // 1 = haut

    }

    /// <summary>
    /// Tâche 2.C - Affiche les statistiques détaillées du joueur via la vue vw_PlayerStats.
    /// </summary>
    void UpdatePlayerStatsDisplay()
    {
        if (currentPlayer == null || playerStatsText == null) return;

        using (var command = dbManager.GetConnection().CreateCommand())
        {
            command.CommandText = @"
                SELECT quests_completed, achievements_unlocked, avg_xp_per_quest 
                FROM vw_PlayerStats 
                WHERE id = @playerId";

            command.Parameters.AddWithValue("@playerId", currentPlayer.Id);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    int quests = reader.GetInt32(0);
                    int achievements = reader.GetInt32(1);
                    float avgXp = reader.GetFloat(2);

                    playerStatsText.text = $"<b>Statistiques de {currentPlayer.Name} :</b>\n" +
                                           $"Quêtes terminées : {quests}\n" +
                                           $"Achievements débloqués : {achievements}\n" +
                                           $"XP moyen par quête : {avgXp:F1}";
                }
                else
                {
                    playerStatsText.text = "Aucune statistique disponible.";
                }
            }
        }
    }

    /// <summary>
    /// Tâche 3 - Supprime le joueur actuel.
    /// </summary>
    void DeleteCurrentPlayer()
    {
        if (currentPlayer == null) return;

        dbManager.PlayerRepo.DeletePlayer(currentPlayer.Id);

        // Réinitialisation de l'interface
        currentPlayer = null;
        playerInfoText.text = "Joueur supprimé.";
        questInfoText.text = "";
        achievementsText.text = "";
        historyText.text = "";
        playerStatsText.text = "";

        // Réactiver la création de joueur
        createButton.interactable = true;
        nameInput.interactable = true;
        classDropdown.interactable = true;

        Debug.Log("Joueur actuel supprimé.");
    }

    /// <summary>
    /// Tâche 3 - Exporte le classement actuel en JSON.
    /// </summary>
    void ExportLeaderboard()
    {
        string filePath = dbManager.PlayerRepo.ExportLeaderboardToJson();

        if (!string.IsNullOrEmpty(filePath))
        {
            leaderboardText.text = $"Classement exporté avec succès !\nFichier : {filePath}";
        }
        else
        {
            leaderboardText.text = "Erreur lors de l'export du classement.";
        }
    }

    /// <summary>
    /// Tâche 3 - Bonus : chargement du dernier joueur qui a joué.
    /// </summary>
    private void LoadLastPlayedPlayer()
    {
        currentPlayer = dbManager.PlayerRepo.GetLastPlayedPlayer();   // À ajouter dans PlayerRepository

        if (currentPlayer != null)
        {
            dbManager.PlayerRepo.UpdateLastPlayed(currentPlayer.Id); // Met à jour la date

            playerInfoText.text = $"Dernier joueur chargé : {currentPlayer.Name} ({currentPlayer.Class}, " +
                                  $"Niv. {currentPlayer.Level}, Exp. {currentPlayer.Experience})";
            
            panelChoice.SetActive(false);

            // Activer les boutons de jeu
            questButton.interactable = startQuestButton.interactable =
                progressQuestButton.interactable = generateQuestButton.interactable = true;

            createButton.interactable = nameInput.interactable = classDropdown.interactable = false;

            UpdateQuestInfo();
            UpdateAchievementsDisplay();
            UpdateHistoryDisplay();
            UpdatePlayerStatsDisplay();
        }
        else
        {
            playerInfoText.text = "Aucun joueur précédent trouvé.";
            ShowCreatePlayerPanel();
        }
    }

    /// <summary>
    /// Affiche le panel de création de nouveau joueur.
    /// </summary>
    private void ShowCreatePlayerPanel()
    {
        panelChoice.SetActive(false);

        // Active les champs de création
        nameInput.interactable = true;
        classDropdown.interactable = true;
        createButton.interactable = true;

        playerInfoText.text = "Création d'un nouveau joueur...";
    }
}
