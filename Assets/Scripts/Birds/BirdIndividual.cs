using UnityEngine;

public class BirdIndividual : MonoBehaviour
{
    private float speed;
    private float noiseOffset;
    private Vector3 direction;
    private BirdFlockManager manager;
    private float internalTimer;
    private float currentBanking;
    private Quaternion lookRotation;

    public void Initialize(BirdFlockManager manager, Vector3 startPos, Vector3 dir, float speed, float scale)
    {
        this.manager = manager;
        this.direction = dir.normalized;
        this.speed = speed;
        this.transform.position = startPos;
        this.transform.localScale = Vector3.one * scale;

        this.noiseOffset = Random.value * 1000f; // Utilise le random d'Unity
        this.internalTimer = 0f;
        this.currentBanking = 0f;

        this.lookRotation = Quaternion.LookRotation(direction);
    }

    void Update()
    {
        internalTimer += Time.deltaTime;

        // 1. Direction organique
        float nX = Mathf.PerlinNoise(internalTimer * 0.4f, noiseOffset) - 0.5f;
        float nY = Mathf.PerlinNoise(noiseOffset, internalTimer * 0.4f) - 0.5f;
        Vector3 noiseVec = new Vector3(nX, nY, 0); // Syntaxe corrigée
        Vector3 finalDir = (direction + transform.TransformDirection(noiseVec)).normalized;

        // 2. Calcul du Banking
        float angleDiff = Vector3.SignedAngle(transform.forward, finalDir, Vector3.up);
        currentBanking = Mathf.Lerp(currentBanking, -angleDiff * 3.0f, Time.deltaTime * 3f);

        // 3. Rotation
        if (finalDir != Vector3.zero)
        {
            Quaternion targetLook = Quaternion.LookRotation(finalDir, Vector3.up);
            lookRotation = Quaternion.Slerp(lookRotation, targetLook, Time.deltaTime * 2.5f);
        }

        transform.rotation = lookRotation * Quaternion.Euler(0, 0, currentBanking);

        // 4. Avancement
        transform.position += speed * Time.deltaTime * transform.forward;

        // 5. Désactivation : basé sur la caméra
        if (Vector3.Distance(transform.position, manager.CameraPosition) > manager.MaxDistance)
        {
            manager.ReturnBirdToPool(this);
        }
    }
}