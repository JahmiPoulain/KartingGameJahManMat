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
        if(InversionCatcher.instance != null)
        {
            // Si la map est inversée, on inverse aussi les checkpoints
            if (InversionCatcher.instance.Inverted)
            {
                foreach (Checkpoint checkpoint in checkpoints)
                {
                    checkpoint.transform.Rotate(0, 180, 0);
                }
            }
        }
    }

    public void CompareCheckpoint(Checkpoint checkpoint)
    {
        // On calcule l'index que le joueur DOIT percuter maintenant
        int expectedIndex;

        if (InversionCatcher.instance.Inverted)
        {
            // Formule : Total - (Nombre déjà validés)
            // Exemple : 10 total, 0 validés -> on attend le 10.
            expectedIndex = TotalCheckpointCount - (nextIndex - 1);
        }
        else
        {
            expectedIndex = nextIndex;
        }

        if (checkpoint.Index == expectedIndex)
        {
            nextIndex++; // On progresse (on a validé un checkpoint de plus)

            // Enregistrement du respawn
            newPos = checkpoint.transform.position + Vector3.up * 0.5f;
            newRotation = checkpoint.transform.rotation;

            // Si inversé, on tourne la rotation de 180° pour réapparaître dans le bon sens
            if (MainMenuUIManager.Instance.isMapInverted) newRotation *= Quaternion.Euler(0, 180, 0);

            hasCheckpoint = true;
            Debug.Log($"Checkpoint {checkpoint.Index} validé. Progrès : {nextIndex - 1}/{TotalCheckpointCount}");
        }
        else
        {
            Debug.LogWarning($"Mauvais sens ! Traversé : {checkpoint.Index}, Attendu : {expectedIndex}");
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