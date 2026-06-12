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

    private void LoadBestScore()
    {
        LeaderboardCircuitMode circuitMode = GetCurrentCircuitMode();

        if (LeaderboardService.Instance != null &&
            LeaderboardService.Instance.TryGetLocalBestScore(LeaderboardGameMode.TimeAttack, circuitMode, out int bestTimeInMs))
        {
            bestLapTime = bestTimeInMs / 1000f;
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
        int lapTimeInMs = Mathf.RoundToInt(lapTime * 1000f);
        bool isReverse = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted);
        bool scoreAccepted = isReverse
            ? LeaderboardService.EnsureInstance().SubmitTimeAttackReverse(lapTimeInMs)
            : LeaderboardService.EnsureInstance().SubmitTimeAttackNormal(lapTimeInMs);

        // L'affichage local reste un feedback immédiat, mais l'envoi est décidé par profil/mode/variante dans LeaderboardService.
        if (scoreAccepted && lapTime < bestLapTime)
        {
            bestLapTime = lapTime;

            if (bestScoreUI != null)
                bestScoreUI.text = "NOUVEAU RECORD : " + FormatTime(bestLapTime);
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

    private LeaderboardCircuitMode GetCurrentCircuitMode()
    {
        return InversionCatcher.instance != null && InversionCatcher.instance.Inverted
            ? LeaderboardCircuitMode.Reverse
            : LeaderboardCircuitMode.Normal;
    }
}
