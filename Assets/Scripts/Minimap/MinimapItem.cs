using UnityEngine;

public class MinimapItem : MonoBehaviour
{
    public RectTransform icon; // icône UI associée

    public bool follow = true; // suit en temps réel ou non
    public bool rotateWithObject = true;

    [HideInInspector]
    public Vector3 startPosition; // position de départ (si follow = false)

    void Awake()
    {
        startPosition = transform.position;
    }
}