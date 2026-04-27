using System;
using System.Collections.Generic;
using System.Data.SQLite;
using UnityEngine;
using static DatabaseConstants;

/// <summary>
/// Repository responsable de toutes les opérations liées aux quêtes :
/// assignation, progression, génération dynamique, historique et nettoyage.
/// </summary>
public class QuestRepository
{
    private readonly SQLiteConnection _db;

    public QuestRepository(SQLiteConnection db)
    {
        _db = db;
    }

    /// <summary>
    /// Assigne une quête à un joueur (ou met à jour si elle existe déjà).
    /// </summary>
    public void AssignQuest(int playerId, int questId)
    {
        using (var command = _db.CreateCommand())
        {
            command.CommandText = $@"
                INSERT OR REPLACE INTO {TablePlayerQuests}
                ({ColPlayerQuestPlayerId}, {ColPlayerQuestQuestId}, {ColPlayerQuestStatus}, {ColPlayerQuestProgress})
                VALUES (@playerId, @questId, 'active', 0)";

            command.Parameters.AddWithValue("@playerId", playerId);
            command.Parameters.AddWithValue("@questId", questId);
            command.ExecuteNonQuery();
        }
        Debug.Log($"Quête {questId} assignée au joueur {playerId}.");
    }

    /// <summary>
    /// Met à jour la progression d'une quête. Si l'objectif est atteint, la quête est marquée comme terminée.
    /// </summary>
    public void UpdateQuestProgress(int playerId, int questId, int progressIncrement)
    {
        if (playerId <= 0 || questId <= 0)
        {
            Debug.LogWarning($"UpdateQuestProgress : paramètres invalides (playerId={playerId}, questId={questId})");
            return;
        }

        using (var transaction = _db.BeginTransaction())
        {
            try
            {
                using (var command = _db.CreateCommand())
                {
                    command.CommandText = @"
                    SELECT pq.progress, q.target_progress, q.reward_xp
                    FROM player_quests pq
                    JOIN quests q ON pq.quest_id = q.quest_id
                    WHERE pq.player_id = @playerId 
                      AND pq.quest_id = @questId";

                    command.Parameters.AddWithValue("@playerId", playerId);
                    command.Parameters.AddWithValue("@questId", questId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            Debug.LogWarning($"Aucune entrée trouvée dans player_quests pour playerId={playerId}, questId={questId}");
                            transaction.Rollback();
                            return;
                        }

                        int currentProgress = reader.GetInt32(0);
                        int targetProgress = reader.GetInt32(1);
                        int rewardXp = reader.GetInt32(2);
                        int newProgress = currentProgress + progressIncrement;

                        Debug.Log($"Quête {questId} : progression {currentProgress}/{targetProgress} → {newProgress}/{targetProgress}");

                        if (newProgress >= targetProgress)
                        {
                            // Quête terminée
                            using (var update = _db.CreateCommand())
                            {
                                update.CommandText = @"
                                UPDATE player_quests 
                                SET status = 'completed', progress = @target 
                                WHERE player_id = @playerId AND quest_id = @questId";

                                update.Parameters.AddWithValue("@target", targetProgress);
                                update.Parameters.AddWithValue("@playerId", playerId);
                                update.Parameters.AddWithValue("@questId", questId);
                                update.ExecuteNonQuery();
                            }
                            Debug.Log($"Quête {questId} terminée !");
                        }
                        else
                        {
                            // Mise à jour de la progression
                            using (var update = _db.CreateCommand())
                            {
                                update.CommandText = @"
                                UPDATE player_quests 
                                SET progress = @newProgress 
                                WHERE player_id = @playerId AND quest_id = @questId";

                                update.Parameters.AddWithValue("@newProgress", newProgress);
                                update.Parameters.AddWithValue("@playerId", playerId);
                                update.Parameters.AddWithValue("@questId", questId);
                                update.ExecuteNonQuery();
                            }
                        }
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.LogError($"Erreur UpdateQuestProgress (questId={questId}) : {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Récupère toutes les quêtes d'un joueur (actives ou terminées).
    /// </summary>
    public List<PlayerQuestData> GetPlayerQuests(int playerId)
    {
        List<PlayerQuestData> quests = new List<PlayerQuestData>();

        using (var command = _db.CreateCommand())
        {
            command.CommandText = $@"
                SELECT pq.{ColPlayerQuestQuestId}, q.{ColQuestName}, q.{ColQuestDescription},
                       q.{ColQuestRewardXp}, q.{ColQuestTargetProgress},
                       pq.{ColPlayerQuestStatus}, pq.{ColPlayerQuestProgress}
                FROM {TablePlayerQuests} pq
                JOIN {TableQuests} q ON pq.{ColPlayerQuestQuestId} = q.{ColQuestId}
                WHERE pq.{ColPlayerQuestPlayerId} = @playerId";

            command.Parameters.AddWithValue("@playerId", playerId);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    quests.Add(new PlayerQuestData
                    {
                        QuestId = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        RewardXp = reader.GetInt32(3),
                        TargetProgress = reader.GetInt32(4),
                        Status = reader.GetString(5),
                        Progress = reader.GetInt32(6)
                    });
                }
            }
        }
        return quests;
    }

    /// <summary>
    /// Récupère toutes les quêtes disponibles pour le dropdown.
    /// </summary>
    public List<(int questId, string name, bool isDynamic)> GetAllQuests()
    {
        List<(int questId, string name, bool isDynamic)> quests = new List<(int questId, string name, bool isDynamic)>();

        using (var command = _db.CreateCommand())
        {
            command.CommandText = $@"
                SELECT {ColQuestId}, {ColQuestName}, {ColQuestIsDynamic}
                FROM {TableQuests}
                ORDER BY {ColQuestIsDynamic}, {ColQuestName}";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    quests.Add((
                        questId: reader.GetInt32(0),
                        name: reader.GetString(1),
                        isDynamic: reader.GetInt32(2) == 1
                    ));
                }
            }
        }
        return quests;
    }

    /// <summary>
    /// Génère une quête dynamique aléatoire et l'assigne au joueur.
    /// Limite à 3 quêtes dynamiques actives.
    /// </summary>
    public bool GenerateDynamicQuest(int playerId)
    {
        if (!CanGenerateDynamicQuest(playerId))
        {
            Debug.LogWarning("Limite de 3 quêtes dynamiques actives atteinte.");
            return false;
        }

        string[] types = { "collect", "defeat", "explore" };
        string[] collectItems = { "Cristaux", "Herbes rares", "Minerais" };
        string[] defeatEnemies = { "Gobelins", "Squelettes", "Loups" };
        string[] exploreLocations = { "Forêt hantée", "Caverne sombre", "Ruines anciennes" };

        System.Random random = new System.Random();
        string type = types[random.Next(types.Length)];
        string name, description;
        int rewardXp = random.Next(50, 201);
        int targetProgress = random.Next(3, 11);

        switch (type)
        {
            case "collect":
                string item = collectItems[random.Next(collectItems.Length)];
                name = $"Collecte de {item}";
                description = $"Rassemble {targetProgress} {item.ToLower()}.";
                break;
            case "defeat":
                string enemy = defeatEnemies[random.Next(defeatEnemies.Length)];
                name = $"Vaincre des {enemy}";
                description = $"Élimine {targetProgress} {enemy.ToLower()}.";
                break;
            case "explore":
                string location = exploreLocations[random.Next(exploreLocations.Length)];
                name = $"Exploration : {location}";
                description = $"Explore {targetProgress} zones dans {location}.";
                break;
            default:
                name = "Quête mystérieuse";
                description = "Accomplis une tâche mystérieuse.";
                break;
        }

        using (var transaction = _db.BeginTransaction())
        {
            try
            {
                using (var command = _db.CreateCommand())
                {
                    command.CommandText = $@"
                        INSERT INTO {TableQuests}
                        ({ColQuestName}, {ColQuestDescription}, {ColQuestType}, 
                         {ColQuestRewardXp}, {ColQuestTargetProgress}, {ColQuestIsDynamic})
                        VALUES (@name, @description, @type, @reward, @target, 1)";

                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@description", description);
                    command.Parameters.AddWithValue("@type", type);
                    command.Parameters.AddWithValue("@reward", rewardXp);
                    command.Parameters.AddWithValue("@target", targetProgress);
                    command.ExecuteNonQuery();

                    command.CommandText = "SELECT last_insert_rowid()";
                    int questId = (int)(long)command.ExecuteScalar();

                    AssignQuest(playerId, questId);
                }

                transaction.Commit();
                Debug.Log($"Quête dynamique générée : {name}");
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.LogError($"Erreur génération quête dynamique : {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Vérifie si le joueur peut encore recevoir une nouvelle quête dynamique (max 3 actives).
    /// </summary>
    private bool CanGenerateDynamicQuest(int playerId)
    {
        using (var command = _db.CreateCommand())
        {
            command.CommandText = $@"
                SELECT COUNT(*)
                FROM {TablePlayerQuests} pq
                JOIN {TableQuests} q ON pq.{ColPlayerQuestQuestId} = q.{ColQuestId}
                WHERE pq.{ColPlayerQuestPlayerId} = @playerId 
                  AND q.{ColQuestIsDynamic} = 1 
                  AND pq.{ColPlayerQuestStatus} = 'active'";

            command.Parameters.AddWithValue("@playerId", playerId);
            return (long)command.ExecuteScalar() < 3;
        }
    }

    /// <summary>
    /// Nettoie les quêtes TERMINÉES du joueur (dynamiques et statiques) :
    /// 1. Insère dans l'historique
    /// 2. Supprime les liaisons terminées dans player_quests
    /// 3. Supprime les quêtes dynamiques orphelines
    /// </summary>
    public void CleanDynamicQuests(int playerId)
    {
        if (playerId <= 0) return;

        using (var transaction = _db.BeginTransaction())
        {
            try
            {
                // 1. Insérer dans l'historique toutes les quêtes terminées
                using (var cmd = _db.CreateCommand())
                {
                    cmd.CommandText = @"
                    INSERT INTO quest_history 
                    (player_id, quest_id, quest_name, completion_date, xp_gained)
                    SELECT 
                        pq.player_id,
                        pq.quest_id,
                        q.name,
                        @date,
                        q.reward_xp
                    FROM player_quests pq
                    JOIN quests q ON pq.quest_id = q.quest_id
                    WHERE pq.player_id = @playerId 
                      AND pq.status = 'completed'";

                    cmd.Parameters.AddWithValue("@playerId", playerId);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }

                // 2. Supprimer TOUTES les liaisons terminées (c'est le point le plus important)
                using (var cmd = _db.CreateCommand())
                {
                    cmd.CommandText = @"
                    DELETE FROM player_quests 
                    WHERE player_id = @playerId 
                      AND status = 'completed'";
                    cmd.Parameters.AddWithValue("@playerId", playerId);
                    int rowsDeleted = cmd.ExecuteNonQuery();
                    Debug.Log($"{rowsDeleted} quêtes terminées supprimées de player_quests.");
                }

                // 3. Supprimer les quêtes dynamiques qui ne sont plus utilisées par aucun joueur
                using (var cmd = _db.CreateCommand())
                {
                    cmd.CommandText = @"
                    DELETE FROM quests 
                    WHERE is_dynamic = 1 
                      AND quest_id NOT IN (SELECT quest_id FROM player_quests)";
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                Debug.Log("Nettoyage terminé avec succès : quêtes terminées supprimées.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.LogError($"Erreur dans CleanDynamicQuests : {ex.Message}");
            }
        }
    }
    /// <summary>
    /// TÂCHE 2.B - Récupère l'historique des quêtes terminées d'un joueur.
    /// Retourne les dernières quêtes terminées, triées par date décroissante.
    /// </summary>
    /// <param name="playerId">ID du joueur</param>
    /// <param name="limit">Nombre maximum d'entrées à retourner (par défaut 10)</param>
    /// <returns>Liste des entrées d'historique</returns>
    public List<QuestHistoryEntry> GetQuestHistory(int playerId, int limit = 10)
    {
        List<QuestHistoryEntry> history = new List<QuestHistoryEntry>();

        using (var command = _db.CreateCommand())
        {
            command.CommandText = $@"
                SELECT {ColQuestHistoryQuestName}, 
                       {ColQuestHistoryCompletionDate}, 
                       {ColQuestHistoryXpGained}
                FROM {TableQuestHistory}
                WHERE {ColQuestHistoryPlayerId} = @playerId
                ORDER BY {ColQuestHistoryCompletionDate} DESC
                LIMIT @limit";

            command.Parameters.AddWithValue("@playerId", playerId);
            command.Parameters.AddWithValue("@limit", limit);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    history.Add(new QuestHistoryEntry(
                        reader.GetString(0),   // QuestName
                        reader.GetString(1),   // CompletionDate
                        reader.GetInt32(2)     // XpGained
                    ));
                }
            }
        }
        return history;
    }
}
