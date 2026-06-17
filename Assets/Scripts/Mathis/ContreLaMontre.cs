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
        // Récupération de l'AudioSource principal par défaut
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Nettoyage sécurité : on laisse Initialize et le GameManager gérer le réveil,
        // mais si besoin de chercher l'UI dynamiquement, décommente les lignes ci-dessous :
        // startUI = GameObject.Find("CountDownUI")?.GetComponent<TextMeshProUGUI>();
        // scoreUI = GameObject.Find("ScoreUI")?.GetComponent<TextMeshProUGUI>();
    }

    public override void Initialize(LapManager lm, KartScriptV2 ks)
    {
        base.Initialize(lm, ks);
        this.MaxLaps = 3; // Mode Contre-la-montre classique en 3 tours

        if (lm != null)
        {
            this.currentTimerUI = lm.ChronoUI;
        }

        startUI = GameObject.Find("CountDownUI")?.GetComponent<TextMeshProUGUI>();
        scoreUI = GameObject.Find("ScoreUI")?.GetComponent<TextMeshProUGUI>();

        if (kartScript != null)
            kartScript.CanDrive = false;

        StartCoroutine(StartCountdown());
    }

    private void Update()
    {
        if (!raceStarted)
        {
            // Détection du timing pour le départ Turbo (Appuyer pendant le chiffre "2")
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
        // Enregistrement et traitement du score
        int lapTimeInMs = Mathf.RoundToInt(lapTime * 1000f);
        bool isReverse = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted);

        bool scoreAccepted = isReverse
            ? LeaderboardService.EnsureInstance().SubmitTimeTrialReverse(lapTimeInMs)
            : LeaderboardService.EnsureInstance().SubmitTimeTrialNormal(lapTimeInMs);

        if (scoreAccepted && lapTime < bestLapTime)
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
            kartScript.CanDrive = false;

        Debug.Log("Course Contre-la-montre terminée !");
    }

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        float seconds = time % 60;
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00}:{1:00.000}", minutes, seconds);
    }

    public override bool getRaceStarted()
    {
        return raceStarted;
    }

    IEnumerator StartCountdown()
    {
        if (kartScript != null) kartScript.CanDrive = false;

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "3";

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "2";
        boostWindow = true; // Fenêtre ouverte pendant le "2"

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "1";
        boostWindow = false; // Fermée au "1"

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "GO!";

        if (kartScript != null) kartScript.CanDrive = true;

        raceStarted = true;

        // --- APPLICATION DE LA FORCE TURBO DIRECTE ET DU SON ---
        if (playerPressed && kartScript != null)
        {
            // On applique le boost au kart
            kartScript.StartTurbo(10f, 2f);

            // Sécurité Audio : Si l'AudioSource principal tourne en boucle (bruit moteur),
            // on cherche ou on crée un canal secondaire pour ne pas étouffer le SFX
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

            // Lecture du jingle de boost de départ
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