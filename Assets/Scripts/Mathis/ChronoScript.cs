using TMPro;
using UnityEngine;

public class ChronoScript : MonoBehaviour
{
    private GameMode gameMode; // Changé de ContreLaMontre à GameMode
    [SerializeField] private TextMeshProUGUI chronoUI;
    private float delta = 0f;
    public float CurrentTime => delta;

    void Start()
    {
        // On récupère le mode de jeu présent sur le Kart au démarrage
        gameMode = FindFirstObjectByType<GameMode>();
    }

    void Update()
    {
        if (gameMode == null || gameMode.RaceFinished || !gameMode.getRaceStarted())
            return;

        delta += Time.deltaTime;
        chronoUI.text = FormatTime(delta);
    }

    public void ResetChrono() => delta = 0f;

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        float seconds = time % 60;
        return $"{minutes:00}:{seconds:00.000}";
    }
}