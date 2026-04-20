using UnityEngine;

public class MinimapUI : MonoBehaviour
{
    public Transform player;          // Le joueur (objet 3D)
    public RectTransform playerIcon;  // Le cercle dans la minimap (UI)

    public float mapSize = 250f;      // Taille de la minimap en pixels
    public float terrainSize = 400f;  // Taille du terrain (40 * 10)

    void Update()
    {
        Vector3 pos = player.position;

        // Normaliser la position (entre 0 et 1)
        float normalizedX = (pos.x + terrainSize / 2f) / terrainSize;
        float normalizedZ = (pos.z + terrainSize / 2f) / terrainSize;

        // Convertir en position UI
        float x = normalizedX * mapSize;
        float y = normalizedZ * mapSize;

        // Recentrer (pivot au centre)
        x -= mapSize / 2f;
        y -= mapSize / 2f;

        // Appliquer à l'UI
        playerIcon.anchoredPosition = new Vector2(x, y);
    }
}