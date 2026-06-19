using UnityEngine;

public class WingWiggleAnimation : MonoBehaviour
{
   // [SerializeField] bool rightWing;
    [SerializeField] float speed;
    bool goUp;
    float maxOffset = 2f;
    float rngOffsetTarget;
    float offset;
    [SerializeField] Transform rightWing;
    [SerializeField] Transform leftWing;
    float accel;

    void Start()
    {
        //rngOffsetTarget = Random.Range(0.3f, maxOffset);
    }

    void Update()
    {
        WiggleWing();
    }

    void WiggleWing()
    {
        if (transform.localScale.x <= 0)
        {
            accel = 0;
            offset = 0;
        }
        if (goUp)
        {
            offset += (speed) * Time.deltaTime;
            if (offset > maxOffset)
            {
                goUp = false;
            }
        }
        else
        {
            offset -= (speed) * Time.deltaTime;
            if (offset < -maxOffset)
            {
                goUp = true;               
            }
        }

        accel += offset * 40f * Time.deltaTime;
        leftWing.localEulerAngles = new Vector3(0, 0, -accel - 20);
        rightWing.localEulerAngles = new Vector3( 0, 0, accel + 20);
        Debug.Log(offset);
    }
}
