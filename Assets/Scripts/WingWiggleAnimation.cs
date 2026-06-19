using UnityEngine;

public class WingWiggleAnimation : MonoBehaviour
{
    [SerializeField] bool rightWing;
    bool goUp;
    float maxOffset = 10f;
    float offset;
    void Start()
    {
        
    }

    void Update()
    {
        WiggleWing();
    }

    void WiggleWing()
    {
        if (rightWing)
        {
            
        }
        offset = Mathf.PingPong(Time.time, maxOffset);
        transform.localEulerAngles = new Vector3( -90f + offset, 0, 0);
    }
}
