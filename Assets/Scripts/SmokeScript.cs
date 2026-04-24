using UnityEngine;

public class SmokeSccript : MonoBehaviour
{    
    void Start()
    {
        
    }

    void Update()
    {
        transform.localScale += Vector3.one * 10f * Time.deltaTime;
        if (transform.localScale.x > 3) Destroy(gameObject);
    }
}
