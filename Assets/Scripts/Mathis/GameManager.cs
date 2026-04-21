using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // Correction du singleton

    public enum GameModeType { TimeTrial, TimeAttack }
    public GameModeType currentMode;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Cette méthode sera appelée par un "LevelLoader" ou au Start de la scène de course
    public void SetupGameMode(GameObject racingKart)
    {
        if (currentMode == GameModeType.TimeAttack)
        {
            racingKart.AddComponent<TimeAttack>();
        }
        else
        {
            racingKart.AddComponent<ContreLaMontre>();
        }
    }
}