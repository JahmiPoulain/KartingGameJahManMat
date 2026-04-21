using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.VersionControl.Message;

public class Respawner : MonoBehaviour
{
    [SerializeField] Transform kartTransform;
    [SerializeField] CheckpointManager checkPointManager;
    [SerializeField] KartScriptV2 kartScriptV2;

    Vector3 dir;

    private bool isOffTrack = false;

    public bool IsOffTrack { get => isOffTrack; set => isOffTrack = value; }




    // Update is called once per frame
    void Update()
    {
        if (isOffTrack)
        {
            Respawn();
            //isOffTrack = false;
        }
    }

    private void FixedUpdate()
    {
        CheckIfOffTrack();
    }

    void CheckIfOffTrack()
    {
        if (kartScriptV2.outOfBounds)
        {
            isOffTrack = true;  
        }
        if (kartTransform.position.y <= 2)
        {

            isOffTrack = true;


        }
    }


    public void Respawn()
    {
        kartScriptV2.GetComponent<SphereCollider>().enabled = false;

        kartScriptV2.outOfBounds = true;



        kartScriptV2.canDrive = false;
        // ✅ si aucun checkpoint → on utilise la position de départ
        if (checkPointManager.HasCheckpoint)
        {
            dir = checkPointManager.NewPos - kartTransform.position;
            float upForce = Mathf.Clamp(dir.magnitude, 0f, 2f);
            kartTransform.rotation = Quaternion.RotateTowards(kartTransform.rotation, checkPointManager.NewRotation, Mathf.Clamp(upForce / 8f, 1f, 5f));
            Debug.Log(checkPointManager.NewRotation);
            kartTransform.position += (dir.normalized * 0.5f + dir + new Vector3(0, upForce, 0)) * Time.fixedDeltaTime;

        }
        else
        {
            // ✅ reset physique propre
            dir = kartScriptV2.StartPosition - kartTransform.position;
            float upForce = Mathf.Clamp(dir.magnitude, 0f, 2f);
            kartTransform.rotation = Quaternion.RotateTowards(kartTransform.rotation, kartScriptV2.StartRotation, Mathf.Clamp(upForce / 8f, 1f, 5f));
            Debug.Log(checkPointManager.NewRotation);
            kartTransform.position += (dir.normalized * 0.5f + dir + new Vector3(0, upForce, 0)) * Time.fixedDeltaTime;

        }
       // Debug.Log(dir.sqrMagnitude);
        if (dir.sqrMagnitude < 0.1f)
        {
            Debug.Log("retour effectué");
            kartScriptV2.GetComponent<SphereCollider>().enabled = true;
            kartScriptV2.canDrive = true;
            isOffTrack = false;
            kartScriptV2.outOfBounds = false;
        }

    }

}
