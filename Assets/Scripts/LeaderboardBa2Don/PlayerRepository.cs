using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using UnityEngine;
using static DataBaseConstants;

/// <summary>
/// Repository responsable de toutes les opérations liées aux joueurs :
/// création, mise à jour d'expérience, achievements, classement et suppression.
/// </summary>
public class PlayerRepository
{
    private readonly SQLiteConnection _db;

    public PlayerRepository(SQLiteConnection db)
    {
        _db = db;
    }

    /// <summary>
    /// Crée un nouveau joueur dans la base de données.
    /// </summary>
    /// <param name="name">Nom unique du joueur</param>
    /// <param name="playerClass">Classe du joueur (Guerrier, Mage, etc.)</param>
    public void CreatePlayer(string name)
    {
        using (var transaction = _db.BeginTransaction())
        {
            try
            {
                using (var command = _db.CreateCommand())
                {
                    command.CommandText = $@"
                        INSERT INTO {TablePlayers} 
                        ({ColPlayerName}, {ColPlayerBestTimeAttack}, {ColPlayerBestTimeTrial}, {ColPlayerBestReverseTimeAttack}, {ColPlayerBestReverseTimeTrial})
                        VALUES (@name, @class, 1, 0)";

                    command.Parameters.AddWithValue("@name", name);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
                Debug.Log($"Joueur créé avec succès : {name}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.LogError($"Erreur lors de la création du joueur {name} : {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Met à jour l'expérience et le niveau d'un joueur.
    /// Utilise une transaction et vérifie automatiquement les achievements.
    /// </summary>
    /// <param name="playerId">ID du joueur</param>
    /// <param name="experienceGain">Quantité d'XP à ajouter</param>
    public void UpdatePlayerExperience(int playerId, int experienceGain)
    {
        using (var transaction = _db.BeginTransaction())
        {
            try
            {
                using (var command = _db.CreateCommand())
                {
                    command.CommandText = $@"
                        UPDATE {TablePlayers}
                        SET {ColPlayerName}, {ColPlayerBestTimeAttack}, {ColPlayerBestTimeTrial}, {ColPlayerBestReverseTimeAttack}, {ColPlayerBestReverseTimeTrial}
                        WHERE {ColPlayerId} = @id";

                    command.Parameters.AddWithValue("@gain", experienceGain);
                    command.Parameters.AddWithValue("@id", playerId);
                    command.ExecuteNonQuery();
                }
            
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.LogError($"Erreur lors de la mise à jour de l'expérience du joueur {playerId} : {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Récupère un joueur par son nom.
    /// </summary>
    /// <param name="name">Nom du joueur à rechercher</param>
    /// <returns>Objet PlayerData ou null si non trouvé</returns>
    public PlayerData GetPlayerByName(string name)
    {
        using (var command = _db.CreateCommand())
        {
            command.CommandText = $@"
                SELECT {ColPlayerName}, {ColPlayerBestTimeAttack}, {ColPlayerBestTimeTrial}, {ColPlayerBestReverseTimeAttack}, {ColPlayerBestReverseTimeTrial}
                FROM {TablePlayers}
                WHERE {ColPlayerName} = @name";

            command.Parameters.AddWithValue("@name", name);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new PlayerData
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        BestTimeAttack = reader.GetFloat(2),
                        BestTimeTrial = reader.GetFloat(3),
                        BestReverseTimeAttack = reader.GetFloat(4),
                        BestReverseTimeTrial = reader.GetFloat(5)
                    };
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Récupère un joueur par son ID.
    /// </summary>
    /// <param name="id">ID du joueur</param>
    /// <returns>Objet PlayerData ou null si non trouvé</returns>
    public PlayerData GetPlayerById(int id)
    {
        using (var command = _db.CreateCommand())
        {
            command.CommandText = $@"
                SELECT {ColPlayerName}, {ColPlayerBestTimeAttack}, {ColPlayerBestTimeTrial}, {ColPlayerBestReverseTimeAttack}, {ColPlayerBestReverseTimeTrial}
                FROM {TablePlayers}
                WHERE {ColPlayerId} = @id";

            command.Parameters.AddWithValue("@id", id);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new PlayerData
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        BestTimeAttack = reader.GetFloat(2),
                        BestTimeTrial = reader.GetFloat(3),
                        BestReverseTimeAttack = reader.GetFloat(4),
                        BestReverseTimeTrial = reader.GetFloat(5)
                    };
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Tâche 2.C + 3 - Retourne le classement amélioré en utilisant la vue vw_PlayerStats.
    /// Tri par expérience → nombre de quêtes terminées → nombre d'achievements.
    /// </summary>
    public PlayerData[] GetLeaderboard()
    {
        PlayerData[] leaderboard = new PlayerData[5];
        int index = 0;

        using (var command = _db.CreateCommand())
        {
            command.CommandText = @"
            SELECT id, name, class, level, experience 
            FROM vw_PlayerStats 
            ORDER BY experience DESC, 
                     quests_completed DESC, 
                     achievements_unlocked DESC 
            LIMIT 5";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read() && index < 5)
                {
                    leaderboard[index] = new PlayerData
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        BestTimeAttack = reader.GetFloat(2),
                        BestTimeTrial = reader.GetFloat(3),
                        BestReverseTimeAttack = reader.GetFloat(4),
                        BestReverseTimeTrial = reader.GetFloat(5)
                    };
                    index++;
                }
            }
        }

        // Debug pour voir si des données sont récupérées
        Debug.Log($"GetLeaderboard() → {index} joueurs trouvés dans le classement.");
        return leaderboard;
    }

    /// <summary>
    /// Tâche 3 - Supprime complètement un joueur et toutes ses données associées 
    /// (player_quests, quest_history, player_achievements).
    /// </summary>
    /// <param name="playerId">ID du joueur à supprimer</param>
    public void DeletePlayer(int playerId)
    {
        using (var transaction = _db.BeginTransaction())
        {
            try
            {
                // Suppression dans l'ordre inverse des dépendances
                using (var cmd = _db.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM quest_history WHERE player_id = @id";
                    cmd.Parameters.AddWithValue("@id", playerId);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "DELETE FROM player_achievements WHERE player_id = @id";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "DELETE FROM player_quests WHERE player_id = @id";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "DELETE FROM players WHERE id = @id";
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                Debug.Log($"Joueur ID {playerId} supprimé complètement avec succès.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.LogError($"Erreur lors de la suppression du joueur {playerId} : {ex.Message}");
            }
        }
    } 

    /// <summary>
    /// Tâche 3 - Export du classement en JSON (version corrigée et robuste).
    /// Utilise une classe dédiée pour éviter les problèmes de sérialisation avec JsonUtility.
    /// </summary>
    /*public string ExportLeaderboardToJson()
    {
        var leaderboardArray = GetLeaderboard();

        Debug.Log($"ExportLeaderboardToJson() → {leaderboardArray.Count(p => p != null)} joueurs trouvés.");

        // Création d'une structure claire et sérialisable
        var exportData = new LeaderboardExportData
        {
            exportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            totalPlayersInRanking = leaderboardArray.Count(p => p != null),
            leaderboard = leaderboardArray
                .Where(p => p != null)
                .Select((p, index) => new LeaderboardEntry
                {
                    rank = index + 1,
                    name = p.Name,
                    className = p.Class,
                    level = p.Level,
                    experience = p.Experience
                })
                .ToList()
        };

        string json = JsonUtility.ToJson(exportData, true); // true = formaté (indenté)

        string filePath = Path.Combine(Application.persistentDataPath, "HeroQuest_Leaderboard.json");

        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"✅ Classement exporté avec succès vers : {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Erreur lors de l'export JSON : {ex.Message}");
            return null;
        }
    }*/

    /// <summary>
    /// Met à jour la date de dernier jeu du joueur (pour le chargement automatique).
    /// </summary>
    public void UpdateLastPlayed(int playerId)
    {
        using (var command = _db.CreateCommand())
        {
            command.CommandText = $@"
                UPDATE {TablePlayers} 
                SET LastPlayed = CURRENT_TIMESTAMP 
                WHERE {ColPlayerId} = @id";

            command.Parameters.AddWithValue("@id", playerId);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Récupère le dernier joueur joué (basé sur la colonne LastPlayed).
    /// </summary>
    /// <returns>PlayerData du dernier joueur ou null</returns>
    public PlayerData GetLastPlayedPlayer()
    {
        using (var command = _db.CreateCommand())
        {
            command.CommandText = $@"
                SELECT {ColPlayerName}, {ColPlayerBestTimeAttack}, {ColPlayerBestTimeTrial}, {ColPlayerBestReverseTimeAttack}, {ColPlayerBestReverseTimeTrial}
                FROM {TablePlayers}
                ORDER BY LastPlayed DESC
                LIMIT 1";

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new PlayerData
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        BestTimeAttack = reader.GetFloat(2),
                        BestTimeTrial = reader.GetFloat(3),
                        BestReverseTimeAttack = reader.GetFloat(4),
                        BestReverseTimeTrial = reader.GetFloat(5)
                    };
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Classe dédiée pour l'export JSON du classement (Tâche 3).
    /// </summary>
    [System.Serializable]
    public class LeaderboardExportData
    {
        public string exportDate;
        public int totalPlayersInRanking;
        public List<LeaderboardEntry> leaderboard;
    }

    /// <summary>
    /// Une entrée individuelle du classement pour l'export JSON.
    /// </summary>
    [System.Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string name;
        public string className;
        public int level;
        public int experience;
    }
}
