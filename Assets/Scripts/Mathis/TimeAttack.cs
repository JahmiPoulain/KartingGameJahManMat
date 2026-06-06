using UnityEngine;
using System.Collections;
using TMPro;

public class TimeAttack : GameMode
{
    [Header("Références")]
    [SerializeField] private TextMeshProUGUI startUI;
    [SerializeField] private TextMeshProUGUI currentTimerUI;
    [SerializeField] private TextMeshProUGUI bestScoreUI; // Pour afficher le record

    private float bestLapTime = float.MaxValue;
    private const string BASE_BEST_TIME_KEY = "BestTimeAttackScore";

    private void Start()
    {
        lapManager = FindFirstObjectByType<LapManager>();
        Initialize(lapManager, FindFirstObjectByType<KartScriptV2>());

        startUI = GameObject.Find("CountDownUI")?.GetComponent<TextMeshProUGUI>();
        currentTimerUI = GameObject.Find("ChronoUI")?.GetComponent<TextMeshProUGUI>();
        bestScoreUI = GameObject.Find("BestScoreUI")?.GetComponent<TextMeshProUGUI>();

        maxLaps = 99999;
        LoadBestScore();
        if (kartScript != null) kartScript.CanDrive = false;
        StartCoroutine(InitialCountdown());
    }

    public override void Initialize(LapManager lm, KartScriptV2 ks)
    {
        base.Initialize(lm, ks);
        this.currentTimerUI = lm.ChronoUI;
    }

    private void Update()
    {
        if (raceStarted && !raceFinished)
        {
            // Logique de mise à jour si nécessaire
        }
    }

    // Génère dynamiquement la clé selon l'état d'inversion de la map
    private string GetSavedKey()
    {
        string suffix = (MainMenuUIManager.Instance != null && MainMenuUIManager.Instance.isMapInverted) ? "_Inverted" : "_Normal";
        return BASE_BEST_TIME_KEY + suffix;
    }

    private void LoadBestScore()
    {
        string key = GetSavedKey();
        if (PlayerPrefs.HasKey(key))
        {
            bestLapTime = PlayerPrefs.GetFloat(key);
            if (bestScoreUI != null)
                bestScoreUI.text = "MEILLEUR TEMPS : " + FormatTime(bestLapTime);
        }
        else
        {
            bestLapTime = float.MaxValue;
            if (bestScoreUI != null)
                bestScoreUI.text = "MEILLEUR TEMPS : --:--.--";
        }
    }

    IEnumerator InitialCountdown()
    {
        if (kartScript != null) kartScript.CanDrive = false;

        yield return new WaitForSeconds(1);

        if (startUI != null) startUI.text = "3";
        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "2";
        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "1";
        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "GO!";

        if (kartScript != null) kartScript.CanDrive = true;
        raceStarted = true;

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "";
    }

    public override void OnLapCompleted(float lapTime)
    {
        // Si le joueur bat son record sur le type de piste actuel
        if (lapTime < bestLapTime)
        {
            bestLapTime = lapTime;
            PlayerPrefs.SetFloat(GetSavedKey(), bestLapTime);
            PlayerPrefs.Save();

            if (bestScoreUI != null)
                bestScoreUI.text = "NOUVEAU RECORD : " + FormatTime(bestLapTime);

            // --- ENVOI ADAPTÉ AU LEADERBOARD EN LIGNE (LOOTLOCKER) ---
            if (LeaderboardManager.Instance != null)
            {
                int lapTimeInMs = Mathf.RoundToInt(lapTime * 1000f);
                bool isReverse = (MainMenuUIManager.Instance != null && MainMenuUIManager.Instance.isMapInverted);

                if (isReverse)
                {
                    LeaderboardManager.Instance.SubmitTimeAttackReverse(lapTimeInMs);
                }
                else
                {
                    LeaderboardManager.Instance.SubmitTimeAttackNormal(lapTimeInMs);
                }

                Debug.Log($"Score TimeAttack envoyé au manager (Reverse: {isReverse}) : {lapTimeInMs} ms");
            }
        }
    }

    public override void CompleteRace()
    {
        // Le mode Time Attack est infini, pas de fin de course automatique requise ici
    }

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        float seconds = time % 60;
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00}:{1:00.000}", minutes, seconds);
    }
}