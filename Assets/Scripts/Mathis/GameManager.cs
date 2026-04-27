using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    [Header("Références Base de Données")]
    [SerializeField] private DataBaseManager dbManager;

    // Ajoute ces champs dans la classe GameManager
    [Header("UI - Choix au démarrage")]
    [SerializeField] private GameObject panelChoice;           // Panel avec les 2 boutons
    [SerializeField] private Button loadLastPlayerButton;
    [SerializeField] private Button createNewPlayerButton;

    [Header("Références UI - Création")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button createButton;

    [Header("Références UI - Actions")]
    [SerializeField] private Button leaderboardButton;

    [Header("Références Textes")]
    [SerializeField] private TextMeshProUGUI playerInfoText;
    [SerializeField] private TextMeshProUGUI leaderboardText;


    [Header("Statistiques Joueur")]
    [SerializeField] private TextMeshProUGUI playerStatsText;     // ← pour la vue vw_PlayerStats

    [Header("Optimisations")]
    [SerializeField] private Button deletePlayerButton;
    [SerializeField] private Button exportJsonButton;

    private PlayerData currentPlayer;

    private static GameManager _instance;

    // Cette fonction permet d'accéder au GameManager partout via GameManager.Instance()
    public static GameManager Instance()
    {
        return _instance;
    }

    public enum GameModeType { TimeAttack, TimeTrial }
    public GameModeType currentMode;

    private void Awake()
    {
        // LA LOGIQUE MAGIQUE :
        if (_instance == null)
        {
            _instance = this;
            // Dit à Unity de ne pas détruire cet objet en changeant de scène
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si un autre GameManager existe déjà (ex: en revenant au menu), on détruit le nouveau
            Destroy(gameObject);
        }
    }

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
        leaderboardButton.onClick.AddListener(ShowLeaderboard);
        exportJsonButton.onClick.AddListener(ExportLeaderboard);

        // Ajout des listeners pour le panel de choix
        loadLastPlayerButton.onClick.AddListener(LoadLastPlayedPlayer);
        createNewPlayerButton.onClick.AddListener(ShowCreatePlayerPanel);

        // Désactivation initiale
        deletePlayerButton.interactable = false;

    }

    private bool CheckReferences()
    {
        var fields = new (UnityEngine.Object obj, string name)[]
        {
            (dbManager,          "dbManager"),
            (panelChoice,        "panelChoice"),
            (loadLastPlayerButton, "loadLastPlayerButton"),
            (createNewPlayerButton, "createNewPlayerButton"),
            (nameInput,          "nameInput"),
            (createButton,       "createButton"),
            (leaderboardButton,  "leaderboardButton"),
            (playerInfoText,     "playerInfoText"),
            (leaderboardText,    "leaderboardText"),
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
            dbManager.PlayerRepo.CreatePlayer(name);
            currentPlayer = dbManager.PlayerRepo.GetPlayerByName(name);
        }

        playerInfoText.text = $"Joueur chargé : {currentPlayer.Name} : TimeAttack {currentPlayer.BestTimeAttack}, " +
                              $"TimeTrial {currentPlayer.BestTimeTrial}, RTimeAttack {currentPlayer.BestReverseTimeAttack}, RTimeTrial {currentPlayer.BestReverseTimeTrial}";

        // Activation des boutons
        deletePlayerButton.interactable = true;

        createButton.interactable = nameInput.interactable = false;

        UpdatePlayerStatsDisplay();       
    }

    void ShowLeaderboard()
    {
        PlayerData[] leaderboard = dbManager.PlayerRepo.GetLeaderboard();
        string leaderboardString = "Classement :\n";

        for (int i = 0; i < leaderboard.Length; i++)
        {
            if (leaderboard[i] != null)
            {
                leaderboardString += $"{i + 1}. {leaderboard[i].Name} : TimeAttack {leaderboard[i].BestTimeAttack}, " +
                    $"TimeTrial {leaderboard[i].BestTimeTrial}, RTimeAttack {leaderboard[i].BestReverseTimeAttack}, RTimeTrial {leaderboard[i].BestReverseTimeTrial}\n";
            }
        }

        leaderboardText.text = leaderboardString;
    }

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
        playerStatsText.text = "";

        // Réactiver la création de joueur
        createButton.interactable = true;
        nameInput.interactable = true;

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

            playerInfoText.text = $"Dernier joueur chargé : {currentPlayer.Name} : TimeAttack {currentPlayer.BestTimeAttack}, " +
                                  $"TimeTrial {currentPlayer.BestTimeTrial}, RTimeAttack {currentPlayer.BestReverseTimeAttack}, RTimeTrial {currentPlayer.BestReverseTimeTrial}";

            panelChoice.SetActive(false);



            createButton.interactable = nameInput.interactable = false;


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
        createButton.interactable = true;

        playerInfoText.text = "Création d'un nouveau joueur...";
    }

    // Cette méthode sera appelée par un "LevelLoader" ou au Start de la scène de course
    public GameMode SetupGameMode(GameObject racingKart, LapManager lm) // Ajoute GameMode comme type de retour
    {
        GameMode mode;
        if (currentMode == GameModeType.TimeAttack)
            mode = racingKart.AddComponent<TimeAttack>();
        else
            mode = racingKart.AddComponent<ContreLaMontre>();

        mode.Initialize(lm, racingKart.GetComponent<KartScriptV2>());
        return mode; // Renvoie le mode
    }
}