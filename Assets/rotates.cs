using System.Collections;
using UnityEngine;

public class rotatesAtPivot : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private Transform pivot;

    private IEnumerator Start()
    {
        rotationSpeed = rotationSpeed * Random.Range(0.5f, 1.5f);

        while (true)
        {
            if (pivot != null)
            {
                transform.RotateAround(pivot.position, Vector3.forward, rotationSpeed * Time.deltaTime);
            }
            yield return null;
        }
    }
}
