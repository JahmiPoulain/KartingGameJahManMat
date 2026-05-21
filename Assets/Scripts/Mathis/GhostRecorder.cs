using UnityEngine;

public class GhostRecorder : MonoBehaviour
{
    public GhostData ghostData;
    public bool isRecording;

    void FixedUpdate()
    {
        if (isRecording)
        {
            ghostData.AddFrame(transform.position, transform.rotation);
        }
    }
}