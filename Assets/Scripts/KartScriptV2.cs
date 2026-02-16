
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
    [Header("Turbo")]
    public float currentTurboForce;
    public float turboAccelSpeed;
    float targetTurboForce;
    public float minTurboDecel;
    bool turbo;    
    [Header("Colisions")]
    public LayerMask wallLayer;
    public Vector3 bounceDirection;
    public float bounceForce;
    public float minBounceDecelForce;
    [Header("Camera")]
    public float camXpos;
    public GameObject playerCamera;
    float turboTimer;
    [Header("Visual Kart")]
    public GameObject visualKartBody;
    public float visKartZRot;
    public float visKartXRot;
    public float visKartXRotCatchUp;
    public float visKartTurboXRotCatchUp;
    public GameObject[] turningWheels;
    public GameObject[] nonTurningWheels;
    public float visWheelsYRot;
    float turningWheelsXRot;
    float nonTurningWheelsXRot;
    public float turningWheelsRatioScaling;
    public float nonTurningWheelsRatioScaling;
    [Header("Bounce Animation")]
    public bool bounce;
    float bounceTimer;
    [Header("Smoke")]
    public GameObject smokePrefab;
    public Transform smokeOrigin;
    public Material baseSmokeMat;
    public Material fireSmokeMat;
    float smokeTimer;
    public ParticleSystem smokeParticlesGenerator;
    public ParticleSystem fireParticlesGenerator;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        PlayerInputs();
        if (Input.GetMouseButtonDown(1))
        {
            StartTurbo(10f, 1.5f);
        }
    }

    private void FixedUpdate()
    {
        
        HandleCurrentSpeed();
        HandleTurning();
        HandleTurbo();
        float unsignedCurSpeed = currentSpeed;
        if (unsignedCurSpeed < 0)
        {
            unsignedCurSpeed = -unsignedCurSpeed;
        }
        float nextBounceForce = bounceForce - (minBounceDecelForce) * Time.fixedDeltaTime;
        if (nextBounceForce > 0)
        {
            bounceForce = nextBounceForce;
        }
        else
        {
            bounceForce = 0;
            bounceDirection = Vector3.zero;
        }
        rb.linearVelocity = transform.forward * (currentSpeed + currentTurboForce) + bounceDirection * bounceForce;
        transform.Rotate(0, currentTurnSpeed, 0);
        HandleVisualKartBody();
        HandleVisualKartWheels();
        HandleCameraLocalPosition();
        SquishAnimation();
        HandleSmoke();

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

    public void StartTurbo(float force ,float time)
    {
        turbo = true;
        turboTimer += time;
        float nextTForce = targetTurboForce + force;
        if (nextTForce > targetTurboForce)
        {
            targetTurboForce = nextTForce;
        }
        Debug.Log("turbo");
    }
    void HandleTurbo()
    {
        if (turbo)
        {
            turboTimer -= Time.fixedDeltaTime;
            if (turboTimer > 0)
            {
                float nextTForce = currentTurboForce + turboAccelSpeed * Time.fixedDeltaTime;
                if (nextTForce < targetTurboForce)
                {
                    currentTurboForce = nextTForce;
                }
                else
                {
                    currentTurboForce = targetTurboForce;
                }
            }
            else
            {
                targetTurboForce = 0;
                currentTurboForce -= minTurboDecel * Time.fixedDeltaTime;
            }
            if (currentTurboForce <= 0)
            {
                turbo = false;
                currentTurboForce = 0;
                targetTurboForce = 0;
            }                       
        }
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
        visualKartBody.transform.localRotation = Quaternion.Euler(visKartXRot + -currentTurboForce * 2, 0, visKartZRot);
    }

    void HandleVisualKartWheels()
    {
        //visWheelsYRot = currentTurnSpeed * 12;
        turningWheelsXRot += (currentSpeed + currentTurboForce) * turningWheelsRatioScaling * Time.fixedDeltaTime;
        nonTurningWheelsXRot += (currentSpeed + currentTurboForce) * nonTurningWheelsRatioScaling * Time.fixedDeltaTime;
        for (int i = 0; i < turningWheels.Length; i++)
        {
           
            turningWheels[i].transform.localRotation = Quaternion.Euler(turningWheelsXRot, visWheelsYRot, 90);
            //turningWheels[i].transform.Rotate(0, 10, 0);
        }
        for (int i = 0; i < nonTurningWheels.Length; i++)
        {
            nonTurningWheels[i].transform.localRotation = Quaternion.Euler(nonTurningWheelsXRot, 0, 90);
            //turningWheels[i].transform.Rotate(0, 10, 0);
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

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log(collision.gameObject.layer);
        if (collision.gameObject.layer == 6)
        { 
            bounce = true;
            Vector3 rawDir = transform.position - collision.contacts[0].point;
            bounceDirection = new Vector3(rawDir.x, 0, rawDir.z).normalized;
            float unsignedCurSpeed = currentSpeed;
            if (unsignedCurSpeed < 0)
            {
                unsignedCurSpeed = -unsignedCurSpeed;
            }
            bounceForce = unsignedCurSpeed * 2f;
            currentSpeed *= 0.2f;
            //rb.AddForce((, ForceMode.Impulse);
        }
    }
    void SquishAnimation()
    {
        if (bounce)
        {
            
            bounceTimer += Time.deltaTime;
            if (bounceTimer < 0.05f)
            {
                visualKartBody.transform.localScale += new Vector3(-6f, 10f, -6f) * Time.deltaTime;
                
            }
            else if (bounceTimer < 0.1f)
            {
                visualKartBody.transform.localScale += new Vector3(6f, -10f, 6f) * Time.deltaTime;
            }
            else
            {
                visualKartBody.transform.localScale = Vector3.one;
                bounce = false;                
                bounceTimer = 0;
            }
        }
    }

    void HandleSmoke()
    {
        if (turbo) {
            //var Pemission = smokeParticlesGenerator.emission;
            //Pemission.rateOverTime = 0;
            var FPemission = fireParticlesGenerator.emission;
            FPemission.rateOverDistance = 3;
            //FPemission.rateOverTime =3;
        }
        else
        {
            //var Pemission = smokeParticlesGenerator.emission;
            //Pemission.rateOverTime = 3;
            var FPemission = fireParticlesGenerator.emission;
            //FPemission.rateOverTime = 0;
            FPemission.rateOverDistance = 0;

        }
        /*smokeTimer -= (currentSpeed + currentTurboForce * 3f) * Time.fixedDeltaTime;
        if (smokeTimer < 0)
        {
            
            GameObject smoke = Instantiate(smokePrefab, smokeOrigin.position, Quaternion.identity);
            if (currentTurboForce > 0)
            {
                smoke.GetComponent<Renderer>().material = fireSmokeMat;
            }
            smokeTimer = 2f;
        }*/
    }
}
