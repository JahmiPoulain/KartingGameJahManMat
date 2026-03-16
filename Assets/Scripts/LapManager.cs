using UnityEngine;
using TMPro;
using System.Collections;

public class LapManager : MonoBehaviour
{
    [SerializeField] private ChronoScript chrono;
    [SerializeField] private Checkpoint[] checkpoints;
    [SerializeField] CheckpointManager checkpointManager;

    [SerializeField]
    private TextMeshProUGUI chronoUI;

    public int _currentLap = 1;
    public float[] _lapTimes = new float[3];
    private bool _isChecking = false;

    public int CurrentLap { get => _currentLap; private set => _currentLap = value; }
    public float[] LapTimes { get => _lapTimes; private set => _lapTimes = value; }
    public bool IsChecking { get => _isChecking; set => _isChecking = value; }
    public Checkpoint[] Checkpoints { get => checkpoints; set => checkpoints = value; }

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
        if (checkpointManager.NextIndex >= Checkpoints.Length+1)
        {

            StartCoroutine(LapCompletion());


        }
        else
        {
            return;
        }
    }

    private IEnumerator LapCompletion()
    {
        IsChecking = true;
        yield return new WaitForSeconds(0.1f);
        LapTimes[CurrentLap - 1] = chrono.CurrentTime;
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.1f);
            chronoUI.text = chrono.CurrentTime.ToString("F3");
            yield return new WaitForSeconds(0.3f);
            chronoUI.text = "";
            yield return new WaitForSeconds(0.3f);
        }
        yield return new WaitForSeconds(0.1f);
        chrono.ResetChrono();
        chronoUI.text = "";
        checkpointManager.NextIndex = 1;
        CurrentLap++;
    }
}
