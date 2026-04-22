using UnityEngine;
using TMPro;
using System.Collections;

public class ContreLaMontre : MonoBehaviour
{

    [SerializeField] LapManager lapManager;
    [SerializeField] private KartScriptV2 kartScript;

    [SerializeField] private TextMeshProUGUI scoreUI;
    [SerializeField] private TextMeshProUGUI startUI;

    [SerializeField] private GameObject leaderboardCanvas;

    bool boostWindow = false;
    bool playerPressed = false;

    public GameObject endCourseCanva;


    [SerializeField] private int maxLaps = 3;

    private bool raceStarted = false;
    private bool raceFinished = false;
    private float raceStartTime;

    public bool getRaceStarted()
    {

        return raceStarted;
    }

    public bool RaceFinished { get => raceFinished; private set => raceFinished = value; }
    public int MaxLaps { get => maxLaps; private set => maxLaps = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (leaderboardCanvas != null)
            leaderboardCanvas.SetActive(false);

        StartCoroutine(StartCountdown());
    }

    // Update is called once per frame

    void Update()
    {
        if (boostWindow && InputSystemHandler.instance.inputForward > 0)
        {
            playerPressed = true;
        }
        CheckRaceCompletion();
    }

    private void CheckRaceCompletion()
    {
        if (!raceFinished && lapManager.CurrentLap -1 >= MaxLaps)
        {
            kartScript.canDrive = false;
            CompleteRace();
        }
    }

    private void CompleteRace()
    {

        raceFinished = true;
        int finalTime = Mathf.RoundToInt((Time.time - raceStartTime) * 1000f);

        scoreUI.text = $"Temps total : \n {FormatTime(finalTime)}";
        kartScript.ghostMode = true;

        OnRaceFinished(finalTime);

        if (leaderboardCanvas != null)
            leaderboardCanvas.SetActive(true);

        endCourseCanva.SetActive(true);
    }

    void OnRaceFinished(int finalTime)
    {
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.SubmitScoreAndRefresh(finalTime);
        }
    }

    private string FormatTime(int milliseconds)
    {
        int min = milliseconds / 60000;
        int sec = milliseconds / 1000 % 60;
        int ms = milliseconds % 1000;

        return string.Format("{0:00}:{1:00}.{2:000}", min, sec, ms);
    }

    IEnumerator StartCountdown()
    {
        kartScript.canDrive = false;

        startUI.text = "3";
        yield return new WaitForSeconds(1);

        startUI.text = "2";
        boostWindow = true;
        yield return new WaitForSeconds(1);

        startUI.text = "1";
        yield return new WaitForSeconds(1);

        startUI.text = "GO!";

        kartScript.canDrive = true;
        raceStartTime = Time.time;

        if (boostWindow && playerPressed)
        {
            kartScript.StartTurbo(8f, 1.2f);
        }

        yield return new WaitForSeconds(1);

        startUI.text = "";
        raceStarted = true;
    }
}
