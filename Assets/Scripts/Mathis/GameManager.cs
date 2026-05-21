using UnityEngine;


public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    // Cette fonction permet d'accéder au GameManager partout via GameManager.Instance()
    public static GameManager Instance()
    {
        return _instance;
    }

    public enum GameModeType { TimeAttack, TimeTrial }
    public GameModeType currentMode;

    private void Awake()
    {
        // LA LOGIQUE MAGIQUE :
        if (_instance == null)
        {
            _instance = this;
            // Dit à Unity de ne pas détruire cet objet en changeant de scène
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si un autre GameManager existe déjà (ex: en revenant au menu), on détruit le nouveau
            Destroy(gameObject);
        }
    }


    // Cette méthode sera appelée par un "LevelLoader" ou au Start de la scène de course
    public GameMode SetupGameMode(GameObject racingKart, LapManager lm) // Ajoute GameMode comme type de retour
    {
        GameMode mode;
        if (currentMode == GameModeType.TimeAttack)
            mode = racingKart.AddComponent<TimeAttack>();
        else
            mode = racingKart.AddComponent<ContreLaMontre>();

        mode.Initialize(lm, racingKart.GetComponent<KartScriptV2>());
        return mode; // Renvoie le mode
    }
}