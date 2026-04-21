using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;

    private GameManager()
    {
        instance = this;
    }

    public GameManager Instance()
    {
        if (instance == null) instance = new GameManager();
        return instance;
    }

    public enum GameMode
    {
        TimeTrial,
        TimeAttack
    }

    public GameMode currentMode;


}