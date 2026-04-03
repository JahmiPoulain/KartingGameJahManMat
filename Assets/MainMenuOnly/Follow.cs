using UnityEngine;

public class Follow : MonoBehaviour
{
    public GameObject gameObjectToFollow;
    void FixedUpdate()
    {
        transform.position = new Vector3(gameObjectToFollow.transform.position.x,transform.position.y,transform.position.z);
    }
}
