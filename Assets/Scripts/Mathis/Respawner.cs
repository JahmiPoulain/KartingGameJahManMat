
using UnityEngine;
using System.Collections;
public class Respawner : MonoBehaviour
{
    [SerializeField] private Transform kartTransform;
    [SerializeField] private CheckpointManager checkPointManager;
    [SerializeField] private KartScriptV2 kartScriptV2;

    Vector3 dir;

    private bool isOffTrack = false;

    public bool IsOffTrack { get => isOffTrack; set => isOffTrack = value; }

    [SerializeField] private Transform respawnBubble;
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
        if (kartScriptV2 == null) kartScriptV2 = FindFirstObjectByType<KartScriptV2>();
        
        if (kartScriptV2.outOfBounds)
        {
            isOffTrack = true;  
        }

        if (kartTransform.position.y <= -0.1f)
        {
            isOffTrack = true;
        }
    }

    public void Respawn()
    {
        kartScriptV2.GetComponent<SphereCollider>().enabled = false;

        kartScriptV2.outOfBounds = true;

        kartScriptV2.CanDrive = false;

        // si aucun checkpoint → on utilise la position de départ
        if (checkPointManager.HasCheckpoint)
        {
            dir = checkPointManager.NewPos - kartTransform.position;
            float upForce = Mathf.Clamp(dir.magnitude, 0f, 2f);
            kartTransform.rotation = Quaternion.RotateTowards(kartTransform.rotation, checkPointManager.NewRotation, Mathf.Clamp(upForce / 8f, 1f, 5f));
            //Debug.Log(checkPointManager.NewRotation);
            kartTransform.position += (dir.normalized * 0.5f + dir + new Vector3(0, upForce, 0)) * Time.fixedDeltaTime;

        }
        else
        {
            // reset physique propre
            dir = kartScriptV2.StartPosition - kartTransform.position;
            float upForce = Mathf.Clamp(dir.magnitude, 0f, 2f);
            kartTransform.rotation = Quaternion.RotateTowards(kartTransform.rotation, kartScriptV2.StartRotation, Mathf.Clamp(upForce / 8f, 1f, 5f));
            //Debug.Log(checkPointManager.NewRotation);
            kartTransform.position += (dir.normalized * 0.5f + dir + new Vector3(0, upForce, 0)) * Time.fixedDeltaTime;

        }


        respawnBubble.position = kartTransform.position;
        if (respawnBubble.localScale.x < 2f)
        {
            respawnBubble.localScale += Vector3.one * 10f * Time.deltaTime;
        }

        if (dir.sqrMagnitude < 0.1f)
        {
            //Debug.Log("retour effectué");
            kartScriptV2.GetComponent<SphereCollider>().enabled = true;
            //respawnBubble.localScale += Vector3.one;
            kartScriptV2.CanDrive = true;
            isOffTrack = false;
            kartScriptV2.outOfBounds = false;
            StartCoroutine(BlowUpBubble());
        }

        IEnumerator BlowUpBubble()
        {
            //Debug.Log("B");
            yield return null;
            if (respawnBubble.localScale.x < 4f)
            {
                //Debug.Log(respawnBubble.localScale.x + "      1");
                respawnBubble.position = kartTransform.position;
                respawnBubble.localScale += Vector3.one * 22f * Time.deltaTime;
                StartCoroutine(BlowUpBubble());
            }
            else
            {
                //Debug.Log(respawnBubble.localScale.x + "      2");
                respawnBubble.localScale = Vector3.zero;
            }
        }
    }
}
