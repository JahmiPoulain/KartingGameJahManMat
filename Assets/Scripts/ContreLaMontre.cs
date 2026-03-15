using System;
using UnityEngine;
using TMPro;

public class ContreLaMontre : MonoBehaviour
{

    [SerializeField] LapManager lapManager;

    [SerializeField]
    private TextMeshProUGUI scoreUI;

    private int maxLaps = 3;
    private bool raceFinished = false;

    public bool RaceFinished { get => raceFinished; private set => raceFinished = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        CheckRaceCompletion();

    }

    private void CheckRaceCompletion()
    {
        if (!raceFinished && lapManager.CurrentLap >= maxLaps)
        {
            CompleteRace();
        }
    }

    private void CompleteRace()
    {
        raceFinished = true;
        float totalTime = 0;
        for (int i = 0; i < maxLaps; i++)
        {
            totalTime += lapManager.LapTimes[i];
        }
        int minutes = (int)(totalTime / 60);
        float seconds = totalTime % 60;
        scoreUI.text = $"{minutes : 00}:{seconds:00.000}";
    }
}
