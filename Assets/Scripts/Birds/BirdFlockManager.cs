using UnityEngine;
using System.Collections.Generic;

public class BirdFlockManager : MonoBehaviour
{
    [Header("--- CONFIGURATION PRÉFAB ---")]
    [Tooltip("Le prefab de l'oiseau avec le script BirdIndividual")]
    [SerializeField] private BirdIndividual birdPrefab;
    [Tooltip("Nombre max d'oiseaux actifs en même temps (Optimisation)")]
    [SerializeField] private int poolSize = 150;

    [Header("--- RÉFÉRENCES ---")]
    [Tooltip("La caméra principale du joueur. Laisse vide pour utiliser Camera.main")]
    [SerializeField] private Camera playerCamera;

    [Header("--- PARAMÈTRES DES GROUPES (FLOCKS) ---")]
    [Range(1, 20)][SerializeField] private int minBirdsPerFlock = 3;
    [Range(1, 20)][SerializeField] private int maxBirdsPerFlock = 10;
    [Tooltip("Rayon de dispersion des oiseaux au sein d'un groupe")]
    [SerializeField] private float flockRadius = 10f;
    [Tooltip("Temps en secondes entre l'apparition de deux groupes")]
    [SerializeField] private float spawnInterval = 3f;

    [Header("--- ZONE DE VOL ---")]
    [Tooltip("Distance de base pour l'apparition par rapport à la caméra")]
    [SerializeField] private float spawnDistance = 80f;
    [SerializeField] private float minAltitude = 15f;
    [SerializeField] private float maxAltitude = 40f;

    [Header("--- VARIATIONS INDIVIDUELLES ---")]
    [SerializeField] private float minSpeed = 7f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float minSize = 0.5f;
    [SerializeField] private float maxSize = 2.0f;

    // On augmente un peu la distance max pour laisser le temps à l'oiseau de sortir de l'écran avant de despawn
    public float MaxDistance => spawnDistance * 1.5f;

    // Propriété publique pour que les oiseaux connaissent la position de la caméra
    public Vector3 CameraPosition => playerCamera != null ? playerCamera.transform.position : Vector3.zero;

    private Stack<BirdIndividual> birdPool = new Stack<BirdIndividual>();
    private Pcg32 rng;
    private float nextSpawnTime;

    void Awake()
    {
        rng = new Pcg32();

        if (playerCamera == null)
        {
            playerCamera = Camera.main; // Récupère la caméra si on a oublié de l'assigner
        }

        if (birdPrefab == null)
        {
            Debug.LogError("BirdFlockManager: Oublie pas d'assigner le Prefab !");
            enabled = false;
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            BirdIndividual bird = Instantiate(birdPrefab);
            bird.gameObject.SetActive(false);
            birdPool.Push(bird);
        }
    }

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnNewFlock();
            float variation = 0.8f + (rng.NextFloat() * 0.4f);
            nextSpawnTime = Time.time + (spawnInterval * variation);
        }
    }

    private void SpawnNewFlock()
    {
        Vector3 spawnOrigin = Vector3.zero;
        Vector3 flyDir = Vector3.forward;
        bool validSpawn = false;
        int attempts = 0;

        // On fait plusieurs essais (max 10) pour trouver un point hors du champ de vision de la caméra
        while (!validSpawn && attempts < 10)
        {
            // Direction de vol aléatoire
            float angle = rng.NextFloat() * Mathf.PI * 2f;
            flyDir = new Vector3(Mathf.Cos(angle), (rng.NextFloat() - 0.5f) * 0.1f, Mathf.Sin(angle));

            // On spawn autour de la CAMÉRA, et en face de la direction de vol pour qu'ils volent VERS/AU-DESSUS de la zone du joueur
            spawnOrigin = playerCamera.transform.position - (flyDir * spawnDistance);
            spawnOrigin.y = playerCamera.transform.position.y + minAltitude + (rng.NextFloat() * (maxAltitude - minAltitude));

            // INTELLIGENCE : Vérifier si le point est dans l'écran
            Vector3 viewportPoint = playerCamera.WorldToViewportPoint(spawnOrigin);

            // Si x et y sont entre 0 et 1, ET z > 0, c'est que c'est visible à l'écran.
            // On élargit un peu (-0.2 à 1.2) pour être sûr qu'ils spawnent vraiment bien au-delà des bords.
            bool isVisible = viewportPoint.z > 0 && viewportPoint.x > -0.2f && viewportPoint.x < 1.2f && viewportPoint.y > -0.2f && viewportPoint.y < 1.2f;

            if (!isVisible)
            {
                validSpawn = true; // C'est bon, le joueur ne le verra pas popper !
            }

            attempts++;
        }

        int count = rng.Range(minBirdsPerFlock, maxBirdsPerFlock);

        for (int i = 0; i < count; i++)
        {
            if (birdPool.Count > 0)
            {
                BirdIndividual bird = birdPool.Pop();

                Vector3 offset = new Vector3(
                    (rng.NextFloat() - 0.5f) * flockRadius,
                    (rng.NextFloat() - 0.5f) * flockRadius,
                    (rng.NextFloat() - 0.5f) * flockRadius
                );

                float s = minSpeed + (rng.NextFloat() * (maxSpeed - minSpeed));
                float sz = minSize + (rng.NextFloat() * (maxSize - minSize));

                bird.gameObject.SetActive(true);
                bird.Initialize(this, rng, spawnOrigin + offset, flyDir, s, sz);
            }
        }
    }

    public void ReturnBirdToPool(BirdIndividual bird)
    {
        bird.gameObject.SetActive(false);
        birdPool.Push(bird);
    }

    private void OnDrawGizmosSelected()
    {
        Camera cam = playerCamera != null ? playerCamera : Camera.main;

        if (cam == null) return;

        Vector3 center = cam.transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, spawnDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, MaxDistance);


        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center + Vector3.up * minAltitude, 5f);
        Gizmos.DrawWireSphere(center + Vector3.up * maxAltitude, 5f);

        Gizmos.DrawLine(center + Vector3.up * minAltitude, center + Vector3.up * maxAltitude);
    }
}