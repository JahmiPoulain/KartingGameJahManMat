using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance; // Correction du singleton

    private GameManager()
    {
        instance = this;
    }

    public static GameManager Instance()
    {
        return instance;
    }

    public enum GameModeType { TimeTrial, TimeAttack }
    public GameModeType currentMode;

    private void Awake()
    {

    }

    // Cette méthode sera appelée par un "LevelLoader" ou au Start de la scène de course
    public void SetupGameMode(GameObject racingKart, LapManager lm)
    {
        GameMode mode;
        if (currentMode == GameModeType.TimeAttack)
            mode = racingKart.AddComponent<TimeAttack>();
        else
            mode = racingKart.AddComponent<ContreLaMontre>();

        // On injecte les dépendances tout de suite
        mode.Initialize(lm, racingKart.GetComponent<KartScriptV2>());
    }
}