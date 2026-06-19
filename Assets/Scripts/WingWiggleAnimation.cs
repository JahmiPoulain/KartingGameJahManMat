using UnityEngine;

public class WingWiggleAnimation : MonoBehaviour
{
   // [SerializeField] bool rightWing;
    [SerializeField] float speed;
    bool goUp;
    float maxOffset = 0.05f;
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
        //speed = KartScriptV2.instance.flightSpeed * 6f;
            if (goUp)
            {
                accel += speed * Time.deltaTime;
                if (offset > maxOffset)
                {
                    goUp = false;
                    //rngOffsetTarget = Random.Range(0.3f, maxOffset);
                }
            }
            else
            {
                accel -= speed * Time.deltaTime;
                // offset -= speed * Time.deltaTime;
                if (offset < -maxOffset)
                {
                    goUp = true;
                    //rngOffsetTarget = Random.Range(0.3f, maxOffset);
                }
            }
        offset += (accel) * Time.deltaTime;
        leftWing.localEulerAngles = new Vector3(0, 0, -offset);
        rightWing.localEulerAngles = new Vector3( 0, 0, offset);
        //offset = Mathf.PingPong(Time.time * 80f, maxOffset);
       // transform.localEulerAngles = new Vector3( +offset, -90, 90);
    }
}
