using UnityEngine;
using TMPro;
using System.Collections;

public class LapManager : MonoBehaviour
{
    [SerializeField] private ChronoScript chrono;
    [SerializeField] private Checkpoint[] checkpoints;
    [SerializeField] CheckpointManager checkpointManager;
    [SerializeField] GameMode currentMode;


    [SerializeField]
    private TextMeshProUGUI chronoUI;
    [SerializeField]
    private TextMeshProUGUI lapUI;

    private int _currentLap = 1;
    private float[] _lapTimes = new float[3];
    private float lapTime = 0f;
    private bool _isChecking = false;

    public int CurrentLap { get => _currentLap; private set => _currentLap = value; }
    public float[] LapTimes { get => _lapTimes; private set => _lapTimes = value; }
    public bool IsChecking { get => _isChecking; set => _isChecking = value; }
    public Checkpoint[] Checkpoints { get => checkpoints; set => checkpoints = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _lapTimes = new float[currentMode.MaxLaps];
        lapUI.text = $"Tour {_currentLap}/{currentMode.MaxLaps}";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !IsChecking)
        {
            CompleteLap();
        }
    }

    private void CompleteLap()
    {
        if (checkpointManager.NextIndex > Checkpoints.Length)
        {
            FinishLapLogic();
            StartCoroutine(LapCompletionAnimation());

            foreach (Checkpoint checkpoint in Checkpoints)
            {
                checkpoint.gameObject.SetActive(true);
            }

            checkpointManager.HasCheckpoint = false;
        }

    }

    private void FinishLapLogic()
    {
        lapTime = chrono.CurrentTime;
        LapTimes[CurrentLap - 1] = lapTime;

        chrono.ResetChrono();

        checkpointManager.NextIndex = 1;

        CurrentLap++;

        lapUI.text = $"Tour {CurrentLap}/{currentMode.MaxLaps}";

        if (CurrentLap >= currentMode.MaxLaps)
        {
            lapUI.text = $"Tour {currentMode.MaxLaps}/{currentMode.MaxLaps}";
        }

        if (CurrentLap > LapTimes.Length)
        {
            Debug.Log("Course terminée !");
        }
    }

    private IEnumerator LapCompletionAnimation()
    {
        IsChecking = true;

        for (int i = 0; i < 3; i++)
        {
            chronoUI.text =lapTime.ToString("F3");
            yield return new WaitForSeconds(0.3f);

            chronoUI.text = "";
            yield return new WaitForSeconds(0.3f);
        }

        chronoUI.text = "";

        IsChecking = false;
    }
}
