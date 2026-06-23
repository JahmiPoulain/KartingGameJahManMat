using UnityEngine;

public class PnjVibes : MonoBehaviour
{
    [Header("Configuration du Parcours")]
    public Transform[] points;
    public float speed = 3f;
    public float rotationSpeed = 10f;
    public float arrivalDistance = 0.2f;

    [Header("Param�tres du Sautillant (Funny)")]
    public float bounceForce = 0.5f;
    public float bounceSpeed = 10f;
    public float tiltAmount = 15f;

    [Header("Param�tres du Idle (Calme)")]
    public float idleBounceForce = 0.1f;
    public float idleBounceSpeed = 2f;
    public float idleTiltAmount = 3f;

    [Header("Param�tres d'Impact (Ragdoll)")]
    public float impactForce = 20f;
    public float explosionRadius = 3f;
    public float upwardModifier = 1.5f;

    [Header("Param�tres du Break Dance (custom)")]
    // Vitesse de rotation sur le cr�ne (degr�s par seconde).
    public float breakSpinSpeed = 540f;
    // Inclinaison du corps quand il tourne sur la t�te (180 = compl�tement renvers�).
    public float breakFlipAngle = 180f;
    // Petit balancement/vacillement pendant la toupie.
    public float breakWobbleAmount = 10f;
    public float breakWobbleSpeed = 6f;
    // Hauteur � laquelle le corps se soul�ve pendant le break dance.
    public float breakLiftHeight = 0.3f;

    private int currentPointIndex = 0;
    private Vector3 meshOffset;
    private float hopTimer;
    private float breakSpinAngle;
    private float breakWobbleTimer;

    private bool isRagdoll = false;
    private Rigidbody mainRigidbody;
    private Collider mainCollider;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private bool IsIdle = false;

    [SerializeField]
    private bool isBreakDancing = false;
    

    void Start()
    {
        // add constraint to the rigid body to avoid any glitch thins and stuff shity butty physics.

        
        meshOffset = transform.position;

        mainRigidbody = GetComponent<Rigidbody>();
        mainCollider = GetComponent<Collider>();

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        mainRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        DisableRagdoll();


        if (points.Length == 0)
        {
            IsIdle = true;

            foreach (Rigidbody rb in ragdollRigidbodies)
            {
                rb.isKinematic = true;
            }

        }
        else
        {
            // On demarre la boucle par le point le plus proche du PNJ,
            // pas forcement le premier de la liste.
            currentPointIndex = GetClosestPointIndex();
        }

        if (isBreakDancing)
        {
            foreach (Rigidbody rb in ragdollRigidbodies)
            {
                rb.isKinematic = true;
            }
        }
    }

    // Retourne l'index du point le plus proche de la position actuelle du PNJ.
    int GetClosestPointIndex()
    {
        int closestIndex = 0;
        float closestSqrDistance = Mathf.Infinity;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;

            float sqrDistance = (points[i].position - transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    void Update()
    {
        // En ragdoll : la physique s'occupe de tout, on ne touche a rien.
        if (isRagdoll) return;

        // Break dance : il tourne sur son cr�ne, prioritaire sur tout le reste.
        if (isBreakDancing)
        {
            ApplyBreakDanceAnimation();
            return;
        }

        // Aucun point : on ne bouge pas, on reste en idle calme.
        if (IsIdle)
        {
            ApplyIdleAnimation();
            return;
        }

        bool arrived = MoveAndRotate();

        // Un seul point et on est arrive : on reste sur place en idle.
        if (points.Length == 1 && arrived)
            ApplyIdleAnimation();
        else
            ApplyFunnyAnimation();
    }

    // Retourne true si on est arrive au point courant.
    bool MoveAndRotate()
    {
        Vector3 targetPos = points[currentPointIndex].position;
        Vector3 flatTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);

        Vector3 direction = (flatTarget - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, flatTarget, speed * Time.deltaTime);

        bool arrived = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                        new Vector3(flatTarget.x, 0, flatTarget.z)) < arrivalDistance;

        // Avec plusieurs points on passe au suivant ; avec un seul on reste dessus.
        if (arrived && points.Length > 1)
        {
            currentPointIndex = (currentPointIndex + 1) % points.Length;
        }

        return arrived;
    }

    void ApplyFunnyAnimation()
    {
        hopTimer += Time.deltaTime * bounceSpeed;

        float hopY = Mathf.Abs(Mathf.Sin(hopTimer)) * bounceForce;
        float tiltZ = Mathf.Sin(hopTimer) * tiltAmount;

        transform.GetChild(0).localPosition = new Vector3(0, hopY, 0);
        transform.GetChild(0).localRotation = Quaternion.Euler(0, 0, tiltZ);
    }

    // Break dance : le corps se renverse et tourne sur son cr�ne comme une toupie.
    void ApplyBreakDanceAnimation()
    {
        // Rotation continue autour de l'axe vertical (la toupie sur la t�te).
        breakSpinAngle += breakSpinSpeed * Time.deltaTime;
        breakWobbleTimer += Time.deltaTime * breakWobbleSpeed;

        // Petit vacillement pour que �a ait l'air vivant et pas robotique.
        float wobble = Mathf.Sin(breakWobbleTimer) * breakWobbleAmount;

        // On soul�ve un peu le corps pendant qu'il tourne.
        float lift = breakLiftHeight;

        transform.GetChild(0).localPosition = new Vector3(0, lift, 0);
        // X = renvers� sur la t�te (breakFlipAngle), Y = la toupie, Z = vacillement.
        transform.GetChild(0).localRotation = Quaternion.Euler(breakFlipAngle, breakSpinAngle, wobble);
    }

    // Version calme de l'animation : leger balancement / respiration sur place.
    void ApplyIdleAnimation()
    {
        hopTimer += Time.deltaTime * idleBounceSpeed;

        float hopY = Mathf.Abs(Mathf.Sin(hopTimer)) * idleBounceForce;
        float tiltZ = Mathf.Sin(hopTimer) * idleTiltAmount;

        transform.GetChild(0).localPosition = new Vector3(0, hopY, 0);
        transform.GetChild(0).localRotation = Quaternion.Euler(0, 0, tiltZ);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") && !isRagdoll)
        {
            TriggerRagdoll(collision.transform.position);
        }
    }


    void DisableRagdoll()
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != mainRigidbody)
                rb.isKinematic = true;
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col != mainCollider)
                col.enabled = false;
        }
    }

    void TriggerRagdoll(Vector3 impactPoint)
    {
        isRagdoll = true;

        if (mainCollider != null) mainCollider.enabled = false;
        if (mainRigidbody != null) mainRigidbody.isKinematic = true;

        // desactiver les constraints pour que le ragdoll puisse se comporter normalement
        mainRigidbody.constraints = RigidbodyConstraints.None;

        foreach (Collider col in ragdollColliders)
        {
            col.enabled = true;
        }

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = false;

            rb.AddExplosionForce(impactForce, impactPoint, explosionRadius, upwardModifier, ForceMode.Impulse);
        }
    }
}
