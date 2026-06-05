using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic; // Nécessaire pour les listes de données

// Structure contenant les données enregistrées à chaque instant (Frame)
[System.Serializable]
public struct GhostFrameData
{
    public float splineProgress;
    public bool isDrifting;
}

public class ContreLaMontre : GameMode
{
    private TextMeshProUGUI scoreUI;
    private TextMeshProUGUI startUI;

    bool boostWindow = false;
    bool playerPressed = false;

    [Header("Configuration du Ghost")]
    [SerializeField] private KartScriptV2 ghostKartPrefab; // Glisses-y ton préfab de Kart Fantôme dans l'inspecteur
    private KartScriptV2 spawnedGhostKart;

    // Listes de stockage pour le Ghost
    private List<GhostFrameData> currentLapGhostData = new List<GhostFrameData>();
    private List<GhostFrameData> bestLapGhostData = new List<GhostFrameData>();

    private bool isRecordingGhost = false;
    private float recordTimer = 0f;
    private float recordInterval = 0.1f; // Enregistre un point toutes les 100ms

    // Variables de lecture pour le Ghost actif
    private int ghostPlaybackIndex = 0;
    private float playbackTimer = 0f;

    private float bestLapTime = float.MaxValue;

    public override void Initialize(LapManager lm, KartScriptV2 ks)
    {
        base.Initialize(lm, ks);
        this.MaxLaps = 3;
        if (kartScript != null) kartScript.CanDrive = false;

        // Récupération automatique de l'UI
        startUI = GameObject.Find("CountDownUI")?.GetComponent<TextMeshProUGUI>();
        scoreUI = GameObject.Find("ScoreUI")?.GetComponent<TextMeshProUGUI>();

        // 1. Charger le meilleur score et la trajectoire fantôme précédente s'ils existent
        LoadBestLapAndGhostData();

        // 2. Si des données existent, on fait apparaître le Kart Fantôme sur la ligne
        if (bestLapGhostData.Count > 0 && ghostKartPrefab != null)
        {
            spawnedGhostKart = Instantiate(ghostKartPrefab, kartScript.transform.position, kartScript.transform.rotation);
            spawnedGhostKart.GhostMode = true; // Active son mode de conduite automatique par Spline
            spawnedGhostKart.CanDrive = false; // Bloqué jusqu'au GO!
        }

        StartCoroutine(StartCountdown());
    }

    void Update()
    {
        if (raceStarted && !raceFinished)
        {
            // --- ENREGISTREMENT DU JOUEUR ---
            if (isRecordingGhost)
            {
                recordTimer += Time.deltaTime;
                if (recordTimer >= recordInterval)
                {
                    recordTimer = 0f;

                    GhostFrameData frame;
                    frame.splineProgress = kartScript.SplineProgress;

                    // On vérifie si le joueur est en train de déraper (drift)
                    // (On utilise la variable "currentTurnSpeed" ou une autre selon ton code de drift interne)
                    frame.isDrifting = kartScript.accelerate && (Mathf.Abs(kartScript.turnDirection) > 0.5f);

                    currentLapGhostData.Add(frame);
                }
            }

            // --- LECTURE DU KART FANTÔME (GHOST) ---
            if (spawnedGhostKart != null && bestLapGhostData.Count > 0)
            {
                playbackTimer += Time.deltaTime;
                if (playbackTimer >= recordInterval)
                {
                    playbackTimer = 0f;
                    ghostPlaybackIndex++;

                    // Si le ghost n'a pas fini sa "cassette", on applique la position
                    if (ghostPlaybackIndex < bestLapGhostData.Count)
                    {
                        // On force sa progression sur la spline
                        spawnedGhostKart.SplineProgress = bestLapGhostData[ghostPlaybackIndex].splineProgress;

                        // Facultatif : Transmettre l'état de dérapage pour les effets visuels du Ghost
                        // spawnedGhostKart.SetDriftVisuals(bestLapGhostData[ghostPlaybackIndex].isDrifting);
                    }
                }
            }
        }
    }

    public override void OnLapCompleted(float lapTime)
    {
        // Si le joueur vient de battre son meilleur temps au tour de la SESSION
        if (lapTime < bestLapTime)
        {
            bestLapTime = lapTime;

            // On remplace la cassette du record par celle du tour venant d'être fini
            bestLapGhostData = new List<GhostFrameData>(currentLapGhostData);

            // Sauvegarde définitive dans les PlayerPrefs
            SaveBestLapAndGhostData();
        }

        // Réinitialisation de la liste pour enregistrer le tour suivant
        currentLapGhostData.Clear();
        recordTimer = 0f;

        // On rembobine la lecture du Ghost pour qu'il reparte lui aussi sur un nouveau tour
        ghostPlaybackIndex = 0;
        playbackTimer = 0f;

        base.OnLapCompleted(lapTime);
    }

    public override void CompleteRace()
    {
        if (raceFinished) return;
        raceFinished = true;

        // On stoppe l'enregistrement et on détruit le ghost pour la fin de course
        isRecordingGhost = false;
        if (spawnedGhostKart != null) Destroy(spawnedGhostKart.gameObject);

        kartScript.CanDrive = false;
        kartScript.GhostMode = true;
        float totalTime = 0;
        string detailScores = "Score :\n";

        for (int i = 0; i < lapManager.LapTimes.Count; i++)
        {
            float t = lapManager.LapTimes[i];
            totalTime += t;
            detailScores += $"Lap {i + 1} : {FormatTime(t)}\n";
        }

        if (scoreUI != null)
            scoreUI.text = detailScores + $"TOTAL : {FormatTime(totalTime)}";

        // --- ENVOI AU LEADERBOARD MANAGER ---
        if (LeaderboardManager.Instance != null)
        {
            int totalTimeInMs = Mathf.RoundToInt(totalTime * 1000f);
            LeaderboardManager.Instance.SubmitScoreAndRefresh(totalTimeInMs);
            Debug.Log($"Score CLM envoyé : {totalTimeInMs} ms");
        }
    }

    // --- LOGIQUE DE SAUVEGARDE ET CHARGEMENT COMPATIBLE MAP INVERSEE ---
    private void SaveBestLapAndGhostData()
    {
        // On récupère l'état d'inversion depuis ton MainMenuUIManager pour séparer les fichiers
        string suffix = (MainMenuUIManager.Instance != null && MainMenuUIManager.Instance.isMapInverted) ? "_Inverted" : "_Normal";

        PlayerPrefs.SetFloat("CLM_BestLapTime" + suffix, bestLapTime);

        // On convertit la liste d'objets complexes en texte JSON pour PlayerPrefs
        Wrapper wrapper = new Wrapper { list = bestLapGhostData };
        string jsonGhost = JsonUtility.ToJson(wrapper);

        PlayerPrefs.SetString("CLM_GhostData_JSON" + suffix, jsonGhost);
        PlayerPrefs.Save();
    }

    private void LoadBestLapAndGhostData()
    {
        string suffix = (MainMenuUIManager.Instance != null && MainMenuUIManager.Instance.isMapInverted) ? "_Inverted" : "_Normal";

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

    // Classe conteneur (Wrapper) requise par l'utilitaire JSON d'Unity pour sérialiser les Listes
    [System.Serializable]
    private class Wrapper { public List<GhostFrameData> list; }

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

        // On libère le mouvement autonome du ghost au signal GO!
        if (spawnedGhostKart != null) spawnedGhostKart.CanDrive = true;

        raceStarted = true;
        isRecordingGhost = true; // On démarre l'enregistrement ici

        yield return new WaitForSeconds(1);
        if (startUI != null) startUI.text = "";
    }
}