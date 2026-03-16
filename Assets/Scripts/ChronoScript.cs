using UnityEngine;
using TMPro;

public class ChronoScript : MonoBehaviour
{
    [SerializeField] private ContreLaMontre contreLaMontre;
    [SerializeField] private LapManager lapManager;


    public float delta = 0f;

    public float _currentTime;

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
        if (lapManager.IsChecking)
        {
            CurrentTime = delta;
            lapManager.IsChecking = false;
        }

    }



    public void ResetChrono()
    {
        delta = 0f;
    }
}