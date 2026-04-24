using UnityEngine;

public class GhostPlayer : MonoBehaviour
{
    public GhostData ghostData;
    private int currentFrame = 0;

    void FixedUpdate()
    {
        if (ghostData != null && currentFrame < ghostData.frames.Count)
        {
            // Appliquer la position et rotation enregistrées
            transform.position = ghostData.frames[currentFrame].position;
            transform.rotation = ghostData.frames[currentFrame].rotation;

            currentFrame++;
        }
    }
}