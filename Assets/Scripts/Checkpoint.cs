using UnityEngine;

public class Checkpoint : MonoBehaviour
{

    [SerializeField] private int index;
    [SerializeField] CheckpointManager checkpointManager;


    public int Index { get => index; private set => index = value; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            checkpointManager.CompareCheckpoint(this);
            Debug.Log("Tu as traversé le checkpoint n" + Index);
            gameObject.SetActive(false);

        }
    }


}
