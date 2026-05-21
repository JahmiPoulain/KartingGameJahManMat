using UnityEngine;

public class BirdIndividual : MonoBehaviour
{
    private float speed;
    private float noiseOffset;
    private Vector3 direction;
    private BirdFlockManager manager;
    private float internalTimer;
    private float currentBanking;

    // Pour éviter l'effet toupie, on stocke la rotation de direction séparément
    private Quaternion lookRotation;

    public void Initialize(BirdFlockManager manager, Pcg32 sharedRng, Vector3 startPos, Vector3 dir, float speed, float scale)
    {
        this.manager = manager;
        this.direction = dir.normalized;
        this.speed = speed;
        this.transform.position = startPos;
        this.transform.localScale = Vector3.one * scale;

        this.noiseOffset = sharedRng.NextFloat() * 1000f;
        this.internalTimer = 0f;
        this.currentBanking = 0f;

        // Initialise la rotation pour éviter un "snap" au départ
        this.lookRotation = Quaternion.LookRotation(direction);
    }

    void Update()
    {
        internalTimer += Time.deltaTime;

        // 1. Direction organique (Perlin Noise)
        float nX = Mathf.PerlinNoise(internalTimer * 0.4f, noiseOffset) - 0.5f;
        float nY = Mathf.PerlinNoise(noiseOffset, internalTimer * 0.4f) - 0.5f;
        Vector3 noiseVec = new Vector3(nX, nY, 0);
        Vector3 finalDir = (direction + transform.TransformDirection(noiseVec)).normalized;

        // 2. Calcul du Banking (Inclinaison)
        float angleDiff = Vector3.SignedAngle(transform.forward, finalDir, Vector3.up);
        currentBanking = Mathf.Lerp(currentBanking, -angleDiff * 3.0f, Time.deltaTime * 3f);

        // 3. Rotation (CORRECTION TOUPIE)
        // On calcule la rotation vers la cible
        Quaternion targetLook = Quaternion.LookRotation(finalDir, Vector3.up);
        // On lisse cette rotation
        lookRotation = Quaternion.Slerp(lookRotation, targetLook, Time.deltaTime * 2.5f);

        // On applique : Rotation de direction + Rotation d'inclinaison (Banking)
        // L'utilisation du "=" au lieu du "*=" empêche l'accumulation infinie
        transform.rotation = lookRotation * Quaternion.Euler(0, 0, currentBanking);

        // 4. Avancement
        transform.position += transform.forward * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, manager.transform.position) > manager.MaxDistance)
        {
            manager.ReturnBirdToPool(this);
        }
    }
}