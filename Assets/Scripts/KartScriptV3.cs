
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

public class KartScriptV3 : MonoBehaviour
{
    public static KartScriptV3 instance;
    [Header("Components")]
    public Rigidbody rb;
    [Header("Inputs")]
    float forwardDirection;
    float turnDirection; // la direction de la rotation du vollant
    float inputGlideTurn;
    InputSystem_Actions controls;
    [Header("Speed")]
    public float maxSpeed;
    public float currentSpeed;
    public float maxBackSpeed;
    float airSpeed;
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
    public Transform camPivot;
    public Vector3 thirdPersonCamPos;
    public Vector3 firstPersonCamPos;
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
    float[] wheelsYPosTarget;
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
    public Transform groundNormalT;
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
    public float driftCoyoteTime;
    float driftCoyoteTimer;
    [Header("Flight")]
    public bool isFlying;
    public float flightSpeed;
    public Transform flightDir;
    public float maxFlightTurnForce;
    float currentFlightTurnForce;
    float inputGlideUpDown;
    public GameObject gliderGO;
    float tryFlightTimer;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        controls = new InputSystem_Actions(); // initialiser input
        //controls.Player.Turn.performed += ctx => HandheldMovePressed(ctx);        
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        PlayerInputs();
        HandleDrift();
        /* if (Input.GetMouseButtonDown(1))
         {
             StartTurbo(10f, 1.5f);
         }*/
    }
    /*private void HandheldMovePressed(InputAction.CallbackContext ctx)
    {
        Debug.Log("ctx");
        turnDirection = ctx.ReadValue<int>();
    }*/
    private void FixedUpdate()
    {
        // On gère la physique du kart
        HandleCurrentSpeed();
        HandleTurning();
        HandleTurbo();

        // ton enleve le signe de currentSpeed
        //float unsignedCurSpeed = currentSpeed;
        //if (unsignedCurSpeed < 0) {unsignedCurSpeed = -unsignedCurSpeed;}

        // on gère la force du bounce contre les murs        
        HandleBounceForce();
        HandleGravity();

        //if (!isFlying)
        //{
        //rb.linearVelocity = (groundNormalT.transform.forward * (currentSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + currentFallSpeed);
        //}
        /*else
        {
            currentSpeed = 25f;
            if (transform.eulerAngles.x > 0f && transform.eulerAngles.x < 180f)
            {
                flightSpeed += transform.eulerAngles.x * 0.25f * Time.fixedDeltaTime;
                if (flightSpeed > 30f)
                {
                    flightSpeed = 30f;
                }
            }
            else if (transform.eulerAngles.x > 180f && transform.eulerAngles.x < 360f)
            {
                flightSpeed += (transform.eulerAngles.x - 360f) * 0.25f * Time.fixedDeltaTime;
                if (flightSpeed < 0f)
                {
                    flightSpeed = 0f;
                }
            }
        
            
                Debug.Log(transform.eulerAngles.x);
            rb.linearVelocity = (transform.forward * (flightSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * 1f / (flightSpeed + 0.1f);           
            transform.Rotate(1f * inputGlideUpDown, 0, 0);            
        }*/
        if (grounded)
        {
            gliderGO.SetActive(false);
            transform.Rotate(0, currentTurnSpeed + currentDriftForce, 0);
            rb.linearVelocity = (groundNormalT.transform.forward * (currentSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + currentFallSpeed);

            //isFlying = false;
        }
        else if (!isFlying)
        {
            transform.Rotate(0, (currentTurnSpeed + currentDriftForce) / 3f, 0);
            rb.linearVelocity = (groundNormalT.transform.forward * (airSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + currentFallSpeed);

            if (currentTurboForce <= 0) airSpeed -= 5f * Time.fixedDeltaTime;
            if (airSpeed < 0) airSpeed = 0;
        }
        else
        {
            gliderGO.SetActive(true);
            //groundNormalT.transform.localEulerAngles = Vector3.zero;
            if (flightDir.eulerAngles.x > 0f && flightDir.eulerAngles.x < 180f)
            {
                flightSpeed += (flightDir.eulerAngles.x) * 0.8f * Time.fixedDeltaTime;
                //Debug.Log("BAS" + flightDir.transform.eulerAngles.x);
                if (flightSpeed > 30f)
                {
                    flightSpeed = 30f;
                }
            }
            else if (flightDir.eulerAngles.x > 180f && flightDir.eulerAngles.x < 360f)
            {
                flightSpeed += (flightDir.eulerAngles.x - 360f) * 0.25f * Time.fixedDeltaTime;
                //Debug.Log("HAUT" + (flightDir.transform.eulerAngles.x - 360f));
                if (flightSpeed < 0.8f)
                {
                    flightSpeed = 0.8f;
                }
            }
            if (flightSpeed < currentTurboForce)
            {
                flightSpeed = currentTurboForce;
            }
            //Debug.Log(flightDir.transform.eulerAngles.x);
            if (inputGlideUpDown == 0)
            {
                float extraNoseSpeed = 0f;
                if (flightDir.eulerAngles.x > 180f && flightDir.eulerAngles.x < 360f)
                {
                    extraNoseSpeed = (360f - flightDir.eulerAngles.x) / 35f;
                    flightDir.Rotate(0.1f + extraNoseSpeed, 0, 0);
                }
                else if (flightDir.eulerAngles.x > 0 && flightDir.eulerAngles.x < 5f)
                {
                    extraNoseSpeed = -flightDir.eulerAngles.x / 35f;
                    flightDir.Rotate(0.1f + extraNoseSpeed, 0, 0);
                }
                else if (flightDir.eulerAngles.x < 90f && flightDir.eulerAngles.x > 5f)
                {
                    extraNoseSpeed = -flightDir.eulerAngles.x / 35f;
                    flightDir.Rotate(-0.1f + extraNoseSpeed, 0, 0);
                }
            }
            else
            {
                //if (flightDir.eulerAngles.x > 45f && flightDir.eulerAngles.x < 180f || flightDir.eulerAngles.x > 360 && flightDir.eulerAngles.x < 180f)
                //{
                flightDir.Rotate(inputGlideUpDown, 0, 0);
                //}       
                //else
                //{
                //    flightDir.eulerAngles = new Vector3(45f ,flightDir.eulerAngles.y, flightDir.eulerAngles.z);
                //}
            }
            //Mathf.Clamp(flightDir.rotation.x, -45f, 90f);
            if (turnDirection != 0f)
            {

                currentFlightTurnForce += (0.1f * turnDirection + maxFlightTurnForce) * turnDirection * Time.fixedDeltaTime;
                if (currentFlightTurnForce < -maxFlightTurnForce) currentFlightTurnForce = -maxFlightTurnForce;
                else if (currentFlightTurnForce > maxFlightTurnForce) currentFlightTurnForce = maxFlightTurnForce;
            }
            else if (currentFlightTurnForce > 0f)
            {
                currentFlightTurnForce -= 3f * Time.fixedDeltaTime;
                if (currentFlightTurnForce < 0) { currentFlightTurnForce = 0; }
            }
            else if (currentFlightTurnForce < 0f)
            {
                currentFlightTurnForce += 3f * Time.fixedDeltaTime;
                if (currentFlightTurnForce > 0) { currentFlightTurnForce = 0; }
            }
            transform.Rotate(0, currentFlightTurnForce, 0);//(0, (currentTurnSpeed + currentDriftForce) / 1.5f, 0);
            flightDir.localEulerAngles = new Vector3(flightDir.localEulerAngles.x, 0f, currentFlightTurnForce * 10f);
            //flightDir.Rotate(0, 0, currentFlightTurnForce);
            rb.linearVelocity = (flightDir.forward * (flightSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + (currentFallSpeed / (1f + flightSpeed / 2.5f)) / 1.2f);
            //Debug.Log(flightSpeed);
            //if (currentTurboForce <= 0) airSpeed -= 5f * Time.fixedDeltaTime;
            //if (airSpeed < 0) airSpeed = 0;
        }
    }
    private void LateUpdate()
    {
        // on gère les visuels du kart
        HandleWholeKartRotationXZ();
        HandleVisualKartBody();
        HandleVisualKartWheels();

        SquishAnimation();
        HandleSmoke();
        HandleCameraTransform();
    }

    void PlayerInputs()
    {
        /*forwardDirection = Input.GetAxisRaw("Vertical");
        turnDirection = Input.GetAxisRaw("Horizontal");
        tryToDrift = Input.GetButtonDown("Drift1");
        keepDrifting = Input.GetButton("Drift1");
        if (bounce) keepDrifting = false;*/
        //tryToDrift = Input.GetMouseButtonDown(0);
        //keepDrifting = Input.GetMouseButton(0);      
        forwardDirection = InputSystemHandler.instance.inputForwardDir;
        turnDirection = InputSystemHandler.instance.inputTurnDir;
        tryToDrift = InputSystemHandler.instance.inputTryDrift;
        keepDrifting = InputSystemHandler.instance.inputDrift;

        inputGlideUpDown = InputSystemHandler.instance.inputGlideUpDownDir;
        inputGlideTurn = InputSystemHandler.instance.inputGlideTurnDir;
        //Debug.Log(InputSystemHandler.instance.inputGlideTurnDir);
    }

    public void TryStartFlight(float fSpeed)
    {
        tryFlightTimer = 1.5f;
    }
    public void StartFlight(float fSpeed)
    {
        isFlying = true;
        flightSpeed = fSpeed;
        groundNormalT.transform.localEulerAngles = Vector3.zero;
        visualKartBody.transform.localEulerAngles = Vector3.zero;
        visualKartWheelsParent.transform.localEulerAngles = Vector3.zero;
        preOrientation.transform.localEulerAngles = Vector3.zero;
    }
    private void HandleBounceForce()
    {
        // on baisse la force jusqu'a qu'elle soit à 0
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
    }

    private void HandleGravity()
    {
        // Si on est pas au sol on accelère la vitesse de chute
        if (grounded) { currentFallSpeed = 0; }
        else if (currentFallSpeed < 32f) { currentFallSpeed += gravity * Time.fixedDeltaTime; }
    }
    bool IsGrounded()
    {
        RaycastHit hit;
        if (Physics.Raycast(groundRayOrigin.position, Vector3.down, out hit, 0.25f))
        {
            groundNormal = hit.normal;
            transform.position = new Vector3(transform.position.x, hit.point.y + 0.54f, transform.position.z);
            //Debug.Log("grounded" + (0.5f - hit.distance));
            return true;
        }
        return false;
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

        if (keepDrifting && grounded)
        {
            // on fait monter ou descendre la rotation Y vers targetYRot
            float targetYRot = (driftDir + turnDirection) * 18f;

            if (nextYDriftRot < targetYRot)
            {
                nextYDriftRot += 12f * Time.fixedDeltaTime;
                if (nextYDriftRot > targetYRot) { nextYDriftRot = targetYRot; } // on dépasse pas targetYRot
            }
            else if (nextYDriftRot > targetYRot)
            {
                nextYDriftRot += -12f * Time.fixedDeltaTime;
                if (nextYDriftRot < targetYRot) { nextYDriftRot = targetYRot; } // on dépasse pas targetYRot
            }
            driftPivot.localRotation = Quaternion.Euler(0, nextYDriftRot, 0);
            oldKeepD = keepDrifting;
            driftCoyoteTime = 0.12f;
        }
        else
        {
            if (driftCoyoteTime > 0f)
            {
                driftCoyoteTime -= Time.deltaTime;
                return;
            }
            if (nextYDriftRot < 0)
            {
                nextYDriftRot += 12f * Time.fixedDeltaTime;
                if (nextYDriftRot > 0)
                {
                    nextYDriftRot = 0;
                }
            }
            else if (nextYDriftRot > 0)
            {
                nextYDriftRot += -12f * Time.fixedDeltaTime;
                if (nextYDriftRot < 0)
                {
                    nextYDriftRot = 0;
                }
            }
            driftPivot.localRotation = Quaternion.Euler(0, nextYDriftRot, 0);

            currentDriftForce = 0;
            driftCatchUp = 0;
            if (driftTurboGauge > gaugeToActivateTurbo)
            {
                StartTurbo(driftTurboGauge * 2.2f, driftTurboGauge / 2.6f);
                driftTurboGauge = 0;
                //Debug.Log(transform.forward + " " + driftPivot.forward);
                Vector3 oldCamForward = camPivot.forward;
                //Debug.Break();
                //Debug.Log(transform.forward + " 1 " + driftPivot.forward);
                transform.forward = new Vector3(driftPivot.forward.x, 0, driftPivot.forward.z);
                //Debug.Break();
                //Debug.Log(transform.forward + " 2 " + driftPivot.forward);
                driftPivot.forward = transform.forward;
                //Debug.Log(transform.forward + " 3 " + driftPivot.forward);
                nextYDriftRot = 0;
                camPivot.forward = oldCamForward;
                //Debug.Break();

            }
            driftDir = 0;
            for (int i = 0; i < fireWheelEffects.Length; i++)
            {
                fireWheelEffects[i].SetActive(false);
                fireWheelEffects[i].transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            }
            for (int i = 0; i < driftParticlesGenerators.Length; i++)
            {
                driftParticlesGenerators[i].gameObject.SetActive(false);
            }
            //return; 
        }
        //driftPivot.forward = Vector3.RotateTowards(driftPivot.forward, new Vector3(driftDir + turnDir , 0,1), 0.75f * Time.fixedDeltaTime, 0.0f);
        if (forwardDirection == 0)
        {
            driftDir = 0;
            currentDriftForce = 0;
            driftCatchUp = 0;
            driftTurboGauge = 0;
            for (int i = 0; i < fireWheelEffects.Length; i++)
            {
                fireWheelEffects[i].SetActive(false);
                fireWheelEffects[i].transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            }
            //return;
        }
        if (tryToDrift && grounded) //if (tryToDrift && grounded)
        {
            if (currentTurnSpeed > 0.5f && turnDirection > 0)
            {
                driftDir = 1;
                for (int i = 0; i < driftParticlesGenerators.Length; i++)
                {
                    driftParticlesGenerators[i].gameObject.SetActive(true);
                }
            }
            else if (currentTurnSpeed < -0.5f && turnDirection < 0)
            {
                driftDir = -1;
                for (int i = 0; i < driftParticlesGenerators.Length; i++)
                {
                    driftParticlesGenerators[i].gameObject.SetActive(true);
                }
            }
        }
        if (driftTurboGauge > gaugeToActivateTurbo)
        {
            for (int i = 0; i < fireWheelEffects.Length; i++)
            {
                fireWheelEffects[i].SetActive(true);
                float fireWheelSize = Mathf.Clamp(1 + driftTurboGauge / 6, 1.01f, 1.8f);
                fireWheelEffects[i].transform.localScale = new Vector3(fireWheelSize, 0.05f, fireWheelSize);
            }
        }
        if (driftDir != 0)
        {
            float nextDriftForceTarget = 1f;
            if (driftDir > 0 && turnDirection < 0)
            {
                nextDriftForceTarget = 3f;
                driftTurboGauge += 0.2f * Time.deltaTime;
            }
            else if (driftDir < 0 && turnDirection > 0)
            {
                nextDriftForceTarget = 3f;
                driftTurboGauge += 0.2f * Time.deltaTime;
            }
            else if (driftDir > 0 && turnDirection > 0)
            {
                driftTurboGauge += 2f * Time.deltaTime;
            }
            else if (driftDir < 0 && turnDirection < 0)
            {
                driftTurboGauge += 2f * Time.deltaTime;
            }
            else
            {
                driftTurboGauge += 0.8f * Time.deltaTime;
            }
            if (driftCatchUp < nextDriftForceTarget)
            {
                driftCatchUp += 5f * Time.deltaTime;
            }
            else if (driftCatchUp > nextDriftForceTarget)
            {
                driftCatchUp -= 5f * Time.deltaTime;
            }
            currentDriftForce = driftDir * driftCatchUp;
        }
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
        /*if (keepDrifting)
        {
            if (nextTurnSpeed > maxTurnSpeed)
            {
                nextTurnSpeed = maxTurnSpeed;
            }
            else if (nextTurnSpeed < -maxTurnSpeed)
            {
                nextTurnSpeed = -maxTurnSpeed;
            }
        }*/

        // on applique
        //Debug.Log(nextTurnSpeed + " + " + currentDriftForce);
        currentTurnSpeed = nextTurnSpeed;
    }

    void StartDrift()
    {
        if (currentTurnSpeed > 1f)
        {
            driftDir = 1;
        }
        else if (currentTurnSpeed < -1f)
        {
            driftDir = -1;
        }
    }



    public void StartTurbo(float force, float time)
    {
        turbo = true;
        if (time > turboTimer)
        {
            turboTimer = time;
        }
        if (force > targetTurboForce)
        {
            targetTurboForce = force;
        }
        //turboTimer = time;
        //targetTurboForce = force;
        //float nextTForce = targetTurboForce + force;

        //Debug.Log("turbo");
    }
    void HandleTurbo()
    {
        if (turbo)
        {
            turboTimer -= Time.fixedDeltaTime;
            if (turboTimer > 0)
            {
                float nextTForce = currentTurboForce + turboAccelSpeed + targetTurboForce * Time.fixedDeltaTime;
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
                currentTurboForce -= 1f + minTurboDecel * minTurboDecel * Time.fixedDeltaTime;
            }
            if (currentTurboForce <= 0)
            {
                turbo = false;
                currentTurboForce = 0;
                targetTurboForce = 0;
            }
        }
    }

    void HandleWholeKartRotationXZ()
    {
        //transform.up = new Vector3(goundNormal.x, goundNormal.y, goundNormal.z);
    }

    void HandleVisualKartBody()
    {

        if (isFlying && !grounded)
        {
            visualKartBody.transform.forward = flightDir.forward;//Quaternion.RotateTowards(visualKartBody.transform.localRotation, rotTarget, 0.7f);
            //Quaternion rotTarget1 = Quaternion.Euler(0, 0,inputGlideTurn * 50f);
            //visualKartBody.transform.localRotation = Quaternion.RotateTowards(visualKartBody.transform.localRotation, rotTarget1, 0.7f);
            //Debug.Log(rotTarget1);
            //visualKartBody.transform.Rotate(0,0, -inputGlideTurn * 30f);
            //visualKartBody.transform.localEulerAngles = Vector3.RotateTowards(visualKartBody.transform.localEulerAngles, new Vector3(visualKartBody.transform.localEulerAngles.x, visualKartBody.transform.localEulerAngles.y, -currentFlightTurnForce * 20f), 1f,0f);
            visualKartBody.transform.localEulerAngles = new Vector3(visualKartBody.transform.localEulerAngles.x, visualKartBody.transform.localEulerAngles.y, -currentFlightTurnForce * 20f);
            //Debug.Log(inputGlideTurn);
            return;
        }
        if (currentSpeed > 0)
        {
            if (forwardDirection > 0)
            {
                visKartXRotCatchUp = (maxSpeed - currentSpeed) / 6f;
            }
            else
            {
                visKartXRotCatchUp = -(maxSpeed - currentSpeed) / 6f;
            }
        }
        else if (currentSpeed < 0)
        {
            if (forwardDirection < 0)
            {
                visKartXRotCatchUp = (maxSpeed + currentSpeed) / 10f;
            }
            else
            {
                visKartXRotCatchUp = -(maxSpeed + currentSpeed) / 10f;
            }
        }
        else if (visKartXRotCatchUp < 0.05f && visKartXRotCatchUp > -0.05f)
        {
            visKartXRotCatchUp = 0;
        }

        float nextVisKartXRotCatchUpBis = visKartXRotCatchUpBis;
        if (nextVisKartXRotCatchUpBis < visKartXRotCatchUp)
        {
            nextVisKartXRotCatchUpBis += 8f * Time.fixedDeltaTime;
            if (nextVisKartXRotCatchUpBis < visKartXRotCatchUp)
            {
                visKartXRotCatchUpBis = nextVisKartXRotCatchUpBis;
            }
            else
            {
                visKartXRotCatchUpBis = visKartXRotCatchUp;
            }
        }
        else if (nextVisKartXRotCatchUpBis > visKartXRotCatchUp)
        {
            nextVisKartXRotCatchUpBis -= 8f * Time.fixedDeltaTime;
            if (nextVisKartXRotCatchUpBis > visKartXRotCatchUp)
            {
                visKartXRotCatchUpBis = nextVisKartXRotCatchUpBis;
            }
            else
            {
                visKartXRotCatchUpBis = visKartXRotCatchUp;
            }
        }
        visKartXRot = (-currentSpeed / 2.5f) * visKartXRotCatchUpBis;
        visKartZRot = currentTurnSpeed * (currentSpeed / 4.5f) + (driftCatchUp * 7f * driftDir);

        float nextTotalSpeed = visKartXRot + -currentTurboForce * 0.5f;
        nextTotalSpeed = Mathf.Clamp(nextTotalSpeed, -(maxSpeed), maxSpeed + 2f);
        groundNormalT.transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, groundNormal), groundNormal); // oriente le y vers le haut de la normale et le x vers l'avant du kart ( 2 semaines de galère )
        preOrientation.localRotation = Quaternion.RotateTowards(preOrientation.localRotation, groundNormalT.localRotation, 1f);
        //preOrientation.forward = Vector3.RotateTowards(preOrientation.forward, groundNormalT.forward, 1.5f * Time.fixedDeltaTime, 0.0f);
        //preOrientation.rotation = Quaternion.Euler(preOrientation.rotation.x, 0, preOrientation.rotation.z);
        //preOrientation.forward = transform.forward;
        //Debug.Log(nextTotalSpeed + " " + visualKartBody.transform.localEulerAngles.x);
        Quaternion rotTarget = Quaternion.Euler(nextTotalSpeed, 0, visKartZRot);
        //if (!grounded)
        visualKartBody.transform.localRotation = Quaternion.RotateTowards(visualKartBody.transform.localRotation, rotTarget, 10f);




        //visualKartBody.transform.localRotation = Quaternion.Euler(nextTotalSpeed, 0, visKartZRot);
        //preOrientation.up = groundNormal;

        //preOrientation.localRotation = Quaternion.LookRotation(groundNormal, transform.up);
        //preOrientation.forward = transform.forward;
        //Debug.Log(gameObject.transform.eulerAngles.y);
        //visualKartBody.transform.localRotation = Quaternion.Euler(nextTotalSpeed, transform.eulerAngles.y, visKartZRot);

        //Quaternion.Euler(nextTotalSpeed, transform.eulerAngles.y, visKartZRot);
        //visualKartBody.transform.up = goundNormal;
        //visualKartBody.transform.
    }

    void HandleVisualKartWheels()
    {
        //visWheelsYRot = currentTurnSpeed * 12;
        //visualKartWheelsParent.transform.localRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        if (isFlying)
        {
            visualKartWheelsParent.transform.localRotation = Quaternion.Euler(visualKartBody.transform.localEulerAngles.x, 0, visualKartBody.transform.localEulerAngles.z);

            return;
        }
        visualKartWheelsParent.transform.localRotation = Quaternion.Euler(0, 0, 0);
        turningWheelsXRot += (currentSpeed + currentTurboForce) * turningWheelsRatioScaling * Time.fixedDeltaTime;
        nonTurningWheelsXRot += (currentSpeed + currentTurboForce) * nonTurningWheelsRatioScaling * Time.fixedDeltaTime;
        for (int i = 0; i < turningWheels.Length; i++)
        {
            turningWheels[i].transform.localRotation = Quaternion.Euler(turningWheelsXRot, visWheelsYRot, 90);
            //turningWheels[i].transform.Rotate(0, 10, 0);
            /*RaycastHit hit;
            if (Physics.Raycast(turningWheels[i].transform.position, new Vector3(0,-1f,0), out hit, 0.4f))
            {
                //Vector3 startPos = turningWheels[i].transform.localPosition;
                turningWheels[i].transform.localPosition = new Vector3(0f, (transform.position - hit.point).magnitude - turningWheels[i].transform.localScale.x, 0f);//new Vector3(-0.54f, -((transform.position - hit.point).magnitude + turningWheels[i].transform.localScale.x), - 0.126f);
                Debug.Log(hit.point);
            }*/
        }
        for (int i = 0; i < nonTurningWheels.Length; i++)
        {
            nonTurningWheels[i].transform.localRotation = Quaternion.Euler(nonTurningWheelsXRot, 0, 90);
            //turningWheels[i].transform.Rotate(0, 10, 0);
        }
    }

    void HandleCameraTransform()
    {
        //ThirdPersonCamPivot.position = transform.position;
        float driftForce = Mathf.Clamp(currentDriftForce, -1.5f, 1.5f);
        if (driftForce < 0)
        {
            driftForce = -driftForce;
        }
        camXpos = Mathf.Clamp((currentSpeed * -currentTurnSpeed / 120f * forwardDirection) + (turnDirection * driftForce), -1f, 1f);//(currentSpeed * -currentTurnSpeed / 120f * forwardDirection);// + turnDirection * currentDriftForce;
        //Debug.Log(camXpos + " driftForce = " + currentDriftForce + " turn dir = " + turnDirection);
        //+ -currentTurnSpeed;
        /*if (playerCamera.transform.localRotation.y > camXpos)
        {
            playerCamera.transform.Rotate(0, -1f * Time.fixedDeltaTime, 0);
        }
        else if (playerCamera.transform.localRotation.y < camXpos)
        {
            playerCamera.transform.Rotate(0, 1f * Time.fixedDeltaTime, 0);
        }*/
        /*if (keepDrifting)
        {
            playerCamera.transform.localPosition = new Vector3(camXpos * driftDir, visualKartBody.transform.localRotation.y, -4.75f);
            return;
        }*/
        if (playerCamera.transform.localPosition.x > camXpos)
        {
            float nextXpos = playerCamera.transform.localPosition.x - 0.15f * Time.fixedDeltaTime;
            if (nextXpos < camXpos)
            {
                nextXpos = camXpos;
            }
            playerCamera.transform.localPosition = new Vector3(nextXpos, playerCamera.transform.localPosition.y, playerCamera.transform.localPosition.z);
        }
        else if (playerCamera.transform.localPosition.x < camXpos)
        {
            float nextXpos = playerCamera.transform.localPosition.x + 0.15f * Time.fixedDeltaTime;
            if (nextXpos > camXpos)
            {
                nextXpos = camXpos;
            }
            playerCamera.transform.localPosition = new Vector3(nextXpos, playerCamera.transform.localPosition.y, playerCamera.transform.localPosition.z);
        }
        //Debug.Log(transform.right * driftDir * currentDriftForce * 0.05f);
        Vector3 targetDir = (transform.forward + (transform.right * currentTurnSpeed * Mathf.Clamp(currentDriftForce, -1f, 1f) * 0.05f)).normalized;
        float rotSpeed = 0.1f + (camPivot.forward - targetDir).magnitude * 0.08f;
        //Debug.Log(" targetDir = "+ targetDir + "  turn = " + (currentTurnSpeed * 0.05f) + "  drift = " +(currentDriftForce * 1f));
        camPivot.forward = Vector3.RotateTowards(camPivot.forward, targetDir, rotSpeed * Time.fixedDeltaTime, 0.0f);

        if (InputSystemHandler.instance.inputCameraMode)
        {
            Vector3 nextDir = thirdPersonCamPos - playerCamera.transform.localPosition;
            Vector3 nextPos = nextDir.normalized * 3f * Time.fixedDeltaTime;
            if (nextDir.sqrMagnitude > 0.2f)
            {
                playerCamera.transform.localPosition += nextPos;
            }
            else
            {
                playerCamera.transform.localPosition = thirdPersonCamPos;
            }
        }
        else
        {
            Vector3 nextDir = firstPersonCamPos - playerCamera.transform.localPosition;
            Vector3 nextPos = nextDir.normalized * 3f * Time.fixedDeltaTime;
            if (nextDir.sqrMagnitude > 0.2f)
            {
                playerCamera.transform.localPosition += nextPos;
            }
            else
            {
                playerCamera.transform.localPosition = firstPersonCamPos;
            }
        }
        // playerCamera.transform.localRotation = Quaternion.Euler(32, camYRot, 0);
        //playerCamera.transform.localPosition = new Vector3(camXpos, 3.9f, -4.7f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log(collision.gameObject.layer);
        isFlying = false;
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
            //keepDrifting = false;
            //rb.AddForce((, ForceMode.Impulse);
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == 7)
        {
            grounded = true;
            groundNormal = collision.contacts[0].normal; // l'orientation du kart visuel
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // quand on quitte le sol
        if (collision.gameObject.layer == 7)
        {
            grounded = false;
            airSpeed = currentSpeed;
            flightSpeed = currentSpeed;
            flightDir.forward = visualKartBody.transform.forward;
            //if (groundNormalT.localEulerAngles.x > 0)
            //{
            //    currentFallSpeed = 8f;
            //    return;
            //}
            //currentFallSpeed += (-groundNormalT.localEulerAngles.x / 70f) * currentSpeed / maxSpeed; // on donne une fausse inertie via la gravité selon l'angle x de la dernière normale            
            // si on recule en sortie de sol on tombe bien plus vite!!!            
        }
    }
    void SquishAnimation()
    {
        if (bounce)
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
        }
    }

    void HandleSmoke()
    {
        if (turbo)
        {
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
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(groundRayOrigin.position, Vector3.down * 0.5f);
    }


    void CheckIfOffTrack()
    {
        Debug.Log("je suis appeler");
        if (respawnCooldown > 0)
        {
            respawnCooldown -= Time.fixedDeltaTime;
            return;
        }

        if (Physics.Raycast(groundRayOrigin.position, Vector3.down, 5f))
        {
            Debug.Log("Je touche quelque chose");
        }
        else
        {
            Debug.Log("Je touche RIEN");
        }
        if (!Physics.Raycast(groundRayOrigin.position, Vector3.down, 5f, trackLayer))
        {
            Debug.Log("cacacacacacaca");
            checkPointManager.Respawn();
            transform.position = startPosition;
            respawnCooldown = 1.5f;
        }
    }

    void GhostDrive()
    {
        if (currentWaypoint == null)
        {
            currentWaypoint = firstWaypoint;
            return;
        }

        Vector3 dir = currentWaypoint.position - transform.position;

        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

        turnDirection = Mathf.Clamp(angle / 30f, -1f, 1f);
        forwardDirection = 1f;

        if (dir.magnitude < 4f)
        {
            currentWaypoint = currentWaypoint.GetComponent<Waypoints>().nextWaypoint;
        }
    }
}