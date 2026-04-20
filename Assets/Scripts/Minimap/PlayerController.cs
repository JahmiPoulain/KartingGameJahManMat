using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float speedMax = 15f;

    public RectTransform minimapIcon;
    public RectTransform minimapParent;
    public RectTransform aiguille;

    void Start()
    {
        // Position de départ de l'aiguille
        aiguille.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        MovePlayer(moveX, moveZ);
    }

    void MovePlayer(float moveX, float moveZ)
    {
        Vector3 movement = new(moveX, 0f, moveZ);

        // Empêche la diagonale d'être plus rapide
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        transform.Translate(speed * Time.deltaTime * movement, Space.World);

        // Vitesse actuelle du joueur
        float currentSpeed = movement.magnitude * speed;

        // Ramène la vitesse entre 0 et 1
        float normalizedSpeed = currentSpeed / speedMax;

        // Sécurité pour rester entre 0 et 1
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        // Convertit la vitesse en angle entre 0 et -90
        float angle = Mathf.Lerp(90f, -90f, normalizedSpeed);

        // Applique la rotation
        aiguille.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}