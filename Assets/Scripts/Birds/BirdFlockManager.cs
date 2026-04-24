using UnityEngine;
using System.Collections.Generic;

public class BirdFlockManager : MonoBehaviour
{
    [Header("--- CONFIGURATION PRÉFAB ---")]
    [Tooltip("Le prefab de l'oiseau avec le script BirdIndividual")]
    [SerializeField] private BirdIndividual birdPrefab;
    [Tooltip("Nombre max d'oiseaux actifs en même temps (Optimisation)")]
    [SerializeField] private int poolSize = 150;

    [Header("--- PARAMÈTRES DES GROUPES (FLOCKS) ---")]
    [Range(1, 20)][SerializeField] private int minBirdsPerFlock = 3;
    [Range(1, 20)][SerializeField] private int maxBirdsPerFlock = 10;
    [Tooltip("Rayon de dispersion des oiseaux au sein d'un groupe")]
    [SerializeField] private float flockRadius = 10f;
    [Tooltip("Temps en secondes entre l'apparition de deux groupes")]
    [SerializeField] private float spawnInterval = 3f;

    [Header("--- ZONE DE VOL ---")]
    [Tooltip("Distance à laquelle les oiseaux apparaissent autour du manager")]
    [SerializeField] private float spawnDistance = 100f;
    [SerializeField] private float minAltitude = 15f;
    [SerializeField] private float maxAltitude = 40f;

    [Header("--- VARIATIONS INDIVIDUELLES ---")]
    [SerializeField] private float minSpeed = 7f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float minSize = 0.5f;
    [SerializeField] private float maxSize = 2.0f;

    public float MaxDistance => spawnDistance * 1.5f;

    private Stack<BirdIndividual> birdPool = new Stack<BirdIndividual>();
    private Pcg32 rng;
    private float nextSpawnTime;

    void Awake()
    {
        rng = new Pcg32();

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
            // Variation de l'intervalle via PCG32 pour plus de naturel
            float variation = 0.8f + (rng.NextFloat() * 0.4f);
            nextSpawnTime = Time.time + (spawnInterval * variation);
        }
    }

    private void SpawnNewFlock()
    {
        // Direction de vol aléatoire sur le plan horizontal
        float angle = rng.NextFloat() * Mathf.PI * 2f;
        Vector3 flyDir = new Vector3(Mathf.Cos(angle), (rng.NextFloat() - 0.5f) * 0.1f, Mathf.Sin(angle));

        // Position de spawn (cercle autour du manager)
        Vector3 spawnOrigin = transform.position - flyDir * spawnDistance;
        spawnOrigin.y += minAltitude + (rng.NextFloat() * (maxAltitude - minAltitude));
        // Plus besoin de cast, on utilise directement les int

        int count = rng.Range(minBirdsPerFlock, maxBirdsPerFlock);

        for (int i = 0; i < count; i++)
        {
            if (birdPool.Count > 0)
            {
                BirdIndividual bird = birdPool.Pop();

                // Décalage aléatoire dans le groupe
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

    // Visualisation dans l'éditeur
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * minAltitude, 5f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * maxAltitude, 5f);
    }
}