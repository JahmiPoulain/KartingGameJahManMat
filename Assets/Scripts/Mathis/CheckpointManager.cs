using UnityEngine;
using System.Collections;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private LapManager lapManager;
    [SerializeField] private KartScriptV2 kartScript;

    private int nextIndex = 1;

    private Vector3 newPos;
    private Quaternion newRotation;
    private bool hasCheckpoint = false; // ✅ savoir si un checkpoint a été validé



    public int NextIndex { get => nextIndex; set => nextIndex = value; }
    public bool HasCheckpoint { get => hasCheckpoint; set => hasCheckpoint = value; }
    public Vector3 NewPos { get => newPos; set => newPos = value; }
    public Quaternion NewRotation { get => newRotation; set => newRotation = value; }

    private void Awake()
    {
        foreach (Checkpoint checkpoint in lapManager.Checkpoints)
        {
            checkpoint.gameObject.SetActive(true);
        }
    }

    public void Update()
    {

    }

    public void CompareCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint.Index == NextIndex)
        {
            NextIndex++;

            // ✅ on enregistre la position + petit offset pour éviter les murs/sol
            NewPos = checkpoint.transform.position + Vector3.up * 0.5f;
            NewRotation = checkpoint.transform.rotation;

            HasCheckpoint = true; // ✅ checkpoint valide
        }
    }


}