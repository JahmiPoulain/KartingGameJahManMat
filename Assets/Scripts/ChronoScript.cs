using TMPro;
using UnityEngine;

public class ChronoScript : MonoBehaviour
{
    [SerializeField] private ContreLaMontre contreLaMontre;
    [SerializeField] private TextMeshProUGUI chronoUI;

    private float delta = 0f;

    public float CurrentTime => delta;

    private void Start()
    {
        //if (chronoUI == null) chronoUI = FindFirstObjectByType<>
    }
    void Update()
    {
        
        if (contreLaMontre.RaceFinished || !contreLaMontre.getRaceStarted())
            return;

        delta += Time.deltaTime;
        int minutes = (int)(delta / 60);
        float seconds = delta % 60;
        chronoUI.text = $"{minutes: 00}:{seconds:00.000}";
    }

    public void ResetChrono()
    {
        delta = 0f;
    }
}