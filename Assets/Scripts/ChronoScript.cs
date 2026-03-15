using UnityEngine;
using TMPro;

public class ChronoScript : MonoBehaviour
{
    [SerializeField] private ContreLaMontre contreLaMontre;
    [SerializeField] private TextMeshProUGUI chronoUI;

    private float delta = 0f;

    private float _currentTime;

    public float CurrentTime
    {
        get { return _currentTime; }
        private set { _currentTime = value; }
    }

    void Update()
    {
        if (contreLaMontre.RaceFinished)
        {
            return;  
        }
        UpdateChrono();
    }

    private void UpdateChrono()
    {
        delta += Time.deltaTime;

        int minutes = (int)(delta / 60);
        float seconds = delta % 60;
        CurrentTime = delta;
        chronoUI.text = $"{minutes:00}:{seconds:00.000}";
    }

    public void ResetChrono()
    {
        delta = 0f;
    }
}