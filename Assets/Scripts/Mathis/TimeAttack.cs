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
    private const string BEST_TIME_KEY = "BestTimeAttackScore";

    private void Start()
    {
        lapManager = FindFirstObjectByType<LapManager>();
        Initialize(lapManager, FindFirstObjectByType<KartScriptV2>());
        // Pour l'UI, cherchez par tag ou via un script UI centralisé
        startUI = GameObject.Find("CountDownUI").GetComponent<TextMeshProUGUI>();
        currentTimerUI = GameObject.Find("ChronoUI").GetComponent<TextMeshProUGUI>();
        bestScoreUI = GameObject.Find("BestScoreUI").GetComponent<TextMeshProUGUI>();

        maxLaps = 999;
        LoadBestScore();
        StartCoroutine(InitialCountdown());
    }

    public override void Initialize(LapManager lm, KartScriptV2 ks)
    {
        base.Initialize(lm, ks);
        // Récupérez les UI depuis le LapManager qui les possède déjà
        this.currentTimerUI = lm.ChronoUI;
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
        //currentTimerUI.text = FormatTime(lapManager.CurrentLapTime);
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


    public override void OnLapCompleted(float lapTime)
    {
        if (lapTime < bestLapTime)
        {
            bestLapTime = lapTime;
            PlayerPrefs.SetFloat(BEST_TIME_KEY, bestLapTime);
            PlayerPrefs.Save(); // Optionnel mais sûr

            if (bestScoreUI != null)
                bestScoreUI.text = "NOUVEAU RECORD : " + FormatTime(bestLapTime);
        }


    }

    // Appelé par LapManager à chaque franchissement de ligne 
    public override void CompleteRace()
    {
        /*// Récupère le temps du tour venant d'être complété
        float lastLapTime = lapManager.LapTimes[lapManager.CurrentLap - 2];

        // Vérifie si c'est un nouveau record 
        if (lastLapTime < bestLapTime)
        {
            bestLapTime = lastLapTime;
            PlayerPrefs.SetFloat(BEST_TIME_KEY, bestLapTime);
            PlayerPrefs.Save();
            bestScoreUI.text = "NOUVEAU RECORD : " + FormatTime(bestLapTime);
        }*/
        Debug.Log("Fin TimeAttack");


    }
}