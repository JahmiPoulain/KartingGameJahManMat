using UnityEngine;

public class BirdCorridor : MonoBehaviour
{
    Vector3 startPoint;
    [SerializeField] Transform endPoint;
    bool goingForward;
    float speed = 20f;
    void Start()
    {
        startPoint = transform.position;
    }

    void Update()
    {
        MoveBird();
    }

    private void OnEnable()
    {
        transform.position = startPoint;
    }

    void MoveBird()
    {
        Vector3 dir = endPoint.position - transform.position;
        float magnitude = dir.magnitude;
        transform.position += dir * (1 / magnitude) * speed * Time.deltaTime;
        if (magnitude < 1f)
        {
            transform.position = startPoint;
        }
    }
}
