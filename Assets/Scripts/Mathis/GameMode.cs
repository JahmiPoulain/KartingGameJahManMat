using UnityEngine;

public abstract class GameMode : MonoBehaviour
{
    protected LapManager lapManager;
    protected KartScriptV2 kartScript;

    protected int maxLaps;
    protected bool raceStarted = false;
    protected bool raceFinished = false;

    public bool getRaceStarted() => raceStarted;
    public bool RaceFinished { get => raceFinished; protected set => raceFinished = value; }
    public int MaxLaps { get => maxLaps; protected set => maxLaps = value; }

    public virtual void Initialize(LapManager lm, KartScriptV2 ks)
    {
        this.lapManager = lm;
        this.kartScript = ks;
    }

    public virtual void OnLapCompleted(float lapTime)
    {
        CheckRaceCompletion();
    }

    public virtual void CheckRaceCompletion()
    {
        if (!raceFinished && lapManager.CurrentLap >= MaxLaps)
        {
            CompleteRace();
        }
    }

    public abstract void CompleteRace();
}