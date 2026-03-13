using UnityEngine;
using TMPro;

public class ChronoScript : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI chronoUI;

    private float delta = 0f;

    private string _currentTime;

    private bool isRunning = true;
    public string CurrentTime
    {
        get { return _currentTime; }
        private set { _currentTime = value; }
    }

    void Update()
    {
        if (!isRunning)
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

        CurrentTime = $"{minutes:00} : {seconds:00.000}";

        chronoUI.text = CurrentTime;
    }

    public void ResetChrono()
    {
        delta = 0f;
    }
}