
using System.Linq;
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
    //public float currentAccelSpeed;
    [Header("Deceleration")]
    public float decelSpeed;
    [Header("Turning")]
    public float maxTurnSpeed;
    public float currentTurnSpeed; // c'est l'equivalent de la rotation du vollant
    public float turnAccelSpeed;
    public float turnDecelSpeed;
    [Header("Visual Kart")]
    public GameObject visualKart;
    public float visKartZRot;
    public float visKartXRot;
    public float visKartXRotCatchUp;
    public GameObject[] turningWheels;
    public float visWheelsYRot;
    [Header("Camera")]
    public float camXpos;
    public GameObject playerCamera;

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
        HandleVisualKartBody();
        HandleVisualKartWheels();
        HandleCameraLocalPosition();
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
            visWheelsYRot = currentTurnSpeed * 16;
        }
        else if (currentSpeed < 0) // si on recule
        {
            nextTurnSpeed += -turnDirection * turnAccelSpeed * Time.fixedDeltaTime;
            visWheelsYRot = -currentTurnSpeed * 16;
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

    void HandleVisualKartBody()
    {
        
        
        if (currentSpeed > 0)
        {            
            if (forwardDirection > 0)
            { 
                visKartXRotCatchUp = (maxSpeed - currentSpeed) / 4;
            }
            else
            {
                visKartXRotCatchUp = -(maxSpeed - currentSpeed) / 4;
            }
        }
        else if (currentSpeed < 0)
        {            
            if (forwardDirection < 0)
            {
                visKartXRotCatchUp = (maxSpeed + currentSpeed) / 8;
            }
            else
            {
                visKartXRotCatchUp = -(maxSpeed + currentSpeed) / 8;
            }            
        }
        else
        {            
            visKartXRotCatchUp = 0;            
        }
        visKartXRot = (-currentSpeed / 2) * visKartXRotCatchUp;
        visKartZRot = currentTurnSpeed * (currentSpeed / 3);
        visualKart.transform.localRotation = Quaternion.Euler(visKartXRot, 0, visKartZRot);
    }

    void HandleVisualKartWheels()
    {
        //visWheelsYRot = currentTurnSpeed * 12;
        for (int i = 0; i < turningWheels.Length; i++)
        {
            turningWheels[i].transform.localRotation = Quaternion.Euler(90, visWheelsYRot, 90);
        }
    }

    void HandleCameraLocalPosition()
    {
        camXpos = currentSpeed * -currentTurnSpeed / 120f * forwardDirection;
        //+ -currentTurnSpeed;
        /*if (playerCamera.transform.localRotation.y > camXpos)
        {
            playerCamera.transform.Rotate(0, -1f * Time.fixedDeltaTime, 0);
        }
        else if (playerCamera.transform.localRotation.y < camXpos)
        {
            playerCamera.transform.Rotate(0, 1f * Time.fixedDeltaTime, 0);
        }*/
        if (playerCamera.transform.localPosition.x > camXpos)
        {
            float nextXpos = playerCamera.transform.localPosition.x - 1f * Time.fixedDeltaTime;
            if (nextXpos < camXpos)
            {
                nextXpos = camXpos;
            }
            playerCamera.transform.localPosition = new Vector3(nextXpos, 3.9f, -4.7f);
        }
        else if (playerCamera.transform.localPosition.x < camXpos)
        {
            float nextXpos = playerCamera.transform.localPosition.x + 1f * Time.fixedDeltaTime;
            if ( nextXpos > camXpos)
            {
                nextXpos = camXpos;
            }
            playerCamera.transform.localPosition = new Vector3(nextXpos, 3.9f, -4.7f);
        }
        // playerCamera.transform.localRotation = Quaternion.Euler(32, camYRot, 0);
        //playerCamera.transform.localPosition = new Vector3(camXpos, 3.9f, -4.7f);
    }
}
