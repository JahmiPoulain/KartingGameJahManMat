using UnityEngine;
using LootLocker.Requests;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public string leaderboardKey = "main_race";
    
    [Header("UI Setup")]
    public LeaderboardEntryUI[] entries = new LeaderboardEntryUI[5]; // Glisse tes 15 TMP ici (3x5)
    
    private bool isConnected = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // On commence par connecter le joueur
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (response.success)
            {
                Debug.Log("Connexion LootLocker réussie !");
                isConnected = true;
                RefreshLeaderboard(); // On affiche le classement dès la connexion
            }
        });
    }

    // Fonction pour envoyer le score ET le nom du joueur
    public void SubmitScoreAndRefresh(string playerName, int timeInSeconds)
    {
        if (!isConnected) return;

        // 1. On définit d'abord le nom du joueur sur LootLocker
        LootLockerSDKManager.SetPlayerName(playerName, (nameResponse) =>
        {
            if (nameResponse.success)
            {
                // 2. On envoie le score (LootLocker gère si c'est un record ou non selon tes settings)
                LootLockerSDKManager.SubmitScore("", timeInSeconds, leaderboardKey, (scoreResponse) =>
                {
                    if (scoreResponse.success)
                    {
                        Debug.Log("Score envoyé, actualisation du classement...");
                        RefreshLeaderboard();
                    }
                });
            }
        });
    }

    public void RefreshLeaderboard()
    {
        // On demande les 5 meilleurs scores
        LootLockerSDKManager.GetScoreList(leaderboardKey, 5, 0, (response) =>
        {
            if (response.success)
            {
                LootLockerLeaderboardMember[] items = response.items;

                for (int i = 0; i < entries.Length; i++)
                {
                    if (i < items.Length) // Si un joueur existe à cette position
                    {
                        entries[i].rankText.text = items[i].rank.ToString();
                        
                        // On récupère le nom (soit le pseudo, soit l'ID si pas de nom)
                        string name = items[i].player.name;
                        if (string.IsNullOrEmpty(name)) name = "Player " + items[i].player.id;
                        
                        entries[i].nameText.text = name;
                        entries[i].scoreText.text = FormatTime(items[i].score);
                    }
                    else // Si la place est vide (moins de 5 joueurs au total)
                    {
                        entries[i].rankText.text = (i + 1).ToString();
                        entries[i].nameText.text = "---";
                        entries[i].scoreText.text = "--:--";
                    }
                }
            }
        });
    }

    // Petit utilitaire pour transformer des secondes en format 00:00
    private string FormatTime(int seconds)
    {
        int min = seconds / 60;
        int sec = seconds % 60;
        return string.Format("{0:00}:{1:00}", min, sec);
    }
}

[System.Serializable]
public class LeaderboardEntryUI
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
}