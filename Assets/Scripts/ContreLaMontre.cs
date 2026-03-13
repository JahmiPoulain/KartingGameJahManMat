using System;
using UnityEngine;
using TMPro;

public class ContreLaMontre : MonoBehaviour
{

    [SerializeField] LapManager lapManager;

    [SerializeField]
    private TextMeshProUGUI scoreUI;

    private int maxLaps = 3;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(lapManager.CurrentLap >= maxLaps)
        {
            CompleteRace();
        }
    }

    private void CompleteRace()
    {
        scoreUI.text = "";
    }
}
