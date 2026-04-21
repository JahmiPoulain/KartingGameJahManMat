using UnityEngine;

public class TimeAttack : GameMode
{
    private float bestTime;

    void Start()
    {
        bestTime = PlayerPrefs.GetFloat("BestTime", float.MaxValue);
    }

    public void SaveTime(float newTime)
    {
        if (newTime < bestTime)
        {
            bestTime = newTime;
            PlayerPrefs.SetFloat("BestTime", bestTime);
            PlayerPrefs.Save();

            Debug.Log("NOUVEAU RECORD !");
        }
    }

    public override void CompleteRace()
    {
        throw new System.NotImplementedException();
    }
}
