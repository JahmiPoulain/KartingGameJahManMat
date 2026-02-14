using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class KartScriptV2 : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody rb;
    [Header("Inputs")]
    float forwardDirection;
    float turnDirection; // la direction de la rotation du vollant
    [Header("Speed")]
    public float maxSpeed;
    public float currentSpeed;
    public float maxBackSpeed;
    [Header("Acceleration")]
    public bool accelerate;
    public float accelSpeed;
    public float currentAccelSpeed;
    [Header("Deceleration")]
    public float decelSpeed;
    [Header("Turning")]
    public float maxTurnSpeed;
    public float currentTurnSpeed; // c'est l'equivalent de la rotation du vollant
    public float turnAccelSpeed;
    public float turnDecelSpeed;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {

    }

    private void FixedUpdate()
    {
        PlayerInputs();
        HandleCurrentSpeed();
        HandleTurning();
        rb.linearVelocity = transform.forward * currentSpeed;
        transform.Rotate(0, currentTurnSpeed, 0);
        //Debug.Log(currentSpeed);
    }

    void PlayerInputs()
    {
        forwardDirection = Input.GetAxisRaw("Vertical");
        turnDirection = Input.GetAxisRaw("Horizontal");
    }

    void HandleCurrentSpeed()
    {
        float nextSpeed = currentSpeed;
        if (forwardDirection > 0) // si on veut accelerer en avant
        {
            nextSpeed += 1f + (maxSpeed - currentSpeed) * accelSpeed * Time.fixedDeltaTime;            
        }
        else if (forwardDirection < 0) // si on veut accelerer en arière
        {
            nextSpeed -= 1f + (maxSpeed - currentSpeed) * accelSpeed * Time.fixedDeltaTime;
        }
        else // si on ne veut pas accelerer
        {           
            if (currentSpeed > 0) // si on avance
            {
                nextSpeed -= 1f + (maxSpeed - currentSpeed) * decelSpeed * Time.fixedDeltaTime;                
                if (nextSpeed < 0)
                {
                    nextSpeed = 0;
                }
            }
            else if (currentSpeed < 0) // si on  recule
            {
                nextSpeed += 1f + (maxSpeed - currentSpeed) * decelSpeed * Time.fixedDeltaTime;
                if (nextSpeed > 0)
                {
                    nextSpeed = 0;
                }
            }
        }
        
        // on clamp
        if (nextSpeed > maxSpeed)
        {
            nextSpeed = maxSpeed;
        }
        else if (nextSpeed < -maxBackSpeed)
        {
            nextSpeed = -maxBackSpeed;
        }

        currentSpeed = nextSpeed;
    }   

    void HandleTurning()
    {
        float nextTurnSpeed = currentTurnSpeed;

        if (currentSpeed > 0) // si on avance
        {
            nextTurnSpeed += turnDirection * turnAccelSpeed * Time.fixedDeltaTime;
        }
        else if (currentSpeed < 0) // si on recule
        {
            nextTurnSpeed += -turnDirection * turnAccelSpeed * Time.fixedDeltaTime;
        }        

        if (turnDirection == 0 || currentSpeed == 0) // turn deceleration
        {
            if (currentTurnSpeed > 0)
            {
                nextTurnSpeed -= turnDecelSpeed * Time.fixedDeltaTime;
                if (nextTurnSpeed < 0)
                {
                    nextTurnSpeed = 0;
                }
            }
            else if (currentTurnSpeed < 0)
            {
                nextTurnSpeed += turnDecelSpeed * Time.fixedDeltaTime;
                if (nextTurnSpeed > 0)
                {
                    nextTurnSpeed = 0;
                }
            }
        }

        // on clamp
        if (nextTurnSpeed > maxTurnSpeed)
        {
            nextTurnSpeed = maxTurnSpeed;
        }
        else if (nextTurnSpeed < -maxTurnSpeed)
        {
            nextTurnSpeed = -maxTurnSpeed; 
        }

        // on applique
        currentTurnSpeed = nextTurnSpeed;
    }
}
