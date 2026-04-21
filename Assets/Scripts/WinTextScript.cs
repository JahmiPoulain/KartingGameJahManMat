using UnityEngine;
using TMPro;

public class WinTextScript : TimedUI
{
    float turnTime;
    int minutes;
    int seconds;
    public TMP_Text text;
    
    private void OnEnable()
    {
        timer = time;        
        turnTime = KartScriptV2.instance.lastTurnTime;
        minutes = 0;
        seconds = 0;
        while ( turnTime > 0)
        {
            if (turnTime > 60f)
            {
                minutes++;
                turnTime -= 60f;
            }
            else
            {
                seconds ++;
                turnTime--;
            }
        }
        string newMinutes = "";
        string newSeconds = "";
        if (minutes < 0)
        {
            newMinutes = "00";
        }
        else if (minutes < 10)
        {
            newMinutes = "0" + minutes.ToString();
        }
        else
        {
            newMinutes = minutes.ToString();
        }
        if (seconds < 0)
        {
            newSeconds = "00";
        }
        else if (seconds < 10)
        {
            newSeconds = "0" + seconds.ToString();
        }
        else
        {
            newSeconds = seconds.ToString();
        }
        text.text = newMinutes + " : " + newSeconds;
    }
    
}
