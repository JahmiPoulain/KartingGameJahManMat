using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct GhostFrameData
{
    public float splineProgress;
    public bool isDrifting;
}

public class ContreLaMontre : GameMode
{
    [SerializeField] private TextMeshProUGUI currentTimerUI;
    private TextMeshProUGUI scoreUI;
    private TextMeshProUGUI startUI;

    bool boostWindow = false;
    bool playerPressed = false;

    [Header("Ghost Configuration")]
    [SerializeField] private KartScriptV2 ghostKartPrefab;
    private KartScriptV2 spawnedGhostKart;

    private List<GhostFrameData> currentLapGhostData = new List<GhostFrameData>();
    private List<GhostFrameData> bestLapGhostData = new List<GhostFrameData>();

    private bool isRecordingGhost = false;
    private float recordTimer = 0f;
    private float recordInterval = 0.1f;

    private int ghostPlaybackIndex = 0;
    private float playbackTimer = 0f;

    private float bestLapTime = float.MaxValue;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip turboStartSound;



    public override void Initialize(LapManager lm, KartScriptV2 ks)
    {
        base.Initialize(lm, ks);
        this.MaxLaps = 3;
        //this.currentTimerUI = lm.ChronoUI;
        if (kartScript != null) kartScript.CanDrive = false;



        startUI = GameObject.Find("CountDownUI")?.GetComponent<TextMeshProUGUI>();
        scoreUI = GameObject.Find("ScoreUI")?.GetComponent<TextMeshProUGUI>();

        LoadBestLapAndGhostData();

        // Le Ghost spawn dès le début de la course s'il y a des données enregistrées
        if (bestLapGhostData.Count > 0 && ghostKartPrefab != null)
        {
            spawnedGhostKart = Instantiate(ghostKartPrefab, kartScript.transform.position, kartScript.transform.rotation);
            spawnedGhostKart.GhostMode = true;
            spawnedGhostKart.CanDrive = false; // Bloqué pendant le décompte
        }

        StartCoroutine(StartCountdown());
    }

    void Update()
    {
        // --- DETECTION INPUT DEPART TURBO ---
        if (boostWindow && !raceStarted)
        {

            if (InputSystemHandler.instance.inputForwardDir >= 1 || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                playerPressed = true;
                Debug.Log("Turbo timing start validated!");

            }

        }

        if (raceStarted && !raceFinished)
        {
            // Enregistrement des données du joueur
            if (isRecordingGhost)
            {
                recordTimer += Time.deltaTime;
                if (recordTimer >= recordInterval)
                {
                    recordTimer = 0f;
                    GhostFrameData frame;
                    frame.splineProgress = kartScript.SplineProgress;
                    frame.isDrifting = kartScript.accelerate && (Mathf.Abs(kartScript.turnDirection) > 0.5f);
                    currentLapGhostData.Add(frame);
                }
            }

            // Lecture et déplacement forcé du Ghost (évite les conflits physiques)
            if (spawnedGhostKart != null && bestLapGhostData.Count > 0)
            {
                playbackTimer += Time.deltaTime;
                if (playbackTimer >= recordInterval)
                {
                    playbackTimer = 0f;
                    ghostPlaybackIndex++;
                    if (ghostPlaybackIndex < bestLapGhostData.Count)
                    {
                        spawnedGhostKart.SplineProgress = bestLapGhostData[ghostPlaybackIndex].splineProgress;

                        // Sécurité anti-gravité / physique résiduelle sur le Ghost
                        Rigidbody ghostRb = spawnedGhostKart.GetComponent<Rigidbody>();
                        if (ghostRb != null)
                        {
                            ghostRb.linearVelocity = Vector3.zero;
                            ghostRb.angularVelocity = Vector3.zero;
                        }
                    }
                    else
                    {
                        ghostPlaybackIndex = 0; // Boucle si le joueur est plus lent que son ghost
                    }
                }
            }
        }
    }

    public override void OnLapCompleted(float lapTime)
    {
        if (lapTime < bestLapTime)
        {
            bestLapTime = lapTime;
            bestLapGhostData = new List<GhostFrameData>(currentLapGhostData);
            SaveBestLapAndGhostData();
        }

        currentLapGhostData.Clear();
        recordTimer = 0f;
        ghostPlaybackIndex = 0;
        playbackTimer = 0f;

        base.OnLapCompleted(lapTime);
    }

    public override void CompleteRace()
    {
        if (raceFinished) return;
        raceFinished = true;

        isRecordingGhost = false;
        if (spawnedGhostKart != null) Destroy(spawnedGhostKart.gameObject);

        if (kartScript != null)
        {
            //kartScript.CanDrive = false;
            kartScript.GhostMode = true;
        }

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

    private void SaveBestLapAndGhostData()
    {
        string suffix = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted) ? "_Inverted" : "_Normal";
        PlayerPrefs.SetFloat("CLM_BestLapTime" + suffix, bestLapTime);

        Wrapper wrapper = new Wrapper { list = bestLapGhostData };
        string jsonGhost = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString("CLM_GhostData_JSON" + suffix, jsonGhost);
        PlayerPrefs.Save();
    }

    private void LoadBestLapAndGhostData()
    {
        string suffix = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted) ? "_Inverted" : "_Normal";
        if (PlayerPrefs.HasKey("CLM_BestLapTime" + suffix))
        {
            bestLapTime = PlayerPrefs.GetFloat("CLM_BestLapTime" + suffix);
            string jsonGhost = PlayerPrefs.GetString("CLM_GhostData_JSON" + suffix);
            if (!string.IsNullOrEmpty(jsonGhost))
            {
                Wrapper wrapper = JsonUtility.FromJson<Wrapper>(jsonGhost);
                bestLapGhostData = wrapper.list;
            }
        }
    }

    private string FormatTime(float time)
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00}:{1:00.000}", (int)(time / 60), time % 60);
    }

    [System.Serializable]
    private class Wrapper { public List<GhostFrameData> list; }

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
        if (spawnedGhostKart != null) spawnedGhostKart.CanDrive = true;

        raceStarted = true;
        isRecordingGhost = true;

        // --- APPLICATION DE LA FORCE TURBO DIRECTE ---
        if (playerPressed && kartScript != null)
        {
            // On injecte directement une forte valeur dans la jauge de poussée du kart
            kartScript.StartTurbo(10f,2f);
            if (audioSource != null && turboStartSound != null)
            {
                audioSource.PlayOneShot(turboStartSound);
            }
            Debug.Log("Turbo Boost triggered at GO!");
        }

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "";
    }
}