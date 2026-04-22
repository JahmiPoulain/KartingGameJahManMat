using TMPro;
using UnityEngine;

public class ChronoScript : MonoBehaviour
{
    private GameMode gameMode;
    [SerializeField] private TextMeshProUGUI chronoUI;
    [SerializeField] private LapManager lapManager;
    private float delta = 0f;
    public float CurrentTime => delta;

    void Update()
    {
        // On cherche le mode de jeu s'il n'est pas encore là
        if (gameMode == null)
        {
            gameMode = FindFirstObjectByType<GameMode>();
            return;
        }

        if (gameMode.RaceFinished || !gameMode.getRaceStarted())
            return;

        // On n'incrémente et n'affiche que si le LapManager n'est pas en train de faire son animation
        if (lapManager != null && !lapManager.IsChecking)
        {
            delta += Time.deltaTime;
            chronoUI.text = FormatTime(delta);
        }
    }

    public void ResetChrono() => delta = 0f;

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        float seconds = time % 60;
        return $"{minutes:00}:{seconds:00.000}";
    }
}