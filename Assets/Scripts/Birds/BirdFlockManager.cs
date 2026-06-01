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

    public float MaxDistance => spawnDistance * 1.5f;
    public Vector3 CameraPosition => playerCamera != null ? playerCamera.transform.position : Vector3.zero;

    private Stack<BirdIndividual> birdPool = new Stack<BirdIndividual>();
    private float nextSpawnTime;

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main; // Récupère la caméra si on a oublié de l'assigner

        if (birdPrefab == null)
        {
            Debug.LogError("BirdFlockManager: N'oublie pas d'assigner le Prefab de l'oiseau !");
            enabled = false;
            return;
        }

        // Création de la réserve (Pool) d'oiseaux au lancement
        for (int i = 0; i < poolSize; i++)
        {
            // Range les clones sous le Manager dans la hiérarchie pour que ça reste propre
            BirdIndividual bird = Instantiate(birdPrefab, this.transform);
            bird.gameObject.SetActive(false);
            birdPool.Push(bird);
        }
    }

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnNewFlock();
            float variation = 0.8f + (Random.value * 0.4f);
            nextSpawnTime = Time.time + (spawnInterval * variation);
        }
    }

    private void SpawnNewFlock()
    {
        Vector3 spawnOrigin = Vector3.zero;
        Vector3 flyDir = Vector3.forward;
        bool validSpawn = false;
        int attempts = 0;

        // On fait plusieurs essais (max 10) pour trouver un point hors du champ de vision
        while (!validSpawn && attempts < 10)
        {
            float angle = Random.value * Mathf.PI * 2f;
            flyDir = new Vector3(Mathf.Cos(angle), (Random.value - 0.5f) * 0.1f, Mathf.Sin(angle));

            spawnOrigin = playerCamera.transform.position - (flyDir * spawnDistance);
            spawnOrigin.y = playerCamera.transform.position.y + minAltitude + (Random.value * (maxAltitude - minAltitude));

            Vector3 viewportPoint = playerCamera.WorldToViewportPoint(spawnOrigin);

            // Vérifie si c'est visible à l'écran
            bool isVisible = viewportPoint.z > 0 && viewportPoint.x > -0.2f && viewportPoint.x < 1.2f && viewportPoint.y > -0.2f && viewportPoint.y < 1.2f;

            if (!isVisible) validSpawn = true;

            attempts++;
        }

        // +1 car le max est exclusif avec les entiers
        int count = Random.Range(minBirdsPerFlock, maxBirdsPerFlock + 1);

        for (int i = 0; i < count; i++)
        {
            if (birdPool.Count > 0)
            {
                BirdIndividual bird = birdPool.Pop();

                Vector3 offset = new Vector3(
                    (Random.value - 0.5f) * flockRadius,
                    (Random.value - 0.5f) * flockRadius,
                    (Random.value - 0.5f) * flockRadius
                );

                float s = Mathf.Lerp(minSpeed, maxSpeed, Random.value);
                float sz = Mathf.Lerp(minSize, maxSize, Random.value);

                bird.gameObject.SetActive(true);
                bird.Initialize(this, spawnOrigin + offset, flyDir, s, sz);
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