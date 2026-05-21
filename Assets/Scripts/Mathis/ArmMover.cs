using UnityEngine;

public class ArmMover : MonoBehaviour
{
    [SerializeField] private Transform target;  // La destination 
    public float baseLength = 1f;    // La longueur naturelle de ton bras 
    public bool updateScale = false;  // Est-ce que le bras doit s'étirer ?

    void Update()
    {
        if (target == null) return;

        // 1. Calculer la direction
        Vector3 direction = target.position - transform.position;

        // 2. Orientation (LookAt)
        transform.rotation = Quaternion.LookRotation(direction);

        // 3. Ajustement du Scale (Étirage)
        if (updateScale)
        {
            float currentDistance = direction.magnitude;

            // On calcule le ratio : distance actuelle / longueur de base
            float scaleMultiplier = currentDistance / baseLength;

            // On applique le scale uniquement sur l'axe Z (profondeur/longueur)
            transform.localScale = new Vector3(0.1072f, 0.1384f, scaleMultiplier);
        }
    }
}