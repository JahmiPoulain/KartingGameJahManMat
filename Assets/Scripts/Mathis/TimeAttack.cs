using UnityEngine;
using System.Collections;
using TMPro;

public class TimeAttack : GameMode
{
    [Header("Références")]
    [SerializeField] private LapManager lapManager;
    [SerializeField] private KartScriptV2 kartScript;
    [SerializeField] private TextMeshProUGUI startUI;
    [SerializeField] private TextMeshProUGUI currentTimerUI;
    [SerializeField] private TextMeshProUGUI bestScoreUI; // Pour afficher le record

    private float bestLapTime = float.MaxValue;
    private const string BEST_TIME_KEY = "BestTimeAttackScore";

    private void Start()
    {
        maxLaps = 999; // Mode infini, le joueur enchaîne sans arrêt 
        LoadBestScore();
        StartCoroutine(InitialCountdown());
    }

    private void Update()
    {
        if (raceStarted && !raceFinished)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // Affichage du chrono actuel 
        currentTimerUI.text = FormatTime(lapManager.CurrentLapTime);
    }

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        float seconds = time % 60;
        return $"{minutes:00}:{seconds:00.000}";
    }

    private void LoadBestScore()
    {
        if (PlayerPrefs.HasKey(BEST_TIME_KEY))
        {
            bestLapTime = PlayerPrefs.GetFloat(BEST_TIME_KEY);
            bestScoreUI.text = "Record : " + FormatTime(bestLapTime);
        }
        else
        {
            bestScoreUI.text = "Record : --:--.---";
        }
    }

    IEnumerator InitialCountdown()
    {
        kartScript.canDrive = false;
        startUI.text = "3";
        yield return new WaitForSeconds(1);
        startUI.text = "2";
        yield return new WaitForSeconds(1);
        startUI.text = "1";
        yield return new WaitForSeconds(1);
        startUI.text = "GO!";

        kartScript.canDrive = true;
        raceStarted = true;

        yield return new WaitForSeconds(1);
        startUI.text = "";
    }

    // Appelé par LapManager à chaque franchissement de ligne 
    public override void CompleteRace()
    {
        // Récupère le temps du tour venant d'être complété
        float lastLapTime = lapManager.LapTimes[lapManager.CurrentLap - 2];

        // Vérifie si c'est un nouveau record 
        if (lastLapTime < bestLapTime)
        {
            bestLapTime = lastLapTime;
            PlayerPrefs.SetFloat(BEST_TIME_KEY, bestLapTime);
            PlayerPrefs.Save();
            bestScoreUI.text = "NOUVEAU RECORD : " + FormatTime(bestLapTime);
        }

        // Note : On ne met pas raceFinished = true car le joueur enchaîne 
    }
}