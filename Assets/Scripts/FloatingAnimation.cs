using UnityEngine;

public class FloatingAnimation : MonoBehaviour
{
    [SerializeField] float maxSpeed;
    float startLocalY;
    float currentSpeed;
    bool uP;
    private void Start()
    {
        startLocalY = transform.localPosition.z;
        currentSpeed = -maxSpeed;// * 0.5f;
    }
    void Update()
    {
        if (uP)
        {
            currentSpeed += 0.0035f * Time.deltaTime;
            if (currentSpeed >= maxSpeed)
            {
                uP = false;
            }
        }
        else
        {
            currentSpeed -= 0.0035f * Time.deltaTime;
            if (currentSpeed <= -maxSpeed)
            {
                uP = true;
            }
        }
        transform.localPosition += new Vector3(0, 0, currentSpeed * 0.25f);
        if (transform.localPosition.z > 0.05f) transform.localPosition = new Vector3(0, 0, 0.05f);
        else if (transform.localPosition.z < -0.05f) transform.localPosition = new Vector3(0, 0, -0.05f);
    }
}
