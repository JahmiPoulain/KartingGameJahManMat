using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
    private float lapTime = 0f;
    private bool _isChecking = false;

    // Remplacez : private float[] _lapTimes = new float[3];
    private List<float> _lapTimes = new List<float>();

    // Modifiez la propriété :
    public List<float> LapTimes { get => _lapTimes; }
    public int CurrentLap { get => _currentLap; private set => _currentLap = value; }
    public bool IsChecking { get => _isChecking; set => _isChecking = value; }
    public Checkpoint[] Checkpoints { get => checkpoints; set => checkpoints = value; }
    public TextMeshProUGUI ChronoUI { get => chronoUI; set => chronoUI = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _lapTimes.Clear();
        lapUI.text = $"Tour {_currentLap}/{currentMode.MaxLaps}";

        // On attend un peu ou on cherche le mode sur le joueur
        StartCoroutine(WaitForMode());

    }

    IEnumerator WaitForMode()
    {
        while (currentMode == null)
        {
            currentMode = FindFirstObjectByType<GameMode>();
            yield return null;
        }
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
        _lapTimes.Add(lapTime); // On ajoute dynamiquement

        // Gestion dynamique du tableau des scores (évite l'erreur d'index hors limites)
        if (CurrentLap <= LapTimes.Count)
        {
            LapTimes[CurrentLap - 1] = lapTime;
        }

        // ON PRÉVIENT LE MODE DE JEU QU'UN TOUR EST FINI
        // C'est ici que la magie opère : chaque mode réagira différemment
        currentMode.OnLapCompleted(lapTime);

        chrono.ResetChrono();
        checkpointManager.NextIndex = 1;
        _currentLap++;

        // Mise à jour de l'UI selon le mode
        if (currentMode is TimeAttack)
        {
            lapUI.text = $"Tour {_currentLap}";
        }
        else
        {
            lapUI.text = $"Tour {_currentLap}/{currentMode.MaxLaps}";
        }
    }

    private IEnumerator LapCompletionAnimation()
    {
        IsChecking = true;

        for (int i = 0; i < 3; i++)
        {
            ChronoUI.text =lapTime.ToString("F3");
            yield return new WaitForSeconds(0.3f);

            ChronoUI.text = "";
            yield return new WaitForSeconds(0.3f);
        }

        ChronoUI.text = "";

        IsChecking = false;
    }
}
