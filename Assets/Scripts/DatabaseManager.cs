using System;
using System.Data.SQLite;
using UnityEngine;

/// <summary>
/// Point d'entrée principal pour la gestion de la base de données SQLite de Hero Quest.
/// Utilise le pattern Repository pour séparer la logique d'accès aux données.
/// Contient l'initialisation de toutes les tables, vues et données de base.
/// </summary>
public class DatabaseManager : MonoBehaviour
{
    private static DatabaseManager instance;
    private SQLiteConnection connection;

    /// <summary>
    /// Repository pour toutes les opérations liées aux joueurs (création, XP, achievements, classement...).
    /// </summary>
    public PlayerRepository PlayerRepo { get; private set; }

    /// <summary>
    /// Repository pour toutes les opérations liées aux quêtes (assignation, progression, historique, nettoyage...).
    /// </summary>
    public QuestRepository QuestRepo { get; private set; }

    void Awake()
    {
        // Pattern Singleton : une seule instance dans la scène
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        string dbPath = "URI=file:" + Application.persistentDataPath + "/HeroQuest.db";
        InitializeDatabase(dbPath);

        // Initialisation des repositories
        PlayerRepo = new PlayerRepository(connection);
        QuestRepo = new QuestRepository(connection);
    }

    /// <summary>
    /// Initialise la connexion SQLite et crée toutes les tables, indexes, vues et données de base.
    /// </summary>
    private void InitializeDatabase(string dbPath)
    {
        try
        {
            connection = new SQLiteConnection(dbPath);
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                // Activation des clés étrangères
                command.CommandText = "PRAGMA foreign_keys = ON";
                command.ExecuteNonQuery();

                // ====================== TABLES PRINCIPALES ======================

                // Table des joueurs
                // ====================== TABLE PLAYERS AVEC CONTRAINTES CHECK (Tâche 3) ======================
                // level ne peut pas être à zéro
                // experience est forcément >= 0
                // Table des joueurs avec LastPlayed pour charger le dernier joueur
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS players (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL UNIQUE,
                        class TEXT NOT NULL,
                        level INTEGER NOT NULL CHECK (level >= 1),
                        experience INTEGER NOT NULL CHECK (experience >= 0),
                        LastPlayed DATETIME DEFAULT CURRENT_TIMESTAMP
                    )";
                command.ExecuteNonQuery();

                // Table des quêtes
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS quests (
                        quest_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        description TEXT,
                        type TEXT NOT NULL,
                        reward_xp INTEGER NOT NULL,
                        target_progress INTEGER NOT NULL,
                        is_dynamic INTEGER NOT NULL DEFAULT 0
                    )";
                command.ExecuteNonQuery();

                // Table de liaison joueur/quête
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS player_quests (
                        player_id INTEGER,
                        quest_id INTEGER,
                        status TEXT NOT NULL,
                        progress INTEGER NOT NULL,
                        PRIMARY KEY (player_id, quest_id),
                        FOREIGN KEY (player_id) REFERENCES players(id),
                        FOREIGN KEY (quest_id) REFERENCES quests(quest_id)
                    )";
                command.ExecuteNonQuery();

                // Index pour optimiser les recherches
                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_player_quests ON player_quests(player_id)";
                command.ExecuteNonQuery();

                // ====================== TABLES POUR L'ÉVALUATION ======================

                // Table des achievements (Tâche 2.A)
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS achievements (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        description TEXT NOT NULL,
                        condition_type TEXT NOT NULL,
                        condition_value INTEGER NOT NULL,
                        reward_xp INTEGER NOT NULL
                    )";
                command.ExecuteNonQuery();

                // Table de liaison player_achievements (Tâche 2.A)
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS player_achievements (
                        player_id INTEGER,
                        achievement_id INTEGER,
                        unlocked_date TEXT NOT NULL,
                        PRIMARY KEY (player_id, achievement_id),
                        FOREIGN KEY (player_id) REFERENCES players(id),
                        FOREIGN KEY (achievement_id) REFERENCES achievements(id)
                    )";
                command.ExecuteNonQuery();

                // Table d'historique des quêtes terminées (Tâche 2.B)
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS quest_history (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        player_id INTEGER,
                        quest_id INTEGER,
                        quest_name TEXT NOT NULL,
                        completion_date TEXT NOT NULL,
                        xp_gained INTEGER NOT NULL,
                        FOREIGN KEY (player_id) REFERENCES players(id)
                    )";
                command.ExecuteNonQuery();

                // ====================== VUE POUR LA TÂCHE 2.C ======================

                // Vue de statistiques améliorée (Tâche 2.C)
                // Compte les quêtes terminées via l'historique (car les quêtes terminées sont nettoyées de player_quests)
                command.CommandText = @"
                    CREATE VIEW IF NOT EXISTS vw_PlayerStats AS
                    SELECT 
                        p.id,
                        p.name,
                        p.class,
                        p.level,
                        p.experience,
                        
                        -- Nombre de quêtes terminées (basé sur l'historique - Tâche 2.B)
                        COALESCE((
                            SELECT COUNT(*) 
                            FROM quest_history 
                            WHERE player_id = p.id
                        ), 0) AS quests_completed,
                        
                        -- Nombre d'achievements débloqués (Tâche 2.A)
                        COUNT(DISTINCT pa.achievement_id) AS achievements_unlocked,
                        
                        -- XP moyen par quête terminée (basé sur l'historique)
                        CASE 
                            WHEN COALESCE((
                                SELECT COUNT(*) 
                                FROM quest_history 
                                WHERE player_id = p.id
                            ), 0) > 0 
                            THEN ROUND(CAST(p.experience AS FLOAT) / 
                                 COALESCE((
                                    SELECT COUNT(*) 
                                    FROM quest_history 
                                    WHERE player_id = p.id
                                 ), 1), 1)
                            ELSE 0 
                        END AS avg_xp_per_quest
                    FROM players p
                    LEFT JOIN player_achievements pa ON p.id = pa.player_id
                    GROUP BY p.id";
                command.ExecuteNonQuery();

                // ====================== DONNÉES DE BASE ======================

                // Achievements de base (Tâche 2.A)
                command.CommandText = "SELECT COUNT(*) FROM achievements";
                if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                {
                    command.CommandText = @"
                        INSERT INTO achievements (name, description, condition_type, condition_value, reward_xp)
                        VALUES 
                            ('Premier pas', 'Atteindre le niveau 5', 'level', 5, 50),
                            ('Chasseur de quêtes', 'Terminer 5 quêtes', 'quests_completed', 5, 100),
                            ('Légende vivante', 'Atteindre 1000 XP', 'xp_total', 1000, 200)";
                    command.ExecuteNonQuery();
                }

                // Quêtes statiques de base
                command.CommandText = "SELECT COUNT(*) FROM quests WHERE is_dynamic = 0";
                if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                {
                    command.CommandText = @"
                        INSERT INTO quests (name, description, type, reward_xp, target_progress, is_dynamic)
                        VALUES 
                            ('Chasse au trésor', 'Trouver le trésor caché dans la forêt.', 'collect', 100, 5, 0),
                            ('Vaincre le dragon', 'Tuer le dragon rouge dans la montagne.', 'defeat', 200, 10, 0),
                            ('Sauver le village', 'Protéger le village des bandits.', 'explore', 150, 7, 0)";
                    command.ExecuteNonQuery();
                }

                // ====================== INDEXES - Tâche 3 ======================
                // Index sur l'historique (recherches fréquentes par joueur)
                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_quest_history_player ON quest_history(player_id)";
                command.ExecuteNonQuery();

                // Index sur les achievements
                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_player_achievements_player ON player_achievements(player_id)";
                command.ExecuteNonQuery();

                // Index sur les quêtes dynamiques
                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_quests_dynamic ON quests(is_dynamic)";
                command.ExecuteNonQuery();
            }

            Debug.Log("Base de données initialisée avec succès (vue vw_PlayerStats mise à jour).");
        }
        catch (Exception ex)
        {
            Debug.LogError("Erreur d'initialisation de la base de données : " + ex.Message);
        }
    }

    /// <summary>
    /// Retourne la connexion SQLite active (utilisée par GameManager pour lire la vue vw_PlayerStats - Tâche 2.C).
    /// </summary>
    public SQLiteConnection GetConnection()
    {
        return connection;
    }

    /// <summary>
    /// Ferme proprement la connexion à la base lors de la destruction de l'objet.
    /// </summary>
    void OnDestroy()
    {
        if (connection != null && connection.State == System.Data.ConnectionState.Open)
        {
            connection.Close();
        }
    }
}
