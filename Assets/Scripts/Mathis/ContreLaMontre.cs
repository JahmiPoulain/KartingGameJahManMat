using UnityEngine;
using TMPro;
using System.Collections;

public class ContreLaMontre : GameMode
{
    [SerializeField] private TextMeshProUGUI currentTimerUI;
    private TextMeshProUGUI scoreUI;
    private TextMeshProUGUI startUI;

    private bool boostWindow = false;
    private bool playerPressed = false;
    private float bestLapTime = float.MaxValue;

    [Header("Audio SFX Boost Départ")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip turboStartSound;
    [Header("Audio SFX Décompte")]
    [SerializeField] private AudioClip countdownBeepSound; // Le bip pour 3, 2, 1
    [SerializeField] private AudioClip countdownGoSound;   // Le jingle pour GO!

    private void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Recherche automatique de secours de l'UI si non assignée
        if (startUI == null) startUI = GameObject.Find("CountDownUI")?.GetComponent<TextMeshProUGUI>();
        if (scoreUI == null) scoreUI = GameObject.Find("ScoreUI")?.GetComponent<TextMeshProUGUI>();
    }

    public override void Initialize(LapManager lm, KartScriptV2 ks)
    {
        base.Initialize(lm, ks);
        this.MaxLaps = 3; // Le Contre-la-montre officiel se joue sur 3 tours

        if (lm != null)
        {
            this.currentTimerUI = lm.ChronoUI;
        }

        if (startUI == null) startUI = GameObject.Find("CountDownUI")?.GetComponent<TextMeshProUGUI>();
        if (scoreUI == null) scoreUI = GameObject.Find("ScoreUI")?.GetComponent<TextMeshProUGUI>();

        if (kartScript != null)
            kartScript.CanDrive = false;

        StartCoroutine(StartCountdown());
    }

    private void Update()
    {
        if (!raceStarted)
        {
            // Détection du timing pour le départ Turbo (Pendant l'affichage du chiffre "2")
            if (boostWindow && InputSystemHandler.instance.inputForwardDir >= 1|| Input.GetKeyDown(KeyCode.Z)|| Input.GetKeyDown(KeyCode.UpArrow))
            {
                playerPressed = true;
                Debug.Log("Timing Turbo Validé !");
            }
            return;
        }
    }

    public override void OnLapCompleted(float lapTime)
    {
        // On conserve la détection du record du tour (Personal Best) pour l'affichage de l'UI en course
        if (lapTime < bestLapTime)
        {
            bestLapTime = lapTime;

        }
        base.OnLapCompleted(lapTime);
    }

    public override void CompleteRace()
    {
        raceFinished = true;

        if (kartScript != null)
        {
            
            kartScript.GhostMode = true; // ← à rajouter
        }


        Debug.Log("Course Contre-la-montre terminée !");

        float totalTime = 0;
        string detailScores = "Race Results:\n";

        for (int i = 0; i < lapManager.LapTimes.Count; i++)
        {
            float t = lapManager.LapTimes[i];
            totalTime += t;
            detailScores += $"{i + 1}: {FormatTime(t)}\n";
        }

        if (scoreUI != null)
            scoreUI.text = detailScores + $"Total Time: {FormatTime(totalTime)}";

        int totalTimeInMs = Mathf.RoundToInt(totalTime * 1000f);
        bool isReverse = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted);

        if (isReverse)
            LeaderboardService.EnsureInstance().SubmitContreLaMontreReverse(totalTimeInMs);
        else
            LeaderboardService.EnsureInstance().SubmitContreLaMontreNormal(totalTimeInMs);
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

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "3";
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx2D(countdownBeepSound);

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "2";
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx2D(countdownBeepSound);
        boostWindow = true;

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "1";
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx2D(countdownBeepSound);
        boostWindow = false;

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "GO!";
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx2D(countdownGoSound);

        if (kartScript != null) kartScript.CanDrive = true;

        raceStarted = true;

        // --- EXÉCUTION DU BOOST DE DÉPART PROPRE ---
        if (playerPressed && kartScript != null)
        {
            kartScript.StartTurbo(10f, 2f);

            // Gestion de l'AudioSource pour le bruit de soufflerie de turbo (sans couper le moteur)
            if (audioSource == null || audioSource.loop)
            {
                AudioSource[] allSources = GetComponents<AudioSource>();
                foreach (AudioSource src in allSources)
                {
                    if (!src.loop) { audioSource = src; break; }
                }
            }

            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            if (audioSource != null && turboStartSound != null)
            {
                audioSource.loop = false;
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(turboStartSound);
            }
        }

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "";
    }
}