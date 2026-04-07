using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private LapManager lapManager;
    [SerializeField] private KartScriptV3 kartScript;

    private int nextIndex = 1;

    private Vector3 newPos;
    private bool hasCheckpoint = false; // ✅ savoir si un checkpoint a été validé

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

            // ✅ on enregistre la position + petit offset pour éviter les murs/sol
            newPos = checkpoint.transform.position + Vector3.up * 0.5f;

            hasCheckpoint = true; // ✅ checkpoint valide
        }
    }

    public void Respawn()
    {
        // ✅ si aucun checkpoint → on utilise la position de départ
        if (hasCheckpoint)
        {
            kartScript.StartPosition = newPos;
        }

        // ✅ reset physique propre
        kartScript.CurrentPosition = kartScript.StartPosition;
        kartScript.rb.linearVelocity = Vector3.zero;
        kartScript.currentSpeed = 0;
    }
}