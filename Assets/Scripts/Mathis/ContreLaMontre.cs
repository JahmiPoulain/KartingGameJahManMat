using UnityEngine;
using TMPro;
using System.Collections;

public class ContreLaMontre : GameMode
{
    private TextMeshProUGUI scoreUI;
    private TextMeshProUGUI startUI;

    bool boostWindow = false;

    public override void Initialize(LapManager lm, KartScriptV2 ks)
    {
        base.Initialize(lm, ks);
        this.MaxLaps = 3;

        // Récupération automatique de l'UI (assure-toi que les noms correspondent dans ta scène)
        startUI = GameObject.Find("CountDownUI")?.GetComponent<TextMeshProUGUI>();
        scoreUI = GameObject.Find("ScoreUI")?.GetComponent<TextMeshProUGUI>();

        StartCoroutine(StartCountdown());
    }

    void Update()
    {
        if (raceStarted && !raceFinished)
        {
            // Logique de boost ou autre
        }
    }

    public override void CompleteRace()
    {
        if (raceFinished) return;
        raceFinished = true;

        kartScript.CanDrive = false;
        kartScript.GhostMode = true;
        float totalTime = 0;
        string detailScores = "Score :\n";

        for (int i = 0; i < lapManager.LapTimes.Count; i++)
        {
            float t = lapManager.LapTimes[i];
            totalTime += t;
            detailScores += $"Round {i + 1} : {FormatTime(t)}\n";
        }

        if (scoreUI != null)
            scoreUI.text = detailScores + $"TOTAL : {FormatTime(totalTime)}";
    }

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        float seconds = time % 60;

        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00}:{1:00.000}", minutes, seconds);
    }

    IEnumerator StartCountdown()
    {
        if (kartScript != null) kartScript.CanDrive = false;

        if (startUI != null) startUI.text = "3";
        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "2";
        boostWindow = true;
        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "1";
        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "GO!";

        raceStarted = true;
        if (kartScript != null) kartScript.CanDrive = true;

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "";
    }
}