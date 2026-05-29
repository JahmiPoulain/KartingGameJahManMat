using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        transform.position += transform.forward;
    }
}
