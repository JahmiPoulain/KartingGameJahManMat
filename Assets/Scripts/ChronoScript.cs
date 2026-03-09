using UnityEngine;
using UnityEngine.UIElements;
using TMPro;

public class ChronoScript : MonoBehaviour
{

    [SerializeField]
    private TextMeshProUGUI chronoUI;

    private float delta = 0;
    private float seconds = 0;
    private int minutes = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        delta += Time.deltaTime;

        minutes = (int)(delta / 60);
        seconds = delta % 60;

        if (minutes <= 10 && seconds <= 10)
        {
            chronoUI.text = $"0{minutes} : 0{seconds.ToString("F3")}";
        }
        else if (minutes <= 10)
        {
            chronoUI.text = $"0{minutes} : {seconds.ToString("F3")}";
        }
        else if (seconds <= 10)
        {
            chronoUI.text = $"{minutes} : 0{seconds.ToString("F3")}";
        }
        else
        {
            chronoUI.text = $"{minutes} : {seconds.ToString("F3")}";
        }


    }
}
