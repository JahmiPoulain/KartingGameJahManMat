using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private LapManager lapManager;
    [SerializeField] private KartScriptV2 kartScript;

    private int nextIndex = 1;
    private Vector3 newPos;


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
            newPos = checkpoint.gameObject.transform.position;

        }
        else
        {
            return;
        }
    }
    public void Respawn()
    {
        if (newPos != null)
        {
            kartScript.StartPosition = newPos;
            kartScript.CurrentPosition = kartScript.StartPosition;
            kartScript.rb.linearVelocity = Vector3.zero;
            kartScript.currentSpeed = 0;
        }
        kartScript.CurrentPosition = kartScript.StartPosition;
    }
}
