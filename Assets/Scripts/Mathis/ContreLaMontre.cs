using System;
using UnityEngine;
using TMPro;
using System.Collections;

public class ContreLaMontre : GameMode
{

    [SerializeField] private TextMeshProUGUI scoreUI;
    [SerializeField] private TextMeshProUGUI startUI;


    bool boostWindow = false;
    bool playerPressed = false;
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        maxLaps = 3;
        StartCoroutine(StartCountdown());
    }

    // Update is called once per frame

    void Update()
    {
        if (boostWindow && InputSystemHandler.instance.inputForward > 0)
        {
            playerPressed = true;
        }
        CheckRaceCompletion();
    }

    public override void CheckRaceCompletion()
    {
        if (!raceFinished && lapManager.CurrentLap -1 >= MaxLaps)
        {
            kartScript.canDrive = false;
            CompleteRace();
        }
    }

    public override void CompleteRace()
    {

        raceFinished = true;
        float totalTime = 0;
        for (int i = 0; i < MaxLaps; i++)
        {
            totalTime += lapManager.LapTimes[i];
            scoreUI.text += $"Tour{i} : {(int)(totalTime / 60): 00} : {totalTime % 60: 00.000}\n";
        }
        int minutes = (int)(totalTime / 60);
        float seconds = totalTime % 60;
        scoreUI.text += $"Temps total : \n {minutes : 00}:{seconds:00.000}";
        kartScript.ghostMode = true;

        OnRaceFinished(totalTime);
    }

    void OnRaceFinished(float totalTime)
    {
        int finalTime = Mathf.RoundToInt(totalTime * 1000f);

        //LeaderboardManager.Instance.SubmitScoreAndRefresh(finalTime);
    }

    IEnumerator StartCountdown()
    {
        kartScript.canDrive = false;

        startUI.text = "3";
        yield return new WaitForSeconds(1);

        startUI.text = "2";
        boostWindow = true;
        yield return new WaitForSeconds(1);

        startUI.text = "1";
        yield return new WaitForSeconds(1);

        startUI.text = "GO!";

        kartScript.canDrive = true;

        if (boostWindow && playerPressed)
        {
            kartScript.StartTurbo(8f, 1.2f);
        }

        yield return new WaitForSeconds(1);

        startUI.text = "";
        raceStarted = true;
    }
}
