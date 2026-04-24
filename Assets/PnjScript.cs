using UnityEngine;

public class PnjVibes : MonoBehaviour
{
    [Header("Configuration du Parcours")]
    public Transform[] points;
    public float speed = 3f;
    public float rotationSpeed = 10f;
    public float arrivalDistance = 0.2f;

    [Header("Paramètres du Sautillant (Funny)")]
    public float bounceForce = 0.5f;   // Hauteur du saut
    public float bounceSpeed = 10f;   // Vitesse du sautillement
    public float tiltAmount = 15f;     // L'angle du balancement gauche/droite

    private int currentPointIndex = 0;
    private Vector3 meshOffset;
    private float hopTimer;

    void Start()
    {
        // On mémorise la position de départ locale pour le sautillement
        meshOffset = transform.position;
    }

    void Update()
    {
        if (points.Length == 0) return;

        MoveAndRotate();
        ApplyFunnyAnimation();
    }

    void MoveAndRotate()
    {
        // 1. Calcul de la cible en ignorant la hauteur (Y)
        Vector3 targetPos = points[currentPointIndex].position;
        Vector3 flatTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);

        // 2. Rotation fluide vers la cible
        Vector3 direction = (flatTarget - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 3. Déplacement vers la cible
        transform.position = Vector3.MoveTowards(transform.position, flatTarget, speed * Time.deltaTime);

        // 4. Changement de point (Distance calculée uniquement sur X et Z)
        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                             new Vector3(flatTarget.x, 0, flatTarget.z)) < arrivalDistance)
        {
            currentPointIndex = (currentPointIndex + 1) % points.Length;
        }
    }

    void ApplyFunnyAnimation()
    {
        // On fait progresser un timer interne
        hopTimer += Time.deltaTime * bounceSpeed;

        // Calcul du saut (Sinus absolu pour toujours rester au-dessus du sol)
        float hopY = Mathf.Abs(Mathf.Sin(hopTimer)) * bounceForce;

        // Calcul du balancement (Sinus simple pour aller de -tilt à +tilt)
        float tiltZ = Mathf.Sin(hopTimer) * tiltAmount;

        // On applique uniquement sur l'apparence visuelle
        // Astuce : Si tu as un modèle 3D enfant, applique ça à l'enfant pour ne pas casser la physique
        transform.GetChild(0).localPosition = new Vector3(0, hopY, 0);
        transform.GetChild(0).localRotation = Quaternion.Euler(0, 0, tiltZ);
    }
}