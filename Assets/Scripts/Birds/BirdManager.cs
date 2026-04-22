using UnityEngine;

public class BirdManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("References")]
    [SerializeField] private GameObject birdPrefab;
    [SerializeField] private Camera targetCamera;

    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 120f;
    [SerializeField] private float minAltitude = 25f;
    [SerializeField] private float maxAltitude = 45f;
    [SerializeField] private int maxSpawnAttempts = 30;

    [Header("Flight")]
    [SerializeField] private float minSpeed = 8f;
    [SerializeField] private float maxSpeed = 16f;
    [SerializeField] private float minFlightDuration = 5f;
    [SerializeField] private float maxFlightDuration = 12f;

    private readonly Plane[] cameraPlanes = new Plane[6];

    private GameObject birdInstance;
    private Renderer[] birdRenderers;
    private Vector3 flightDirection;
    private float speed;
    private float flightTimer;
    private Pcg32 rng;

    void Start()
    {
        rng = new Pcg32();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogWarning("BirdManager: aucune camera assignee ou taggee MainCamera.");
            enabled = false;
            return;
        }

        if (birdPrefab == null)
        {
            Debug.LogWarning("BirdManager: aucun birdPrefab assigne.");
            enabled = false;
            return;
        }

        birdInstance = Instantiate(birdPrefab);
        birdRenderers = birdInstance.GetComponentsInChildren<Renderer>();

        RespawnBirdOutsideCamera();
    }

    void Update()
    {
        if (birdInstance == null)
            return;

        flightTimer -= Time.deltaTime;
        birdInstance.transform.position += speed * Time.deltaTime * flightDirection;

        if (flightTimer <= 0f && !BirdIsVisible())
        {
            RespawnBirdOutsideCamera();
        }
    }

    private void RespawnBirdOutsideCamera()
    {
        Vector3 spawnPosition = GetSpawnPositionOutsideCamera();
        flightDirection = GetRandomHorizontalDirection();
        speed = RandomRange(minSpeed, maxSpeed);
        flightTimer = RandomRange(minFlightDuration, maxFlightDuration);

        birdInstance.transform.SetPositionAndRotation(spawnPosition,
            Quaternion.LookRotation(flightDirection, Vector3.up));
    }

    private Vector3 GetSpawnPositionOutsideCamera()
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector3 candidate = GetRandomSkyPosition();
            birdInstance.transform.position = candidate;

            if (!BirdIsVisible())
                return candidate;
        }

        return transform.position - targetCamera.transform.forward * spawnRadius + Vector3.up * RandomRange(minAltitude, maxAltitude);
    }

    private Vector3 GetRandomSkyPosition()
    {
        Vector2 circle = RandomPointInCircle();
        float altitude = RandomRange(minAltitude, maxAltitude);

        return transform.position + new Vector3(circle.x, altitude, circle.y);
    }

    private Vector3 GetRandomHorizontalDirection()
    {
        float angle = RandomRange(0f, Mathf.PI * 2f);
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
    }

    private Vector2 RandomPointInCircle()
    {
        float angle = RandomRange(0f, Mathf.PI * 2f);
        float distance = Mathf.Sqrt(rng.NextFloat()) * spawnRadius;

        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    private bool BirdIsVisible()
    {
        if (targetCamera == null || birdRenderers == null || birdRenderers.Length == 0)
            return false;

        GeometryUtility.CalculateFrustumPlanes(targetCamera, cameraPlanes);

        foreach (Renderer birdRenderer in birdRenderers)
        {
            if (birdRenderer != null && GeometryUtility.TestPlanesAABB(cameraPlanes, birdRenderer.bounds))
                return true;
        }

        return false;
    }

    private float RandomRange(float min, float max)
    {
        if (max <= min)
            return min;

        return min + rng.NextFloat() * (max - min);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 minCenter = transform.position + Vector3.up * minAltitude;
        Vector3 maxCenter = transform.position + Vector3.up * maxAltitude;

        Gizmos.color = new Color(0f, 0.7f, 1f, 0.35f);
        DrawCircle(minCenter, spawnRadius);
        DrawCircle(maxCenter, spawnRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        const int segments = 64;
        Vector3 previousPoint = center + Vector3.right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
