using UnityEngine;

public class PnjVibes : MonoBehaviour
{
    [Header("Configuration du Parcours")]
    public Transform[] points;
    public float speed = 3f;
    public float rotationSpeed = 10f;
    public float arrivalDistance = 0.2f;

    [Header("Paramètres du Sautillant (Funny)")]
    public float bounceForce = 0.5f;
    public float bounceSpeed = 10f;
    public float tiltAmount = 15f;

    [Header("Paramètres d'Impact (Ragdoll)")]
    public float impactForce = 20f;
    public float explosionRadius = 3f;
    public float upwardModifier = 1.5f;

    private int currentPointIndex = 0;
    private Vector3 meshOffset;
    private float hopTimer;

    private bool isRagdoll = false;
    private Rigidbody mainRigidbody;
    private Collider mainCollider;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    void Start()
    {
        meshOffset = transform.position;

        mainRigidbody = GetComponent<Rigidbody>();
        mainCollider = GetComponent<Collider>();

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        DisableRagdoll();
    }

    void Update()
    {
        if (isRagdoll || points.Length == 0) return;

        MoveAndRotate();
        ApplyFunnyAnimation();
    }

    void MoveAndRotate()
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

        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                             new Vector3(flatTarget.x, 0, flatTarget.z)) < arrivalDistance)
        {
            currentPointIndex = (currentPointIndex + 1) % points.Length;
        }
    }

    void ApplyFunnyAnimation()
    {
        hopTimer += Time.deltaTime * bounceSpeed;

        float hopY = Mathf.Abs(Mathf.Sin(hopTimer)) * bounceForce;
        float tiltZ = Mathf.Sin(hopTimer) * tiltAmount;

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