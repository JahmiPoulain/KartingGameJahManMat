using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public struct GhostNodeData
{
    public Vector3 position;
    public Quaternion rotation;
}

public class TimeAttack : GameMode
{
    [Header("Références UI")]
    [SerializeField] private TextMeshProUGUI startUI;
    [SerializeField] private TextMeshProUGUI currentTimerUI;
    [SerializeField] private TextMeshProUGUI bestScoreUI;
    [SerializeField] private GameObject bestScoreImage;

    [Header("Configuration Fantôme")]
    [SerializeField] private KartScriptV2 ghostKartPrefab;
    private KartScriptV2 spawnedGhostKart;

    [Header("Audio SFX Décompte")]
    [SerializeField] private AudioClip countdownBeepSound; // Le bip pour 3, 2, 1
    [SerializeField] private AudioClip countdownGoSound;   // Le jingle pour GO!

    // Listes pour stocker les positions/rotations à chaque frame
    private List<GhostNodeData> currentLapPositions = new List<GhostNodeData>();
    private List<GhostNodeData> bestLapPositions = new List<GhostNodeData>();

    private bool isRecording = false;
    private int playbackIndex = 0;
    private float bestLapTime = float.MaxValue;

    private void Start()
    {
        lapManager = FindFirstObjectByType<LapManager>();
        Initialize(lapManager, FindFirstObjectByType<KartScriptV2>());

        startUI = GameObject.Find("CountDownUI")?.GetComponent<TextMeshProUGUI>();
        currentTimerUI = GameObject.Find("ChronoUI")?.GetComponent<TextMeshProUGUI>();
        bestScoreUI = GameObject.Find("BestScoreUI")?.GetComponent<TextMeshProUGUI>();
        bestScoreImage = GameObject.Find("BestScoreImage");
        bestScoreImage?.SetActive(true);

        maxLaps = 99999; // Mode infini
        LoadBestScore();

        if (kartScript != null) kartScript.CanDrive = false;
        StartCoroutine(InitialCountdown());
    }

    IEnumerator InitialCountdown()
    {
        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "3";
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx2D(countdownBeepSound);
        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "2";
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx2D(countdownBeepSound);
        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "1";
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx2D(countdownBeepSound);
        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "GO!";
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySfx2D(countdownGoSound);

        if (kartScript != null) kartScript.CanDrive = true;

        raceStarted = true;
        isRecording = true; // On commence à enregistrer dès le premier tour !

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "";
    }

    private void Update()
    {
        if (!raceStarted) return;

        // 1. ENREGISTREMENT DU JOUEUR (À chaque frame)
        if (isRecording && kartScript != null)
        {
            GhostNodeData node = new GhostNodeData
            {
                position = kartScript.transform.position,
                rotation = kartScript.transform.rotation
            };
            currentLapPositions.Add(node);
        }

        // 2. LECTURE DU FANTÔME (S'il a été généré au tour précédent)
        if (spawnedGhostKart != null && bestLapPositions.Count > 0)
        {
            if (playbackIndex < bestLapPositions.Count)
            {
                spawnedGhostKart.transform.position = bestLapPositions[playbackIndex].position;
                spawnedGhostKart.transform.rotation = bestLapPositions[playbackIndex].rotation;
                playbackIndex++;
            }
            else
            {
                // Si le fantôme a fini son enregistrement avant que le joueur passe la ligne,
                // il s'arrête sur place (ou on peut le cacher)
                spawnedGhostKart.gameObject.SetActive(false);
            }
        }
    }

    public override void OnLapCompleted(float lapTime)
    {
        int lapTimeInMs = Mathf.RoundToInt(lapTime * 1000f);
        bool isReverse = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted);

        bool scoreAccepted = isReverse
            ? LeaderboardService.EnsureInstance().SubmitTimeAttackReverse(lapTimeInMs)
            : LeaderboardService.EnsureInstance().SubmitTimeAttackNormal(lapTimeInMs);

        // --- LOGIQUE DU FANTÔME DYNAMIQUE ---
        // Si c'est le meilleur temps absolu de la session en cours
        if (lapTime < bestLapTime)
        {
            bestLapTime = lapTime;

            if (bestScoreUI != null)
                bestScoreUI.text = "" + FormatTime(bestLapTime);

            // On écrase l'ancien record de positions par les positions du tour qu'on vient de faire
            bestLapPositions = new List<GhostNodeData>(currentLapPositions);
        }

        // Réinitialisation pour le tour suivant
        currentLapPositions.Clear();
        playbackIndex = 0;

        // S'il existe un fantôme du meilleur tour, on le fait apparaître (ou réapparaître) sur la ligne de départ
        if (bestLapPositions.Count > 0)
        {
            if (spawnedGhostKart != null)
            {
                Destroy(spawnedGhostKart.gameObject); // On détruit l'ancien modèle
            }

            // On fait apparaître le nouveau fantôme au point de départ du premier nœud enregistré
            spawnedGhostKart = Instantiate(ghostKartPrefab, bestLapPositions[0].position, bestLapPositions[0].rotation);
            spawnedGhostKart.IsGhost = true; // Marqué comme fantôme (intangible + silencieux !)
            spawnedGhostKart.gameObject.SetActive(true);
        }
    }

    public override void CompleteRace() { }

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        float seconds = time % 60;
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00}:{1:00.000}", minutes, seconds);
    }

    private void LoadBestScore()
    {
        LeaderboardCircuitMode circuitMode = GetCurrentCircuitMode();

        if (LeaderboardService.Instance != null &&
            LeaderboardService.Instance.TryGetLocalBestScore(LeaderboardGameMode.TimeAttack, circuitMode, out int bestTimeInMs))
        {
            bestLapTime = bestTimeInMs / 1000f;
            if (bestScoreUI != null)
                bestScoreUI.text = "" + FormatTime(bestLapTime);
        }
        else
        {
            bestLapTime = float.MaxValue;
            if (bestScoreUI != null)
                bestScoreUI.text = "--:--.--";
        }
    }

    private LeaderboardCircuitMode GetCurrentCircuitMode()
    {
        return InversionCatcher.instance != null && InversionCatcher.instance.Inverted
            ? LeaderboardCircuitMode.Reverse
            : LeaderboardCircuitMode.Normal;
    }
}