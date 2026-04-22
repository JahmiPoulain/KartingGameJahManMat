using UnityEngine;

public abstract class GameMode : MonoBehaviour
{

    [SerializeField] protected LapManager lapManager;
    [SerializeField] protected KartScriptV2 kartScript;

    protected int maxLaps;
    protected bool raceStarted = false;
    protected bool raceFinished = false;

    public bool getRaceStarted()
    {
        return raceStarted;
    }

    public bool RaceFinished { get => raceFinished; private set => raceFinished = value; }
    public int MaxLaps { get => maxLaps; private set => maxLaps = value; }

    public virtual void Initialize(LapManager lm, KartScriptV2 ks)
    {
        this.lapManager = lm;
        this.kartScript = ks;
    }

    // Cette méthode peut être ignorée ou surchargée
    public virtual void OnLapCompleted(float lapTime)
    {
        // Par défaut, on vérifie si la course est finie
        CheckRaceCompletion();
    }

    public virtual void CheckRaceCompletion()
    {
        if (!raceFinished && lapManager.CurrentLap > MaxLaps)
        {
            CompleteRace();
        }
    }

    public abstract void CompleteRace();
}
