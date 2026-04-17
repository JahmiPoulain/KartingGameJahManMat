using UnityEngine;
using System.Collections;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private LapManager lapManager;
    [SerializeField] private KartScriptV3 kartScript;

    private int nextIndex = 1;

    private Vector3 newPos;
    private bool hasCheckpoint = false; // ✅ savoir si un checkpoint a été validé

    private bool isOffTrack = false;

    public int NextIndex { get => nextIndex; set => nextIndex = value; }
    public bool IsOffTrack { get => isOffTrack; set => isOffTrack = value; }

    private void Awake()
    {
        foreach (Checkpoint checkpoint in lapManager.Checkpoints)
        {
            checkpoint.gameObject.SetActive(true);
        }
    }

    public void Update()
    {
        if (isOffTrack)
        {
            StartCoroutine(Respawn());
            isOffTrack = false;
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

    public IEnumerator Respawn()
    {
        kartScript.canDrive = false;
        yield return new WaitForSeconds(1);
        // ✅ si aucun checkpoint → on utilise la position de départ
        if (hasCheckpoint)
        {

            kartScript.StartPosition = newPos;
            kartScript.currentSpeed = 0;
            yield return new WaitForSeconds(0.5f);
            kartScript.canDrive = true;
        }
        yield return new WaitForSeconds(1);
        // ✅ reset physique propre
        kartScript.CurrentPosition = kartScript.StartPosition;
        kartScript.currentSpeed = 0;
        kartScript.rb.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(0.5f);
        kartScript.canDrive = true;

    }
}