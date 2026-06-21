using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public struct GhostNodeData
{
    public Vector3 position;
    public Quaternion rotation;
    public bool gliderActive; // ← ajout
}

public class TimeAttack : GameMode
{
    [Header("Références UI")]
    [SerializeField] private TextMeshProUGUI startUI;
    [SerializeField] private TextMeshProUGUI currentTimerUI;
    [SerializeField] private TextMeshProUGUI bestScoreUI;
    [SerializeField] private GameObject bestScoreImage;

    [Header("Configuration Fantôme")]
    [SerializeField] private KartScriptV2 ghostKart;

    [Header("Audio SFX Décompte")]
    [SerializeField] private AudioClip countdownBeepSound; // Le bip pour 3, 2, 1
    [SerializeField] private AudioClip countdownGoSound;   // Le jingle pour GO!

    // Listes pour stocker les positions/rotations à chaque frame
    private List<GhostNodeData> currentLapPositions = new List<GhostNodeData>();
    private List<GhostNodeData> bestLapPositions = new List<GhostNodeData>();

    private bool isRecording = false;
    private int playbackIndex = 0;
    private float bestLapTime = float.MaxValue;

    private float recordTimer = 0f;
    private float recordInterval = 0.05f;
    private float playBackTimer = 0f;   

    [System.Serializable]
    private class GhostWrapper { public List<GhostNodeData> list; }

    private void Start()
    {
        lapManager = FindFirstObjectByType<LapManager>();
        KartScriptV2 found = null;
        foreach (var k in FindObjectsByType<KartScriptV2>(FindObjectsSortMode.None))
        {
            if (k != ghostKart) { found = k; break; }
        }
        Initialize(lapManager, found);

        startUI = GameObject.Find("CountDownUI")?.GetComponent<TextMeshProUGUI>();
        currentTimerUI = GameObject.Find("ChronoUI")?.GetComponent<TextMeshProUGUI>();
        bestScoreUI = GameObject.Find("BestScoreUI")?.GetComponent<TextMeshProUGUI>();
        bestScoreImage = GameObject.Find("BestScoreImage");
        bestScoreImage?.SetActive(true);

        maxLaps = 99999; // Mode infini
        LoadBestScore();
        LoadGhostData(); // Charge et spawn le ghost du meilleur tour précédent

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
            recordTimer += Time.deltaTime;
            if (recordTimer >= recordInterval)
            {
                recordTimer = 0f;
                currentLapPositions.Add(new GhostNodeData
                {
                    position = kartScript.transform.position,
                    rotation = kartScript.transform.rotation,
                    gliderActive = kartScript.Glider.activate
                });
            }
        }

        // 2. LECTURE DU FANTÔME (S'il a été généré au tour précédent)
        if (ghostKart != null && bestLapPositions.Count > 0)
        {
            playBackTimer += Time.deltaTime;
            if (playBackTimer >= recordInterval)
            {
                playBackTimer = 0f;
                if (playbackIndex < bestLapPositions.Count)
                {
                    ghostKart.transform.position = bestLapPositions[playbackIndex].position;
                    ghostKart.transform.rotation = bestLapPositions[playbackIndex].rotation;
                    ghostKart.Glider.activate = bestLapPositions[playbackIndex].gliderActive;
                    playbackIndex++;
                }
                else
                {
                    ghostKart.gameObject.SetActive(false);
                }
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
        if (lapTime < bestLapTime)
        {
            bestLapTime = lapTime;

            if (bestScoreUI != null)
                bestScoreUI.text = "" + FormatTime(bestLapTime);

            bestLapPositions = new List<GhostNodeData>(currentLapPositions);
            SaveGhostData(); // Sauvegarde le ghost du nouveau record
        }

        // Réinitialisation pour le tour suivant
        currentLapPositions.Clear();
        playbackIndex = 0;

        // Spawn ou respawn le ghost au début de la ligne
        SpawnGhost();
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

    private void SaveGhostData()
    {
        string key = "TA_GhostData" + GetSuffix();
        GhostWrapper wrapper = new GhostWrapper { list = bestLapPositions };
        PlayerPrefs.SetString(key, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private void LoadGhostData()
    {
        string key = "TA_GhostData" + GetSuffix();
        if (!PlayerPrefs.HasKey(key)) return;

        string json = PlayerPrefs.GetString(key);
        if (string.IsNullOrEmpty(json)) return;

        GhostWrapper wrapper = JsonUtility.FromJson<GhostWrapper>(json);
        if (wrapper?.list != null && wrapper.list.Count > 0)
        {
            bestLapPositions = wrapper.list;
            SpawnGhost();
        }
    }

    private void SpawnGhost()
    {
        if (bestLapPositions.Count == 0 || ghostKart == null) return;

        ghostKart.gameObject.SetActive(false);
        ghostKart.enabled = false;

        // Désactive la caméra du ghost pour qu'elle ne prenne pas le contrôle
        Camera ghostCam = ghostKart.GetComponentInChildren<Camera>(true);
        if (ghostCam != null) ghostCam.gameObject.SetActive(false);

        // Désactive aussi le playerCamera référencé dans KartScriptV2
        if (ghostKart.playerCamera != null) ghostKart.playerCamera.SetActive(false);

        ghostKart.transform.position = bestLapPositions[0].position;
        ghostKart.transform.rotation = bestLapPositions[0].rotation;
        ghostKart.IsGhost = true;
        ghostKart.gameObject.SetActive(true);
        playbackIndex = 0;
    }

    private string GetSuffix()
    {
        string dir = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted)
            ? "_Inverted" : "_Normal";
        return ActiveProfileBridge.Key(dir);
    }

    private LeaderboardCircuitMode GetCurrentCircuitMode()
    {
        return InversionCatcher.instance != null && InversionCatcher.instance.Inverted
            ? LeaderboardCircuitMode.Reverse
            : LeaderboardCircuitMode.Normal;
    }
}