using UnityEngine;

public class Follow : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject gameObjectToFollow;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(gameObjectToFollow.transform.position.x,transform.position.y,transform.position.z);
    }
}
