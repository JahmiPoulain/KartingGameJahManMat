using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private LapManager lapManager;
    [SerializeField] private KartScriptV2 kart;
    private int nextIndex = 1;


    public int NextIndex { get => nextIndex; set => nextIndex = value; }

    private void Awake()
    {
        foreach (Checkpoint checkpoint in lapManager.Checkpoints)
        {
            checkpoint.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (kart.currentSpeed <=-1)
        {
            foreach (Checkpoint checkpoint in lapManager.Checkpoints)
            {
                checkpoint.gameObject.SetActive(false);
            }
        }
        else
        {
            foreach (Checkpoint checkpoint in lapManager.Checkpoints)
            {
                checkpoint.gameObject.SetActive(true);
            }
        }
    }


    public void CompareCheckpoint(Checkpoint checkpoint)
    {
        if(checkpoint.Index == NextIndex)
        {
            NextIndex++;
        }
        else
        {
            return;
        }
    }
}
