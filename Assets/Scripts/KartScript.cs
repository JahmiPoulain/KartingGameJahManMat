using UnityEngine;

public class KartScript : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody rb;
    [Header("Speed")]
    public float maxSpeed;
    float currentSpeed;
    public float maxBackSpeed;
    [Header("Acceleration")]
    public bool accelerate;
    public float maxAccelSpeed;
    public float currentAccelSpeed;
    [Header("Deceleration")]
    bool decelerate;
    public float maxDecelSpeed;
    [Header("Turning")]
    public float maxTurnSpeed;
    public float currentTurnSpeed;
    public float turnAccelSpeed;
    public float turnDecelSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        HandleAcceleration();
        HandleDeceleration();
        HandleCurrentSpeed();
        HandleTurning();
        //HandleTurnAcceleration();
        rb.linearVelocity = transform.forward * currentSpeed;
        transform.Rotate(0, currentTurnSpeed, 0);
        //Debug.Log(currentSpeed);
    }

    void HandleCurrentSpeed()
    {
        float nextSpeed = currentSpeed + currentAccelSpeed * Time.fixedDeltaTime;
        if (nextSpeed < maxSpeed)
        {
            currentSpeed = nextSpeed;
        }
        else
        {
            currentSpeed = maxSpeed;
        }
        if (nextSpeed < -maxBackSpeed)
        {
            currentSpeed = -maxBackSpeed;
        }
    }
    void HandleAcceleration()
    {
        if (Input.GetAxisRaw("Vertical") > 0)
        {
            accelerate = true;
            decelerate = false;
        }
        else if (Input.GetAxisRaw("Vertical") < 0)
        {
            accelerate = false;
            decelerate = true;
        }
        else
        {
            accelerate = false;
            decelerate = false;
        }
            float nextAccelSpeed = 0;
        if (accelerate)
        {
            nextAccelSpeed = (maxSpeed - currentSpeed) / 2;
        }
        else
        {
            nextAccelSpeed = -(maxSpeed - currentSpeed) * 2f;
        }
        if (nextAccelSpeed < 0 && currentSpeed <= 0)
        {
            nextAccelSpeed = 0;
        }
        else if (nextAccelSpeed > maxAccelSpeed)
        {
            nextAccelSpeed = maxAccelSpeed;
        }
        currentAccelSpeed = nextAccelSpeed;
    }

    void HandleDeceleration()
    {
        if (decelerate)
        {
            currentAccelSpeed -= (maxSpeed - currentSpeed) * 2f;
            if (currentAccelSpeed < maxDecelSpeed)
            {
                currentAccelSpeed = maxDecelSpeed;
            }
        }
        else if (!accelerate && currentSpeed != 0)
        {
            if (currentSpeed < 0)
            {
                currentAccelSpeed += (maxSpeed + currentSpeed) * 2f;                                
            }
            //currentAccelSpeed = 0;
        }
        
    }


    void HandleTurning()
    {
        float directionMult = 0;
        if (currentSpeed == 0)
        {
            currentAccelSpeed = 0;
            DecelerateTurning();
            return;
        }
        if (currentSpeed > 0)
        {
            directionMult = 1f;

        }
        else if (currentSpeed < 0)
        {
            directionMult = -1f;
            
        }
        if (Input.GetAxisRaw("Horizontal") > 0)
        {
            currentTurnSpeed += directionMult * turnAccelSpeed * Time.fixedDeltaTime;
            Debug.Log("+");
            if (currentTurnSpeed > maxTurnSpeed)
            {
                currentTurnSpeed = maxTurnSpeed;
            }
        }
        else if (Input.GetAxisRaw("Horizontal") < 0)
        {
            currentTurnSpeed -= directionMult * turnAccelSpeed * Time.fixedDeltaTime;
            Debug.Log("-");
            if (currentTurnSpeed < -maxTurnSpeed)
            {
                currentTurnSpeed = -maxTurnSpeed;
            }
        }
        else
        {
            DecelerateTurning();
        }
        
        
            
    }

    void DecelerateTurning()
    {
        float nextTurnSpeed = currentTurnSpeed;
        if (currentTurnSpeed > 0)
        {
            nextTurnSpeed -= turnDecelSpeed * Time.fixedDeltaTime;
            if (nextTurnSpeed < 0)
            {
                //Debug.Log("<");
                nextTurnSpeed = 0;
            }
        }
        else if (currentTurnSpeed < 0)
        {
            nextTurnSpeed += turnDecelSpeed * Time.fixedDeltaTime;
            if (nextTurnSpeed > 0)
            {
                //Debug.Log(">");
                nextTurnSpeed = 0;
            }
        }
        currentTurnSpeed = nextTurnSpeed;
    }    
}
