using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;
using System.Collections.Generic;
using LootLocker.Extension.DataTypes;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;
    
    [Header("Leaderboard Setup")]
    [SerializeField] private string leaderboardKey;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject scrollbar;
    [SerializeField] private int maxResults;
      
    private readonly List<GameObject> spawnedEntries = new();

    private bool isConnected = false;
    private bool isLoading = false;
    private int localPlayerId;
    private Pcg32 rng; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        rng = Pcg32.CreateCombined();

        // Expression Lambda / Fonction Callback
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            // Si le joueur n'a pas encore de nom
            if (string.IsNullOrEmpty(response.player_name))
            {
                // On fabrique son nom unique à partir de son ID LootLocker publique
                // On prend les 5 premiers caractères de cet ID pour pas que ce soit trop long
                string uniqueName = "Joueur_" + response.public_uid[..4];

                // On envoie ce nom au serveur
                LootLockerSDKManager.SetPlayerName(uniqueName, (nameResponse) =>
                {
                    if (nameResponse.success)
                    {
                        Debug.Log("Nom unique enregistré : " + uniqueName);
                    }
                });
            }

            localPlayerId = response.player_id;
            isConnected = true;
            RefreshLeaderboard();
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && isConnected)
        {
            SubmitScoreAndRefresh(rng.Range(10000, 200000)); 
        }
    }

    public void SubmitScoreAndRefresh(int timeInMilliseconds)
    {
        if (!isConnected) return;

        LootLockerSDKManager.SubmitScore("", timeInMilliseconds, leaderboardKey, (scoreResponse) =>
        {
            if (scoreResponse.success)
                RefreshLeaderboard();
        });
    }

    public void RefreshLeaderboard()
    {
        if (isLoading) return;
        isLoading = true;

        LootLockerSDKManager.GetScoreList(leaderboardKey, maxResults, 0, (response) =>
        {
            if (!response.success)
            {
                isLoading = false;
                return;
            }

            LootLockerLeaderboardMember[] items = response.items ?? new LootLockerLeaderboardMember[0];

            // Nettoyage des anciennes entrées
            foreach (var obj in spawnedEntries)
            {
                Destroy(obj);
            }
            spawnedEntries.Clear();

            int displayCount = Mathf.Max(items.Length, 5);

            for (int i = 0; i < displayCount; i++)
            {
                GameObject obj = Instantiate(entryPrefab, contentParent);
                spawnedEntries.Add(obj);

                LeaderboardEntryUI entry = obj.GetComponent<LeaderboardEntryUI>();

                if (entry == null)
                {
                    Debug.LogError("Prefab sans LeaderboardEntryUI !");
                    continue;
                }

                if (i < items.Length)
                {
                    var item = items[i];

                    // Couleur joueur local
                    if (item.player != null && item.player.id == localPlayerId)
                        entry.SetColor(Color.yellow);
                    else
                        entry.SetColor(Color.white);

                    // Rank
                    entry.rankText.text = "#" + item.rank;

                    // Nom
                    string displayName = "Unknown";
                    if (item.player != null)
                    {
                        if (!string.IsNullOrEmpty(item.player.name))
                            displayName = item.player.name;
                        else
                            displayName = "Player " + item.player.id;
                    }
                    entry.nameText.text = displayName;

                    // Score
                    entry.scoreText.text = FormatTime(item.score);
                }
                else
                {
                    // Ligne vide
                    entry.rankText.text = "#" + (i + 1);
                    entry.nameText.text = "--";
                    entry.scoreText.text = "--:--.--";
                }

                // Alternance visuelle
                Image bg = obj.GetComponent<Image>();
                if (bg != null && i % 2 == 0)
                    bg.color = new Color(1f, 1f, 1f, 0.05f);
            }

            // Activation du scroll si nécessaire
            bool needScroll = items.Length > 5;
            scrollRect.vertical = needScroll;
            scrollbar.SetActive(needScroll);

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;

            isLoading = false;
        });
    }

    private string FormatTime(int milliseconds)
    {
        int min = milliseconds / 60000;
        int sec = milliseconds / 1000 % 60;
        int ms = milliseconds % 1000;

        return string.Format("{0:00}:{1:00}.{2:000}", min, sec, ms);
    }
}