using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.VersionControl.Message;

public class Respawner : MonoBehaviour
{
    [SerializeField] Transform kartTransform;
    [SerializeField] CheckpointManager checkPointManager;
    [SerializeField] KartScriptV3 kartScriptV3;

    private bool isOffTrack = false;

    public bool IsOffTrack { get => isOffTrack; set => isOffTrack = value; }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isOffTrack)
        {
            StartCoroutine(Respawn());
            isOffTrack = false;
        }
    }

    private void FixedUpdate()
    {
        CheckIfOffTrack();
    }

    void CheckIfOffTrack()
    {




        if (kartTransform.position.y <= 5)
        {

            StartCoroutine(Respawn());


        }
    }


    public IEnumerator Respawn()
    {
        kartScriptV3.canDrive = false;
        yield return new WaitForSeconds(1);
        // ✅ si aucun checkpoint → on utilise la position de départ
        if (checkPointManager.HasCheckpoint)
        {

            kartTransform.position = checkPointManager.NewPos;
            kartScriptV3.currentSpeed = 0;
            yield return new WaitForSeconds(0.5f);
            kartScriptV3.canDrive = true;
        }
        yield return new WaitForSeconds(1);
        // ✅ reset physique propre
        kartTransform.position = kartScriptV3.StartPosition;
        kartScriptV3.currentSpeed = 0;
        kartScriptV3.rb.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(0.5f);
        kartScriptV3.canDrive = true;

    }

}
