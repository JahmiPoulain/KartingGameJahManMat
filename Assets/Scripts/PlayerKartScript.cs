using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerKartScript : MonoBehaviour
{
    //public static KartScriptV2 instance;
    [Header("Components")]
    public Rigidbody rb;
    [Header("Inputs")]
    float forwardDirection;
    float turnDirection; // la direction de la rotation du vollant
    InputSystem_Actions controls;
    [Header("Speed")]
    public float maxSpeed;
    public float currentSpeed;
    public float maxBackSpeed;
    [Header("Acceleration")]
    public bool accelerate;
    public float accelSpeed;
    public float flatAccelSpeed;
    //public float currentAccelSpeed;
    [Header("Deceleration")]
    public float decelSpeed;
    public float flatDecelSpeed;
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
    public GameObject visualKartWheelsParent;
    public float visKartZRot;
    public float visKartXRot;
    public float visKartXRotCatchUp;
    float visKartXRotCatchUpBis;
    public float visKartTurboXRotCatchUp;
    public GameObject[] turningWheels;
    public GameObject[] nonTurningWheels;
    public GameObject[] fireWheelEffects;
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
    public ParticleSystem[] driftParticlesGenerators;
    [Header("Gravity")]
    public float gravity;
    public float currentFallSpeed;
    int minimalGrav;
    Vector3 groundNormal;
    public Transform preOrientation;
    public Transform groundRayOrigin;
    bool grounded;
    [Header("Drift")]
    bool tryToDrift;
    public bool keepDrifting;
    bool oldKeepD;
    float currentDriftForce;
    float driftCatchUp;
    int driftDir;
    public float highDrift;
    public float lowDrift;
    float driftTurboGauge;
    public float gaugeToActivateTurbo;
    public Transform driftPivot;
    float nextYDriftRot;
    private void Awake()
    {
   
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        PlayerInputs();
        HandleDrift();
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
        if (grounded)
        {
            currentFallSpeed = 0;
        }
        else
        {
            currentFallSpeed += gravity * Time.fixedDeltaTime;
        }
        rb.linearVelocity = (transform.forward * (currentSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + currentFallSpeed);
        transform.Rotate(0, currentTurnSpeed + currentDriftForce, 0);
    }

    private void LateUpdate()
    {
        HandleWholeKartRotationXZ();
        HandleVisualKartBody();
        HandleVisualKartWheels();

        SquishAnimation();
        HandleSmoke();
        HandleCameraLocalPosition();
    }

    void PlayerInputs()
    {
        /*forwardDirection = Input.GetAxisRaw("Vertical");
        //turnDirection = Input.GetAxisRaw("Horizontal");
        tryToDrift = Input.GetKeyDown("space");
        keepDrifting = Input.GetKey("space");*/
        forwardDirection = InputSystemHandler.instance.inputForwardDir;
        turnDirection = InputSystemHandler.instance.inputTurnDir;
        keepDrifting = InputSystemHandler.instance.inputDrift;
    }
    void HandleCurrentSpeed()
    {
        float nextSpeed = currentSpeed;
        if (forwardDirection > 0) // si on veut accelerer en avant
        {
            nextSpeed += flatAccelSpeed + (maxSpeed - currentSpeed) * accelSpeed * Time.fixedDeltaTime;
        }
        else if (forwardDirection < 0) // si on veut accelerer en arière
        {
            nextSpeed -= flatAccelSpeed + (maxSpeed - currentSpeed) * accelSpeed * Time.fixedDeltaTime;
        }
        else // si on ne veut pas accelerer
        {
            if (currentSpeed > 0) // si on avance
            {
                nextSpeed -= flatDecelSpeed + (maxSpeed - currentSpeed) * decelSpeed * Time.fixedDeltaTime;
                if (nextSpeed < 0)
                {
                    nextSpeed = 0;
                }
            }
            else if (currentSpeed < 0) // si on  recule
            {
                nextSpeed += flatDecelSpeed + (maxSpeed - currentSpeed) * decelSpeed * Time.fixedDeltaTime;
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
    void HandleDrift()
    {

    }

    void HandleTurning()
    {
 
    }

    void StartDrift()
    {
 
    }



    public void StartTurbo(float force, float time)
    {
        turbo = true;
        turboTimer = time;
        targetTurboForce = force;
        Debug.Log("turbo");
    }
    void HandleTurbo()
    {
       
    }

    void HandleWholeKartRotationXZ()
    {
   
    }

    void HandleVisualKartBody()
    {

    }

    void HandleVisualKartWheels()
    {

    }

    void HandleCameraLocalPosition()
    {
 
    }

    private void OnCollisionEnter(Collision collision)
    {
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
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == 7)
        {
            grounded = true;
            groundNormal = collision.contacts[0].normal; // l'orientation du kart
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == 7)
        {
            grounded = false;
        }
    }
    void SquishAnimation()
    {
      /*  if (bounce)
        {
            bounceTimer += Time.fixedDeltaTime;
            if (bounceTimer < 0.05f)
            {
                visualKartBody.transform.localScale += new Vector3(-6f, 10f, -6f) * Time.fixedDeltaTime;
            }
            else if (bounceTimer < 0.1f)
            {
                visualKartBody.transform.localScale += new Vector3(6f, -10f, 6f) * Time.fixedDeltaTime;
            }
            else
            {
                visualKartBody.transform.localScale = Vector3.one;
                bounce = false;
                bounceTimer = 0;
            }
        }*/
    }
    void HandleSmoke()
    {
     /*   if (turbo)
        {
            var FPemission = fireParticlesGenerator.emission;
            FPemission.rateOverDistance = 3;
        }
        else
        {
            var FPemission = fireParticlesGenerator.emission;
            FPemission.rateOverDistance = 0;
        }*/
    }
}
