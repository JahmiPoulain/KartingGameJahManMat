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
        // Au lieu de FindFirstObjectByType, on récupère le mode validé et configuré par le GameManager
        if (gameMode == null)
        {


            // Secours si tu utilises l'ancien système :
            gameMode = FindFirstObjectByType<ContreLaMontre>();
            if (gameMode == null || !gameMode.enabled)
                gameMode = FindFirstObjectByType<TimeAttack>();

            if (gameMode == null) return;
        }

        if (gameMode.RaceFinished || !gameMode.getRaceStarted())
        {
            // Ces logs te diront si tu es sur le bon script
            // Si getRaceStarted() affiche False alors que le décompte est fini, 
            // c'est que le chrono pointe sur un deuxième script ContreLaMontre fantôme dans la scène !
            Debug.Log("Chrono bloqué - RaceFinished: " + gameMode.RaceFinished + " | RaceStarted: " + gameMode.getRaceStarted() + " sur l'objet: " + gameMode.gameObject.name);
            return;
        }

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

 
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00}:{1:00.000}", minutes, seconds);
    }
}