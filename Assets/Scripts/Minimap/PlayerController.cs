using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Déplacement")]
    public float speed = 5f;
    public float speedMax = 15f;

    [Header("Souris")]
    public float mouseSensitivity = 100f;
    public Transform playerCamera;

    private float xRotation = 0f;

    [Header("UI")]
    public RectTransform minimapIcon;
    public RectTransform minimapParent;
    public RectTransform aiguille;

    void Start()
    {
        // Verrouille la souris au centre de l'écran
        Cursor.lockState = CursorLockMode.Locked;

        // Position de départ de l'aiguille
        aiguille.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    void Update()
    {
        Look(); // Gestion souris
        Move(); // Déplacement
    }

    void Look()
    {
        // Récupère les mouvements de souris
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotation verticale (haut/bas)
        xRotation -= mouseY;

        // Limite pour éviter de tourner la tête à 360°
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Applique rotation caméra
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotation horizontale du joueur
        transform.Rotate(Vector3.up * mouseX);
    }

    void Move()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Mouvement relatif à la direction du joueur
        Vector3 movement = transform.right * moveX + transform.forward * moveZ;

        // Normalisation (évite la diagonale rapide)
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        transform.Translate(speed * Time.deltaTime * movement, Space.World);

        // -------- TON SYSTÈME D'AIGUILLE (inchangé) --------

        float currentSpeed = movement.magnitude * speed;
        float normalizedSpeed = currentSpeed / speedMax;
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        float angle = Mathf.Lerp(90f, -90f, normalizedSpeed);
        aiguille.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}