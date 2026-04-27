/*using System;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public class DataBaseManager : MonoBehaviour
{
    private static DataBaseManager instance;
    private SQLiteConnection connection;

    //public PlayerRepository PlayerRepo { get; private set; }
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        string dbPath = "URI=file:" + Application.persistentDataPath + "/CocotRacing.db";
        InitializeDatabase(dbPath);

        // Initialisation des repositories
        //PlayerRepo = new PlayerRepository(connection);       
    }

    private void InitializeDatabase(string dbPath)
    {
        try
        {
            connection = new SQLiteConnection(dbPath);
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_keys = ON";
                command.ExecuteNonQuery();

                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS players (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL UNIQUE,
                        bestTimeAttack REAL,
                        bestTimeTrial REAL,
                        bestReverseTimeAttack REAL,
                        bestReverseTimeTrial REAL
                    )";
                command.ExecuteNonQuery();


                command.CommandText = @"
                    CREATE VIEW IF NOT EXISTS vw_PlayerStats AS
                    SELECT 
                        p.id,
                        p.name,
                        p.bestTimeAttack,
                        p.bestTimeTrial,     
                        P.bestReverseTimeAttack,
                        p.bestReverseTimeTrial";
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
*/