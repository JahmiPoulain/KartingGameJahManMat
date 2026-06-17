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
            if (boostWindow && (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Accelerate")))
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
            if (scoreUI != null)
            {
                scoreUI.text = "PB: " + FormatTime(bestLapTime);
            }
        }
    }

    public override void CompleteRace()
    {
        raceFinished = true;

        if (kartScript != null)
        {
            kartScript.CanDrive = true;
            kartScript.GhostMode = true; // ← à rajouter
        }


        Debug.Log("Course Contre-la-montre terminée !");

        // Récupération de la logique de l'ancien script pour le calcul du score final des 3 tours
        if (lapManager != null)
        {
            float totalRaceTime = 0f;

            // On additionne le temps de chaque tour stocké dans le LapManager
            for (int i = 0; i < lapManager.LapTimes.Count; i++)
            {
                totalRaceTime += lapManager.LapTimes[i];
                Debug.Log($"Tour {i + 1} : {FormatTime(lapManager.LapTimes[i])}");
            }

            Debug.Log($"[RESULTAT FINAL] Temps total des 3 tours : {FormatTime(totalRaceTime)}");

            // Conversion du temps total en millisecondes pour le Leaderboard
            int totalTimeInMs = Mathf.RoundToInt(totalRaceTime * 1000f);
            bool isReverse = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted);

            // Envoi officiel du temps global de la course (et non pas d'un seul tour) au classement
            bool scoreAccepted = isReverse
                ? LeaderboardService.EnsureInstance().SubmitContreLaMontreReverse(totalTimeInMs)
                : LeaderboardService.EnsureInstance().SubmitContreLaMontreNormal(totalTimeInMs);

            Debug.Log("Score envoyé au Leaderboard. Accepté : " + scoreAccepted);
        }
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

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "2";
        boostWindow = true;

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "1";
        boostWindow = false;

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "GO!";

        if (kartScript != null) kartScript.CanDrive = true;

        raceStarted = true;

        // --- EXÉCUTION DU BOOST DE DÉPART PROPRE ---
        if (playerPressed && kartScript != null)
        {
            kartScript.StartTurbo(10f, 2f);

            // Gestion de l'AudioSource pour ne pas écraser le son du moteur
            if (audioSource == null || audioSource.loop)
            {
                AudioSource[] allSources = GetComponents<AudioSource>();
                foreach (AudioSource src in allSources)
                {
                    if (!src.loop)
                    {
                        audioSource = src;
                        break;
                    }
                }
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

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