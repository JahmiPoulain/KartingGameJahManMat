using UnityEngine;
using System.Collections.Generic;

public class MinimapManager : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private RectTransform minimapContainer;

    [Header("Terrain")]
    [SerializeField] private Vector2 terrainOrigin;
    [SerializeField] private Vector2 terrainSize;

    [Header("Map")]
    [SerializeField] private float mapSize = 250f;

    [Header("Objets à afficher")]
    [SerializeField] private List<MinimapItem> items = new();

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

        // Normalisation
        float normalizedX = (worldPos.x - terrainOrigin.x) / terrainSize.x;
        float normalizedZ = (worldPos.z - terrainOrigin.y) / terrainSize.y;

        // Conversion UI
        float x = normalizedX * mapSize;
        float y = normalizedZ * mapSize;

        // Recentrage
        x -= mapSize / 2f;
        y -= mapSize / 2f;

        item.icon.anchoredPosition = new Vector2(x, y);

        if (item.rotateWithObject)
        {
            float rotationY = item.transform.eulerAngles.y;
            item.icon.localRotation = Quaternion.Euler(0f, 0f, -rotationY);
        }
    }
}