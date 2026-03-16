using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private LapManager lapManager;
    [SerializeField] private KartScriptV2 kartScript;

    private int nextIndex = 1;


    public int NextIndex { get => nextIndex; set => nextIndex = value; }

    private void Awake()
    {
        foreach (Checkpoint checkpoint in lapManager.Checkpoints)
        {
            checkpoint.gameObject.SetActive(true);
        }
    }




    public void CompareCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint.Index == NextIndex)
        {
            NextIndex++;
            Vector3 newPos = checkpoint.gameObject.transform.position;
            kartScript.StartPosition = newPos;
        }
        else
        {
            return;
        }
    }
}
