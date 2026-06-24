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

    [Header("Param�tres du Bourr� (Drunk)")]
    // Vitesse d'avance (plus lent que la normale, il tra�ne).
    public float drunkSpeed = 2f;
    // Vitesse de rotation : faible = il tourne mollement, en retard sur la direction.
    public float drunkRotationSpeed = 3f;
    // Force du serpentin lat�ral (il zigzague autour du chemin vers le point).
    public float drunkSwayAmount = 1.5f;
    // Fr�quence du titubement (lent = grandes embard�es, rapide = tremblote).
    public float drunkSwaySpeed = 1.5f;
    // De combien de degr�s son regard part de travers par rapport � la vraie direction.
    public float drunkHeadingWobble = 45f;
    // Inclinaison max du corps qui tangue dans tous les sens.
    public float drunkBodyTilt = 25f;
    // Sautillement irr�gulier vertical.
    public float drunkBobSpeed = 4f;
    public float drunkBobHeight = 0.15f;

    private int currentPointIndex = 0;
    // Sens de parcours des points : +1 = ordre normal, -1 = ordre invers�.
    // Tir� au hasard (une chance sur deux) au d�marrage.
    private int pointDirection = 1;
    private Vector3 meshOffset;
    private float hopTimer;
    private float breakSpinAngle;
    private float breakWobbleTimer;
    // Graine al�atoire propre � ce PNJ pour que chaque bourr� titube diff�remment.
    private float drunkSeed;

    private bool isRagdoll = false;
    private Rigidbody mainRigidbody;
    private Collider mainCollider;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private bool IsIdle = false;

    [SerializeField]
    private bool isBreakDancing = false;

    [SerializeField]
    private bool isDrunk = false; // Rotate et sorient vers des endroit aleatoire (vers les direction donner par moi là les points) genre il est bourré genre.
    

    void Start()
    {
        // add constraint to the rigid body to avoid any glitch thins and stuff shity butty physics.

        
        meshOffset = transform.position;

        // Chaque bourr� a sa propre graine pour ne pas tituber tous pareil.
        drunkSeed = Random.value * 100f;

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

            // Une chance sur deux de parcourir les points dans le sens inverse.
            pointDirection = Random.value < 0.5f ? -1 : 1;
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

        // Bourr� : il va globalement vers les points mais titube, zigzague et tangue.
        if (isDrunk)
        {
            MoveAndRotateDrunk();
            ApplyDrunkAnimation();
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

        // Avec plusieurs points on passe au suivant (dans le sens choisi) ; avec un seul on reste dessus.
        if (arrived && points.Length > 1)
        {
            currentPointIndex = (currentPointIndex + pointDirection + points.Length) % points.Length;
        }

        return arrived;
    }

    // Version bourr�e du d�placement : il rejoint quand m�me le point demand�,
    // mais en serpentant, en visant de travers et en tournant mollement.
    void MoveAndRotateDrunk()
    {
        Vector3 targetPos = points[currentPointIndex].position;
        Vector3 flatTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);

        Vector3 direction = (flatTarget - transform.position).normalized;

        // Bruit de Perlin -> -1..1, lisse, pour un titubement organique (pas saccad�).
        float swayNoise = (Mathf.PerlinNoise(Time.time * drunkSwaySpeed + drunkSeed, drunkSeed) - 0.5f) * 2f;

        if (direction != Vector3.zero)
        {
            // Son regard part de travers de la vraie direction (il vise mal).
            Quaternion drunkRot = Quaternion.Euler(0, swayNoise * drunkHeadingWobble, 0);
            Quaternion targetRotation = Quaternion.LookRotation(drunkRot * direction);
            // Rotation lente : il r�agit en retard, l'air pas net.
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, drunkRotationSpeed * Time.deltaTime);
        }

        // Vitesse irr�guli�re : il acc�l�re et ralentit n'importe comment.
        float speedNoise = Mathf.PerlinNoise(Time.time * drunkSwaySpeed * 0.5f, drunkSeed + 10f);
        float currentSpeed = drunkSpeed * Mathf.Lerp(0.35f, 1f, speedNoise);

        // On avance vers la cible (garantit qu'il finit par arriver)...
        transform.position = Vector3.MoveTowards(transform.position, flatTarget, currentSpeed * Time.deltaTime);

        // ...mais on ajoute un zigzag lat�ral perpendiculaire au chemin (le serpentin).
        Vector3 sideways = new Vector3(-direction.z, 0, direction.x);
        transform.position += sideways * swayNoise * drunkSwayAmount * Time.deltaTime;

        bool arrived = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                        new Vector3(flatTarget.x, 0, flatTarget.z)) < arrivalDistance;

        if (arrived && points.Length > 1)
        {
            currentPointIndex = (currentPointIndex + pointDirection + points.Length) % points.Length;
        }
    }

    // Le corps tangue dans tous les sens et sautille de fa�on irr�guli�re.
    void ApplyDrunkAnimation()
    {
        // Deux bruits d�cal�s pour incliner le corps en X et en Z ind�pendamment.
        float tiltNoiseX = (Mathf.PerlinNoise(Time.time * drunkBobSpeed * 0.5f + drunkSeed, 0f) - 0.5f) * 2f;
        float tiltNoiseZ = (Mathf.PerlinNoise(0f, Time.time * drunkBobSpeed * 0.5f + drunkSeed) - 0.5f) * 2f;

        float tiltX = tiltNoiseX * drunkBodyTilt;
        float tiltZ = tiltNoiseZ * drunkBodyTilt;

        // Sautillement vertical avec une fr�quence l�g�rement modul�e par le bruit.
        float bobY = Mathf.Abs(Mathf.Sin(Time.time * drunkBobSpeed + tiltNoiseX)) * drunkBobHeight;

        transform.GetChild(0).localPosition = new Vector3(0, bobY, 0);
        transform.GetChild(0).localRotation = Quaternion.Euler(tiltX, 0, tiltZ);
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
