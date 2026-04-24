using UnityEngine;

public class PnjScript : MonoBehaviour
{
    // pnj qui se balade entre un nombre de points personnalisable, script modulaire et generique

    public Transform[] points; // Tableau de points entre lesquels le PNJ se déplacera
    
    public float speed = 2f; // Vitesse de déplacement du PNJ

    private int currentPointIndex = 0; // Index du point actuel vers lequel le PNJ se déplace

    void Update()
    {
        if (points.Length == 0) return; // Si aucun point n'est défini, ne rien faire

        // Se déplacer vers le point actuel
        Transform targetPoint = points[currentPointIndex];
        Vector3 direction = targetPoint.position - transform.position;
        transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);

        // Vérifier si le PNJ est proche du point cible
        if (Vector3.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            // Passer au point suivant
            currentPointIndex = (currentPointIndex + 1) % points.Length; // Boucle à travers les points
        }
    }


}
