using UnityEngine;
using TMPro;
using System.Collections;

public class LapManager : MonoBehaviour
{
    [SerializeField] private ChronoScript chrono;
    [SerializeField] Checkpoint[] checkpoints;
    [SerializeField] CheckpointManager checkpointManager;

    [SerializeField]
    private TextMeshProUGUI chronoUI;

    private int currentLap = 1;
    private float[] lapTimes = new float[3];

    public int CurrentLap { get => currentLap; private set => currentLap = value; }
    public float[] LapTimes { get => lapTimes; private set => lapTimes = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CompleteLap();
        }
    }

    private void CompleteLap()
    {
        if (checkpointManager.NextIndex >= checkpoints.Length)
        {
            LapTimes[CurrentLap-1] = chrono.CurrentTime;
            CurrentLap++;
            StartCoroutine(ShowLapTime());

        }
        else
        {
            return;
        }
    }

    private IEnumerator ShowLapTime()
    {
        for (int i = 0; i < 5; i++)
        {
            chronoUI.text = chrono.CurrentTime.ToString();
            yield return new WaitForSeconds(0.3f);
            chronoUI.text = "";
        }
        chrono.ResetChrono();
        chronoUI.text = "";
    }
}
