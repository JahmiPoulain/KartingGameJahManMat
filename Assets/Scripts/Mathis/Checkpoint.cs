using UnityEngine;

public class Checkpoint : MonoBehaviour
{

    [SerializeField] private int index;
    [SerializeField] GameObject tutoUI;
    [SerializeField] CheckpointManager checkpointManager;


    public int Index { get => index; private set => index = value; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(this.index == 1)
            {
                tutoUI.SetActive(false);
            }
            checkpointManager.CompareCheckpoint(this);
            Debug.Log("Tu as traversé le checkpoint n" + Index);
            gameObject.SetActive(false);

        }
    }


}
