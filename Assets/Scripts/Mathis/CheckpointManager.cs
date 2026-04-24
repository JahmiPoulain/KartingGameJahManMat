using UnityEngine;
using System.Collections;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private LapManager lapManager;
    [SerializeField] private KartScriptV2 kartScript;
    [SerializeField] private Checkpoint[] checkpoints;

    private int nextIndex = 1;

    private Vector3 newPos;
    private Quaternion newRotation;
    private bool hasCheckpoint = false;

    // Propriétés
    public int NextIndex { get => nextIndex; set => nextIndex = value; }
    public bool HasCheckpoint { get => hasCheckpoint; set => hasCheckpoint = value; }
    public Vector3 NewPos { get => newPos; set => newPos = value; }
    public Quaternion NewRotation { get => newRotation; set => newRotation = value; }

    public Checkpoint[] Checkpoints { get => checkpoints; set => checkpoints = value; }

    // --- AJOUT : Pour que le LapManager sache combien il y a de checkpoints au total ---
    public int TotalCheckpointCount { get => checkpoints.Length; }

    private void Awake()
    {
        // On s'assure que tous les checkpoints sont actifs au départ
        if (lapManager != null && checkpoints != null)
        {
            foreach (Checkpoint checkpoint in checkpoints)
            {
                checkpoint.gameObject.SetActive(true);
            }
        }
    }

    public void CompareCheckpoint(Checkpoint checkpoint)
    {
        // Si le joueur traverse le bon checkpoint dans l'ordre
        if (checkpoint.Index == nextIndex)
        {
            nextIndex++;

            // Enregistrement de la position de respawn sécurisée
            newPos = checkpoint.transform.position + Vector3.up * 0.5f;
            newRotation = checkpoint.transform.rotation;

            hasCheckpoint = true;

            Debug.Log($"Checkpoint {checkpoint.Index} validé. Prochain attendu : {nextIndex}");
        }
        else
        {
            Debug.LogWarning($"Mauvais checkpoint ! Traversé : {checkpoint.Index}, Attendu : {nextIndex}");
        }
    }

    // Méthode pour réinitialiser le cycle lors d'un nouveau tour
    public void ResetCheckpoints()
    {
        nextIndex = 1;
        hasCheckpoint = false;

        // On réactive visuellement les checkpoints pour le nouveau tour
        foreach (Checkpoint checkpoint in checkpoints)
        {
            checkpoint.gameObject.SetActive(true);
        }
    }
}