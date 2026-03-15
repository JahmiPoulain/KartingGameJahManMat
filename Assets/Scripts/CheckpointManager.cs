using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    private int nextIndex;

    public int NextIndex { get => nextIndex; private set => nextIndex = value; }

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
