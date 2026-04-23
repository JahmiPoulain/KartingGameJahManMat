using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LapManager : MonoBehaviour
{
    [SerializeField] private ChronoScript chrono;
    [SerializeField] private CheckpointManager checkpointManager;

    [SerializeField] private TextMeshProUGUI chronoUI;
    [SerializeField] private TextMeshProUGUI lapUI;

    private GameMode currentMode;
    private int _currentLap = 1;
    private List<float> _lapTimes = new List<float>();
    private bool _isChecking = false;

    public List<float> LapTimes => _lapTimes;
    public int CurrentLap => _currentLap;
    public bool IsChecking { get => _isChecking; set => _isChecking = value; }
    public TextMeshProUGUI ChronoUI { get => chronoUI; }

    void Start()
    {
        StartCoroutine(WaitForMode());
    }

    IEnumerator WaitForMode()
    {
        while (currentMode == null)
        {
            currentMode = FindFirstObjectByType<GameMode>();
            yield return null;
        }
        UpdateLapUI();
    }

    // Supprime le Start et la Coroutine WaitForMode
    // Ajoute cette méthode à la place :
    public void SetGameMode(GameMode mode)
    {
        currentMode = mode;
        UpdateLapUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_isChecking)
        {
            if (checkpointManager.NextIndex > checkpointManager.TotalCheckpointCount)
            {
                CompleteLap();
            }
        }
    }

    private void CompleteLap()
    {
        float finalLapTime = chrono.CurrentTime; // CAPTURE ICI
        _lapTimes.Add(finalLapTime);

        currentMode.OnLapCompleted(finalLapTime);
        chrono.ResetChrono();
        checkpointManager.ResetCheckpoints();

        StartCoroutine(LapCompletionAnimation(finalLapTime));
        _currentLap++;
        UpdateLapUI();

        checkpointManager.HasCheckpoint = false;
    }

    private void UpdateLapUI()
    {
        if (currentMode is TimeAttack) lapUI.text = $"Essai {_currentLap}";
        else lapUI.text = $"Tour {_currentLap}/{currentMode.MaxLaps}";
        if (currentMode.RaceFinished) lapUI.text = $"Tour {currentMode.MaxLaps}/{currentMode.MaxLaps}";
    }

    private IEnumerator LapCompletionAnimation(float timeToShow)
    {
        _isChecking = true;
        string formatted = $"{((int)timeToShow / 60):00}:{(timeToShow % 60):00.000}";

        for (int i = 0; i < 3; i++)
        {
            ChronoUI.text = formatted;
            ChronoUI.color = Color.yellow;
            yield return new WaitForSeconds(0.25f);
            ChronoUI.text = "";
            yield return new WaitForSeconds(0.2f);
        }

        ChronoUI.color = Color.white;
        _isChecking = false;
    }
}