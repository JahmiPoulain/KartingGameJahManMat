using UnityEngine;

public class MinimapItem : MonoBehaviour
{
    public RectTransform icon; // Icône UI associée (à assigner dans l'Inspector)

    public bool follow = true;            // Suit la position en temps réel
    public bool rotateWithObject = true;  // Suit la rotation

    [HideInInspector]
    public Vector3 startPosition; // Position de départ (si objet statique)

    void Awake()
    {
        // On enregistre la position initiale pour les objets non mobiles
        startPosition = transform.position;
    }
}