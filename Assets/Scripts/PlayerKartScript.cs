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
    public float maxForwardSpeed;
    //public float maxBackwardSpeed;
    public float currentSpeed;
    public float currentSpeedTarget;
    [Header("Acceleration")]
    public float accelForce;

    [Header("Ground")]
    bool grounded;
    Vector3 groundNormal;
    public Transform groundNormalT;

    [Header("Wheels")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform backLeftWheel;
    public Transform backRightWheel;

    public Transform frontLeftWheelPivot;
    public Transform frontRightWheelPivot;
    public Transform backLeftWheelPivot;
    public Transform backRightWheelPivot;

    float[] wheelDistFromPivots;

    [Header("Deceleration")]
    
    [Header("Turning")]
    
    [Header("Turbo")]
    
    [Header("Colisions")]
    
    [Header("Camera")]
   
    [Header("Visual Kart")]
   
    [Header("Bounce Animation")]
   
    [Header("Smoke")]
  
    [Header("Gravity")]
  
    [Header("Drift")]
    
    public Transform driftPivot;

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
        //HandleAccelerationForceTarget();
        //HandleCurrentAccelForce();

        HandleCurrentSpeedTarget();
        HandleCurrentSpeed();

        HandleTurning();

        
        rb.linearVelocity = transform.forward * currentSpeed;

        for (int i = 0; i < wheelDistFromPivots.Length; i++)
        {

        }
        //groundNormalT.transform.localRotation = Quaternion.LookRotation(Vector3.Cross(transform.right, groundNormal), groundNormal);//Quaternion.LookRotation(Vector3.Cross(transform.right, groundNormal), groundNormal); // oriente le y vers le haut de la normale et le x vers l'avant du kart ( 2 semaines de galère )
        //transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.forward, groundNormalT.localEulerAngles), groundNormalT.localEulerAngles);//Quaternion.RotateTowards(transform.rotation, groundNormalT.localRotation, 1f);

        transform.Rotate(0, turnDirection, 0);
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
        //keepDrifting = InputSystemHandler.instance.inputDrift;
    }
    void HandleCurrentSpeed()
    {
        float nextSpeed = currentSpeed;
        //si la vitesse est plus basse que la vitesse cible on fait monter la vitesse
        if (currentSpeed < currentSpeedTarget)
        { 
            nextSpeed += accelForce * Time.fixedDeltaTime; 
            if (nextSpeed > currentSpeedTarget) { nextSpeed = currentSpeedTarget; }
        }
        //si la vitesse est plus haute que la vitesse cible on fait baisser la vitesse
        else if (currentSpeed > currentSpeedTarget)
        {
            nextSpeed -= accelForce * Time.fixedDeltaTime;
            if (nextSpeed < currentSpeedTarget) { nextSpeed = currentSpeedTarget; }
        }
        // on applique la nouvelle vitesse
        currentSpeed = nextSpeed;
    }
    void HandleCurrentSpeedTarget()
    {
        currentSpeedTarget = forwardDirection * maxForwardSpeed;
    }
    void HandleTurning()
    {
 
    }
    /*void HandleAccelerationForceTarget()
    {
        if (forwardDirection > 0) { currentAccelForceTarget = maxAccelerationForce; }
        else if (forwardDirection < 0) { currentAccelForceTarget = -maxAccelerationForce; }
        else { currentAccelForceTarget = 0; }
    }

    void HandleCurrentAccelForce()
    {
        // fait monter ou descendre la force d'acceleration pour ateindre la force cible
        float nextAccelForce = accelForce;
        if (accelForce < currentAccelForceTarget)
        {
            nextAccelForce += maxAccelerationForce * Time.fixedDeltaTime;
            if (nextAccelForce > currentAccelForceTarget) { nextAccelForce = currentAccelForceTarget; }            
        }
        else if (accelForce > currentAccelForceTarget)
        {
            nextAccelForce -= maxAccelerationForce * Time.fixedDeltaTime;
            if (nextAccelForce < currentAccelForceTarget){nextAccelForce = currentAccelForceTarget;}
        }
        accelForce = nextAccelForce;
    }*/
    void HandleDrift()
    {

    }

    

    void StartDrift()
    {
 
    }

    public void StartTurbo(float force, float time)
    {
       
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

    /*private void OnCollisionEnter(Collision collision)
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
    }*/
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
