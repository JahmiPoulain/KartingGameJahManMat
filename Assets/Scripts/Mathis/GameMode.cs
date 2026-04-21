using UnityEngine;

public abstract class GameMode : MonoBehaviour
{

    [SerializeField] LapManager lapManager;
    [SerializeField] private KartScriptV2 kartScript;

    protected int maxLaps;
    protected bool raceStarted = false;
    protected bool raceFinished = false;

    public bool getRaceStarted()
    {
        return raceStarted;
    }

    public bool RaceFinished { get => raceFinished; private set => raceFinished = value; }
    public int MaxLaps { get => maxLaps; private set => maxLaps = value; }

    private void CheckRaceCompletion()
    {
        if (!raceFinished && lapManager.CurrentLap - 1 >= MaxLaps)
        {
            kartScript.canDrive = false;
            CompleteRace();
        }
    }

    public abstract void CompleteRace();
}
