using UnityEngine;
using LootLocker.Requests;

public class LootLockerLeaderboard : MonoBehaviour
{
    public static LootLockerLeaderboard Instance;
    public string leaderboardKey = "main_race";
    private bool isConnected = false;

    void Awake()
    {
        // On s'assure qu'il n'y a qu'un seul Manager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (response.success)
            {
                Debug.Log("Connexion LootLocker réussie !");
                isConnected = true;

                EnvoyerScore(1);
            }
            else
            {
                Debug.Log("Échec : " + response.errorData.message);
            }
        });
    }

    public void EnvoyerScore(int score)
    {
        if (!isConnected)
        {
            Debug.LogWarning("LootLocker n'est pas encore prêt !");
            return;
        }

        LootLockerSDKManager.SubmitScore("", score, leaderboardKey, (response) =>
        {
            if (response.success) Debug.Log("Score envoyé !");
        });
    }
}