using UnityEngine;
using System.Collections.Generic;

public class MinimapManager : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private Vector2 terrainOrigin; // Coin bas-gauche du terrain
    [SerializeField] private Vector2 terrainSize;   // Taille du terrain (X, Z)

    [Header("Minimap")]
    [SerializeField] private float mapSize = 250f;  // Taille UI de la minimap

    [Header("Objets à afficher")]
    [SerializeField] private List<MinimapItem> items = new List<MinimapItem>();

    void Start()
    {
        // Optionnel : auto-détection de tous les objets MinimapItem dans la scène
        if (items.Count == 0)
        {
            items.AddRange(FindObjectsByType<MinimapItem>(FindObjectsSortMode.None));
        }
    }

    void Update()
    {
        foreach (MinimapItem item in items)
        {
            UpdateItem(item);
        }
    }

    void UpdateItem(MinimapItem item)
    {
        Vector3 worldPos;

        // Choix position dynamique ou fixe
        if (item.follow)
            worldPos = item.transform.position;
        else
            worldPos = item.startPosition;

        // Normalisation (0 → 1)
        float normalizedX = (worldPos.x - terrainOrigin.x) / terrainSize.x;
        float normalizedZ = (worldPos.z - terrainOrigin.y) / terrainSize.y;

        // Conversion en coordonnées UI
        float x = normalizedX * mapSize;
        float y = normalizedZ * mapSize;

        // Recentrage (pivot au centre)
        x -= mapSize / 2f;
        y -= mapSize / 2f;

        // Appliquer la position
        item.icon.anchoredPosition = new Vector2(x, y);

        // Appliquer la rotation (optionnelle)
        if (item.rotateWithObject)
        {
            float rotationY = item.transform.eulerAngles.y;
            item.icon.localRotation = Quaternion.Euler(0f, 0f, -rotationY);
        }
    }
}