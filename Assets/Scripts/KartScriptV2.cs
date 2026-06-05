using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class KartScriptV2 : MonoBehaviour
{
    // JAHMI

    public static KartScriptV2 instance;

    [Header("Components")]
    public Rigidbody rb;

    [Header("Inputs")]
    private float forwardDirection;
    public float turnDirection; // la direction de la rotation du volant
    private float inputGlideTurn;
    private InputSystem_Actions controls;

    [Header("Speed")]
    public float maxSpeed;
    public float currentSpeed;
    public float maxBackSpeed;
    private float airSpeed;

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
    public float currentMaxTurnSpeed;
    public float currentTurnSpeed; // c'est l'equivalent de la rotation du vollant
    public float turnAccelSpeed;
    public float turnDecelSpeed;

    [Header("Turbo")]
    public float currentTurboForce;
    public float turboAccelSpeed;
    public float minTurboDecel;
    private float targetTurboForce;
    private bool turbo;

    [Header("Colisions")]
    public LayerMask wallLayer;
    public Vector3 bounceDirection;
    public float bounceForce;
    public float minBounceDecelForce;

    [Header("Camera")]
    public GameObject playerCamera;
    public Transform camPivot;
    [SerializeField] Transform emptyCamSpot;
    public Vector3 thirdPersonCamPos;
    public Vector3 firstPersonCamPos;
    private Vector3 currentCamPosCenter;
    private float turboTimer;
    float cameraZoneUp;

    [Header("Visual Kart")]
    public GameObject visualKartBody;
    public GameObject visualKartWheelsParent;
    public float visKartZRot;
    public float visKartXRot;
    public float visKartXRotCatchUp;
    private float visKartXRotCatchUpBis;
    public float visKartTurboXRotCatchUp;
    public GameObject[] turningWheels;
    public GameObject[] nonTurningWheels;
    public GameObject[] fireWheelEffects;
    public float visWheelsYRot;
    private float turningWheelsXRot;
    private float nonTurningWheelsXRot;
    public float turningWheelsRatioScaling;
    public float nonTurningWheelsRatioScaling;
    [SerializeField] Transform steeringWheel;
    [SerializeField] Transform cocot;
    [SerializeField] Transform[] cretes;
    float creteAccelZ;
    float creteAccelX;

    [Header("Bounce Animation")]
    public bool bounce;
    private float bounceTimer;

    [Header("Smoke")]
    public GameObject smokePrefab;
    public Transform smokeOrigin;
    public Material baseSmokeMat;
    public Material fireSmokeMat;
    public ParticleSystem[] smokeParticlesGenerator;
    public ParticleSystem[] fireParticlesGenerator;
    public ParticleSystem[] driftParticlesGenerators;

    [Header("Gravity")]
    public float gravity;
    public float currentFallSpeed;
    private int minimalGrav;
    private Vector3 groundNormal;
    public Transform preOrientation;
    public Transform groundNormalT;
    public Transform groundRayOrigin;
    private bool grounded;
    float groundedCoyoteTimer;

    [Header("Drift")]
    private bool tryToDrift;
    public bool keepDrifting;
    private float currentDriftForce;
    private float driftCatchUp;
    private int driftDir;
    public float highDrift;
    public float lowDrift;
    private float driftTurboGauge;
    public float gaugeToActivateTurbo;
    public Transform driftPivot;
    private float nextYDriftRot;
    public float driftCoyoteTime;
    private float driftCoyoteTimer;
    public float tryDriftCoyoteTime;

    [Header("Flight")]
    public bool isFlying;
    public float flightSpeed;
    public Transform flightDir;
    public float maxFlightTurnForce;
    private float currentFlightTurnForce;
    private float inputGlideUpDown;
    public GameObject gliderGO;
    // visual flight
    private float visualFlightRotSpeedZ;
    [Header("Wind")]
    float currentTargetWindForce;
    float currentWindForce;
    Vector3 currentWindDir;

    [Header("Respawn Points")]
    public List<Transform> respawnPoints;
    public Transform[] respawnPointsArr;
    public List<Transform> activeRespawnPoints;
    public bool outOfBounds;
    private Vector3 currentRespawnPosition;
    private Quaternion currentRespawnRotation;
    private Transform startRespawnPoint;
    public GameObject winText;
    private float raceTimer;
    public float lastTurnTime;
    // MATHIS

    [Header("Checkpoint")]
    [SerializeField] private CheckpointManager checkPointManager;

    [Header("Respawn")]
    [SerializeField] private bool canDrive = true;

    private float respawnCooldown = 0f;
    private Vector3 startPosition;
    private Quaternion startRotation;

    [Header("Ghost")]
    [SerializeField] private bool ghostMode = false;
    [SerializeField] private SplineContainer raceSpline;
    [Range(0, 1)] private float splineProgress = 0f;
    [SerializeField] private float ghostSpeed = 15f; // Vitesse cible du ghost
    [SerializeField] private float lookAheadDistance = 0.05f; // Distance d'anticipation (0.01 à 0.1)


    public Vector3 StartPosition { get => startPosition; set => startPosition = value; }
    public Quaternion StartRotation { get => startRotation; set => startRotation = value; }
    public bool GhostMode { get => ghostMode; set => ghostMode = value; }
    public bool CanDrive { get => canDrive; set => canDrive = value; }

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

        startPosition = transform.position;
        startRotation = transform.rotation;
        currentMaxTurnSpeed = maxTurnSpeed;
    }

    void Start()
    {
        //Application.targetFrameRate = 20;
        rb = GetComponent<Rigidbody>();
        groundNormal = new Vector3(0, 1, 0);
        activeRespawnPoints = respawnPoints;
        if (GameModes.isMapInverted)
        {
            // On fait faire demi-tour au kart immédiatement
            transform.rotation *= Quaternion.Euler(0, 180, 0);
        }
    }

    void Update()
    {
        if(GhostMode)
        {
            GhostDrive();
            return;
        }
        PlayerInputs();
        HandleDrift();
        HandleSteeringWheel();
    }

    private void FixedUpdate()
    {
        raceTimer += Time.fixedDeltaTime;
        
        if (respawnPointsArr.Length == 0)
        {
            respawnPointsArr = new Transform[respawnPoints.Count];
            respawnPointsArr = respawnPoints.ToArray();
        }

        //HandleRespawn();
        // On gère la physique du kart
        HandleCurrentSpeed();
        HandleTurning();
        HandleTurbo();

        // on gère la force du bounce contre les murs        
        HandleBounceForce();
        HandleWindBlow();
        HandleGravity();

        if (outOfBounds)
        {
            gliderGO.SetActive(false);
            currentSpeed = 0f;
            currentTurboForce = 0f;
            bounceForce = 0f;
            currentFallSpeed = 0f;
            airSpeed = 0f;
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (grounded)
        {
            if (flightSpeed > 0f)
            { 
                currentSpeed = flightSpeed;
                flightSpeed = 0f;
            }

            groundedCoyoteTimer = 0.3f;
            gliderGO.SetActive(false);
            transform.Rotate(0, currentTurnSpeed + currentDriftForce, 0);
            rb.linearVelocity = (preOrientation.transform.forward * (currentSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + currentFallSpeed);
        }
        else if (!isFlying)
        {
            if (groundedCoyoteTimer > 0)
            {
                //Debug.Log(groundedCoyoteTimer);
                groundedCoyoteTimer -= Time.fixedDeltaTime;
                transform.Rotate(0, (currentTurnSpeed + currentDriftForce), 0);
            }
            else transform.Rotate(0, (currentTurnSpeed + currentDriftForce) / 3f, 0);

            rb.linearVelocity = (preOrientation.transform.forward * (airSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + currentFallSpeed);

            if (currentTurboForce <= 0)
            {
                airSpeed -= 5f * Time.fixedDeltaTime;
            }

            if (airSpeed < 0) airSpeed = 0;
        }
        else
        {
            HandleGliderFlight();
        }
    }

    public void StartWindBlow(Vector3 dir, float force, bool fast)
    {
        currentWindDir = dir;
        currentTargetWindForce = force;
        Debug.Log(dir + " " + force);
        if (fast) { currentWindForce = force * 0.85f; }
        //if (flightSpeed < 6f) { flightSpeed = 6f; }
        //flightDir.forward = dir;
    }

    void HandleWindBlow()
    {
        if (!isFlying) { return; }   
        float nextWindForce = currentWindForce;
        //Debug.Log(nextWindForce + " " + currentTargetWindForce);
        if (flightSpeed < 14f) { flightSpeed += 5f * Time.fixedDeltaTime; }
        if (nextWindForce > currentTargetWindForce)
        {
            nextWindForce -= 100f * Time.fixedDeltaTime;
            if (nextWindForce < currentTargetWindForce)
            {
                //Debug.Log(1);
                nextWindForce = currentTargetWindForce;
                currentTargetWindForce = 0f;
            }
            //currentWindForce = Mathf.Lerp(currentWindForce, currentTargetWindForce, 20f * Time.fixedDeltaTime);
        }
        else if (nextWindForce < currentTargetWindForce)
        {
            nextWindForce += 100f * Time.fixedDeltaTime;
            if (nextWindForce > currentTargetWindForce)
            {
                //Debug.Log(2);
                nextWindForce = currentTargetWindForce;
                currentTargetWindForce = 0f;
            }
        }
        else if(nextWindForce == currentTargetWindForce)
            {
            currentTargetWindForce = 0f;
        }
            
            //Debug.Log(nextWindForce);
            currentWindForce = nextWindForce;
        /*else
        {
            currentTargetWindForce = 0f;
        }*/

    }
    private void HandleGliderFlight()
    {
        gliderGO.SetActive(true);

        if (flightDir.eulerAngles.x > 0f && flightDir.eulerAngles.x < 180f)
        {
            if (flightSpeed > 30f)
            {
                flightSpeed = 30f;
            }
        }
        else if (flightDir.eulerAngles.x > 180f && flightDir.eulerAngles.x < 360f)
        {
            flightSpeed += (flightDir.eulerAngles.x - 360f) * 0.25f * Time.fixedDeltaTime;

            if (flightSpeed < 0.8f)
            {
                flightSpeed = 0.8f;
            }
        }

        if (flightSpeed < currentTurboForce)
        {
            flightSpeed = currentTurboForce;
        }

        if (inputGlideUpDown == 0)
        {
            float extraNoseSpeed;

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
            flightDir.Rotate(inputGlideUpDown, 0, 0);
        }

        if (turnDirection != 0f)
        {
            currentFlightTurnForce += (0.1f * turnDirection + maxFlightTurnForce) * turnDirection * Time.fixedDeltaTime;

            if (currentFlightTurnForce < -maxFlightTurnForce)
            {
                currentFlightTurnForce = -maxFlightTurnForce;
            }
            else if (currentFlightTurnForce > maxFlightTurnForce)
            {
                currentFlightTurnForce = maxFlightTurnForce;
            }
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
        rb.linearVelocity = (flightDir.forward * (flightSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + (currentFallSpeed / (1f + flightSpeed / 2.5f)) * 1.2f) + currentWindDir * currentWindForce;
    }

    private void LateUpdate()
    {
        // on gère les visuels du kart
        HandleVisualKartBody();
        HandleVisualKartWheels();

        SquishAnimation();
        HandleSmoke();
        HandleCameraTransform();
    }

    void PlayerInputs()
    {
        if (outOfBounds)
        {
            return;
        }

        if (GhostMode == true)
        {
            GhostDrive();
            return;
        }

        if (!CanDrive)
        {
            forwardDirection = 0f;
            turnDirection = 0f;
            return;
        }

        forwardDirection = InputSystemHandler.instance.inputForwardDir;
        turnDirection = Mathf.Clamp( InputSystemHandler.instance.inputTurnDir, -1, 1);
        tryToDrift = InputSystemHandler.instance.inputTryDrift;
        keepDrifting = InputSystemHandler.instance.inputDrift;

        inputGlideUpDown = InputSystemHandler.instance.inputGlideUpDownDir;
        inputGlideTurn = InputSystemHandler.instance.inputGlideTurnDir;
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

    public void StopFlight()
    {
        isFlying = false;
        gliderGO.SetActive(false);
        flightSpeed = 0f;
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
        if (grounded)
        {
            currentFallSpeed = 0;
        }
        else if (currentFallSpeed < 32f)
        {
            currentFallSpeed += gravity * Time.fixedDeltaTime;
        }
    }
    bool IsGrounded()
    {
        if (Physics.Raycast(groundRayOrigin.position, Vector3.down, out RaycastHit hit, 0.25f))
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
            float breakForce = 0;
            if (currentSpeed > 0f)
            {
                breakForce = 0.1f;
            }
            else breakForce = 0f;
            nextSpeed -= flatAccelSpeed + breakForce + (maxSpeed - currentSpeed) * accelSpeed * Time.fixedDeltaTime;
        }
        else // si on ne veut pas accelerer
        {
            if (currentSpeed > 0.0f) // si on avance
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
        if (tryToDrift) //if (tryToDrift && grounded)
        {
            tryDriftCoyoteTime = 0.4f;
            if (groundedCoyoteTimer > 0f && tryDriftCoyoteTime > 0f)
            {
                if (turnDirection > 0) //if (currentTurnSpeed > 0.05f && turnDirection > 0)
                {
                    driftDir = 1;

                    for (int i = 0; i < driftParticlesGenerators.Length; i++)
                    {
                        driftParticlesGenerators[i].gameObject.SetActive(true);
                        //Debug.Log("SET ACTIVE");
                    }
                }
                else if (turnDirection < 0)//(currentTurnSpeed < -0.05f && turnDirection < 0)
                {
                    driftDir = -1;

                    for (int i = 0; i < driftParticlesGenerators.Length; i++)
                    {
                        driftParticlesGenerators[i].gameObject.SetActive(true);
                        //Debug.Log("SET ACTIVE");
                    }
                }
            }
        }
        if (tryDriftCoyoteTime > 0)
        {
            tryDriftCoyoteTime -= Time.deltaTime;
            //Debug.Log("Coyote" + tryDriftCoyoteTime);
        }

        //ca mem
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! faux drift
        if (keepDrifting && grounded && driftDir != 0)
        {
            // on fait monter ou descendre la rotation Y vers targetYRot
            float targetYRot = (driftDir + turnDirection) * 15f; // correspond au tournant max du drift

            if (nextYDriftRot < targetYRot)
            {
                nextYDriftRot += 10f * Time.fixedDeltaTime;
                //nextYDriftRot += (nextYDriftRot + (targetYRot - nextYDriftRot)) * Time.fixedDeltaTime;
                if (nextYDriftRot > targetYRot) { nextYDriftRot = targetYRot; } // on dépasse pas targetYRot
            }
            else if (nextYDriftRot > targetYRot)
            {
                nextYDriftRot += -10f * Time.fixedDeltaTime;
                //nextYDriftRot -= (nextYDriftRot - (targetYRot + nextYDriftRot)) * Time.fixedDeltaTime;
                if (nextYDriftRot < targetYRot) { nextYDriftRot = targetYRot; } // on dépasse pas targetYRot
            }

            driftPivot.localRotation = Quaternion.Euler(0, nextYDriftRot, 0);

            //oldKeepD = keepDrifting;
            driftCoyoteTime = 0.3f;
        }
        else // quand on lache le drift
        {
             if (driftCoyoteTime > 0f)
             {
                 driftCoyoteTime -= Time.deltaTime;
                 return;
             }                   
            /*if (nextYDriftRot < 0)
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

            driftPivot.localRotation = Quaternion.Euler(0, nextYDriftRot, 0);*/
            nextYDriftRot = IncrementTowardsValue(nextYDriftRot, 0, 12f * Time.fixedDeltaTime);
            driftPivot.localRotation = Quaternion.Euler(0, nextYDriftRot, 0);

            currentDriftForce = 0;
            driftCatchUp = 0;

            if (driftTurboGauge > gaugeToActivateTurbo)
            {
                StartTurbo(driftTurboGauge * 2.2f, driftTurboGauge / 2.6f);
                driftTurboGauge = 0;
                Vector3 oldCamForward = camPivot.forward;
                transform.forward = new Vector3(driftPivot.forward.x, 0, driftPivot.forward.z);
                driftPivot.forward = transform.forward;
                nextYDriftRot = 0;
                camPivot.forward = oldCamForward;

            }

            driftDir = 0;

            for (int i = 0; i < fireWheelEffects.Length; i++)
            {
                fireWheelEffects[i].SetActive(false);
                fireWheelEffects[i].transform.localScale = new Vector3(0.3f, 0.04f, 0.3f);
            }

            for (int i = 0; i < driftParticlesGenerators.Length; i++)
            {
                driftParticlesGenerators[i].gameObject.SetActive(false);
               // Debug.Log("DEACTIVATE");
            }

            //return; 
        }

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
        }
       
        if (driftTurboGauge > gaugeToActivateTurbo)
        {
            for (int i = 0; i < fireWheelEffects.Length; i++)
            {
                fireWheelEffects[i].SetActive(true);
                float fireWheelSize = Mathf.Clamp(1 + driftTurboGauge / 6, 0.0042f, 0.01f);
                fireWheelEffects[i].transform.localScale = new Vector3(fireWheelSize, 0.001f, fireWheelSize);
            }
        }

        if (driftDir != 0)
        {
            float nextDriftForceTarget = 1f;

            if (driftDir > 0 && turnDirection < 0)
            {
                nextDriftForceTarget = 2.5f;
                driftTurboGauge += 0.2f * Time.deltaTime;
            }
            else if (driftDir < 0 && turnDirection > 0)
            {
                nextDriftForceTarget = 2.5f;
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
                driftCatchUp += 4f * Time.deltaTime;
            }
            else if (driftCatchUp > nextDriftForceTarget)
            {
                driftCatchUp -= 4f * Time.deltaTime;
            }

            currentDriftForce = driftDir * driftCatchUp * 0.8f;
        }
    }

    void HandleTurning()
    {
        float nextTurnSpeed = currentTurnSpeed;
        Debug.Log("1      " + nextTurnSpeed);
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
            /*if (currentTurnSpeed > 0)
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
            }*/
            
            //nextTurnSpeed = //IncrementTowardsValue(nextTurnSpeed, 0, turnDecelSpeed * Time.fixedDeltaTime);
            Debug.Log("2      " + nextTurnSpeed);
            //Debug.Log(nextTurnSpeed);
        }

        currentMaxTurnSpeed = Mathf.Abs(turnDirection * maxTurnSpeed);
        // on clamp
        if (nextTurnSpeed > currentMaxTurnSpeed)
        {
            nextTurnSpeed = currentMaxTurnSpeed;
        }
        else if (nextTurnSpeed < -currentMaxTurnSpeed)
        {
            nextTurnSpeed = -currentMaxTurnSpeed;
        }
        
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
    }

    void HandleTurbo()
    {
        if (turbo)
        {
            turboTimer -= Time.fixedDeltaTime;
            if (turboTimer > 0)
            {
                float nextTForce = currentTurboForce + turboAccelSpeed + targetTurboForce * Time.fixedDeltaTime;
                currentTurboForce = nextTForce < targetTurboForce ? nextTForce : targetTurboForce;
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

    void HandleVisualKartBody()
    {

        if (isFlying && !grounded)
        {
            visualKartBody.transform.forward = flightDir.forward;
            visualKartBody.transform.localEulerAngles = new Vector3(visualKartBody.transform.localEulerAngles.x, visualKartBody.transform.localEulerAngles.y, -currentFlightTurnForce * 32f);
            return;
        }
        if (currentSpeed > 0)
        {
            visKartXRotCatchUp = forwardDirection > 0 ? (maxSpeed - currentSpeed) / 6f : -(maxSpeed - currentSpeed) / 6f;
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
        preOrientation.localRotation = Quaternion.RotateTowards(preOrientation.localRotation, groundNormalT.localRotation, 120f * Time.deltaTime);
        Quaternion rotTarget = Quaternion.Euler(nextTotalSpeed, 0, visKartZRot);
        visualKartBody.transform.localRotation = Quaternion.RotateTowards(visualKartBody.transform.localRotation, rotTarget, 40f * Time.deltaTime);

        cocot.localRotation = Quaternion.RotateTowards(cocot.localRotation, Quaternion.Euler(nextTotalSpeed * 0.6f - 90, visKartZRot * 1.2f, 0), 120f * Time.deltaTime);
        creteAccelZ += Mathf.Abs(visKartZRot);
        creteAccelZ = Mathf.Clamp(creteAccelZ, 0, 3f);
        creteAccelX += Mathf.Abs(nextTotalSpeed);
        creteAccelX = Mathf.Clamp(creteAccelX, 0, 3f);
        for (int i = 0; i < cretes.Length; i++)
        {
            cretes[i].localRotation = Quaternion.RotateTowards(cretes[i].localRotation, Quaternion.Euler((-currentSpeed * 0.75f + nextTotalSpeed  / 2f) * creteAccelX, -visKartZRot * creteAccelZ * 0.2f, 0f ), 500f * Time.deltaTime);
        }
    }

    void HandleSteeringWheel()
    {
        steeringWheel.localEulerAngles = new Vector3(visWheelsYRot - 90f, 90, -90f);
    }
    void HandleVisualKartWheels()
    {
        if (isFlying)
        {
            visualKartWheelsParent.transform.localRotation = Quaternion.Euler(visualKartBody.transform.localEulerAngles.x, 0, visualKartBody.transform.localEulerAngles.z);
            return;
        }

        visualKartWheelsParent.transform.localRotation = Quaternion.Euler(0, 0, 0);
        turningWheelsXRot += (currentSpeed + currentTurboForce) * turningWheelsRatioScaling * Time.deltaTime;
        nonTurningWheelsXRot += (currentSpeed + currentTurboForce) * nonTurningWheelsRatioScaling * Time.deltaTime;

        for (int i = 0; i < turningWheels.Length; i++)
        {
            turningWheels[i].transform.localRotation = Quaternion.Euler(turningWheelsXRot, visWheelsYRot, 0);
        }
        for (int i = 0; i < nonTurningWheels.Length; i++)
        {
            nonTurningWheels[i].transform.localRotation = Quaternion.Euler(nonTurningWheelsXRot, 0, 0);
        }
    }

    float IncrementTowardsValue(float currentValue, float targetValue, float increment)
    {
        //Debug.Log(currentValue + " " + targetValue + " " + increment);
        if (currentValue > targetValue)
        {
            //Debug.Log("plus grang");
            currentValue -= increment;
            if (currentValue < targetValue)
            {
                //Debug.Log("<");
                return targetValue;
            }
        }
        else if (currentValue < targetValue)
        {
            //Debug.Log("plus ptit");
            currentValue += increment;
            if (currentValue > targetValue)
            {
                //Debug.Log(">");
                return targetValue;
            }
        }
        //Debug.Log(currentValue);
        return currentValue;
    }
    void HandleCameraTransform()
    {
        float driftForce = Mathf.Clamp(currentDriftForce, -1f, 1f);

        if (driftForce < 0)
        {
            driftForce = -driftForce;
        }

        float targetX = Mathf.Clamp((currentSpeed * -currentTurnSpeed / 110f * forwardDirection) + (turnDirection * driftForce), -10f, 10f);

        Vector3 targetDir = Vector3.down * cameraZoneUp + (transform.forward + (transform.right * currentTurnSpeed * Mathf.Clamp(currentDriftForce, -1f, 1f) * 0.05f)).normalized;
        float rotSpeed = 0.1f + (camPivot.forward - targetDir).magnitude * 2f; // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        camPivot.forward = Vector3.RotateTowards(camPivot.forward, targetDir, Time.deltaTime, 0.0f);

         if (InputSystemHandler.instance.inputCameraMode)
         {
            currentCamPosCenter = thirdPersonCamPos;
         }
         else
         {
            currentCamPosCenter = firstPersonCamPos;           
         }

        Vector3 rayOrigin = transform.position + transform.forward * 5f + new Vector3 (currentCamPosCenter.z, currentCamPosCenter.y, 0);// + playerCamera.transform.forward * 5f;
        //Debug.Log("ORIGIN" + rayOrigin);
        Vector3 rayDirToCam = playerCamera.transform.position - rayOrigin;
       // Debug.Log("DIRETION" + rayDirToCam);
        Vector3 camCollisionCompensation = Vector3.zero;
        if (Physics.Raycast(rayOrigin, rayDirToCam, out RaycastHit hit, 5f, wallLayer))
        {
            camCollisionCompensation = new Vector3(0, 0, rayDirToCam.x).normalized * (hit.distance - 5f);// - (transform.right).normalized * 5f;
          //  Debug.Log("camCollisionCompensation +  + nextDir");
            //Debug.Log(camCollisionCompensation);
        }

        Vector3 nextDir = playerCamera.transform.localPosition - (currentCamPosCenter + camCollisionCompensation);
        if (nextDir.sqrMagnitude > 0.01f)
        {
          //  Debug.Log(camCollisionCompensation + " " + nextDir);
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, currentCamPosCenter + camCollisionCompensation, 8f * Time.deltaTime);
        }
    }

    public void ReorientKart(Vector3 newDir)
    {
        Quaternion oldYRot = camPivot.transform.rotation;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, newDir.y, transform.eulerAngles.z);
        camPivot.transform.rotation = oldYRot;
    }

    private void OnCollisionEnter(Collision collision)
    {
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
        }
        else if (collision.gameObject.layer == 9)
        {
            //Debug.Log("gogog");
            //transform.position = collision.transform.GetChild(0).transform.position;
            //transform.eulerAngles = collision.transform.GetChild(0).transform.localEulerAngles;
            //transform.rotation = collision.transform.GetChild(0).transform.rotation;
            //transform.position = new Vector3(230.6f, 16, 365.2f);
            //transform.eulerAngles = new Vector3(0, 661.515f, 0);
            outOfBounds = true;
        }

    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == 10)
        {
            cameraZoneUp = 0.32f;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 10)
        {
            cameraZoneUp = 0f;
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
            flightDir.forward = groundNormalT.forward;
        }
    }
    void SquishAnimation()
    {
        if (bounce)
        {
            bounceTimer += Time.deltaTime;

            if (bounceTimer < 0.05f)
            {
                visualKartBody.transform.localScale = visualKartBody.transform.localScale + new Vector3(-6f, 10f, -6f) * Time.deltaTime;
            }
            else if (bounceTimer < 0.1f)
            {
                visualKartBody.transform.localScale = visualKartBody.transform.localScale + new Vector3(6f, -10f, 6f) * Time.deltaTime;
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
            for (int i = 0; fireParticlesGenerator.Length > 0; i++)
            {
                var FPemission = fireParticlesGenerator[i].emission;
                FPemission.rateOverDistance = 3;
            }
        }
        else
        {
            for (int i = 0; fireParticlesGenerator.Length > 0; i++)
            {
                var FPemission = fireParticlesGenerator[i].emission;
                FPemission.rateOverDistance = 0;
            }

        }
    }

    void HandleRespawn()
    {
        if (!outOfBounds)
        {
            for (int i = 0; i < activeRespawnPoints.Count; i++)
            {
                if ((activeRespawnPoints[i].position - transform.position).sqrMagnitude < 50f)
                {
                    currentRespawnPosition = activeRespawnPoints[i].position;
                    //Debug.Log(currentRespawnPosition);
                    currentRespawnRotation = activeRespawnPoints[i].rotation;
                    if (startRespawnPoint == null) startRespawnPoint = activeRespawnPoints[i];
                    activeRespawnPoints.RemoveAt(i);
                    i = activeRespawnPoints.Count;

                }
            }
            // Place Holder Respawner
            if (transform.position.y < 11f)
            {
                outOfBounds = true;
                Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                //transform.position = new Vector3(106.9f, 16, 151.6f);
                //transform.eulerAngles = new Vector3(0, 585.413f, 0);
                //transform.position = currentRespawnPosition;
                //transform.rotation = currentRespawnRotation;
            }
            if (activeRespawnPoints.Count == 0 && (startRespawnPoint.position - transform.position).sqrMagnitude < 50f)
            {
                Debug.Log("GAME WOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOON");
                lastTurnTime = raceTimer;
                raceTimer = 0;
                winText.SetActive(true);
                
                for (int i = 0; i < respawnPointsArr.Length; i++)
                {
                    activeRespawnPoints.Add(respawnPointsArr[i]);
                }
                //activeRespawnPoints = respawnPointsArr.ToList<Transform>();
            }
        }
        else
        {
            /*if (transform.position.y < currentRespawnPosition.y + 3f)
            {
                transform.position += new Vector3(0,);
            }*/
            GetComponent<SphereCollider>().enabled = false;
            Vector3 dir = currentRespawnPosition - transform.position;
            //float dirMagn = dir.magnitude;

            float upForce = Mathf.Clamp(dir.magnitude, 0f, 15f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, currentRespawnRotation, Mathf.Clamp(upForce / 8f, 1f, 5f));
            transform.position += (dir.normalized * 8f + dir + new Vector3(0, upForce, 0)) * Time.fixedDeltaTime;
            if (dir.sqrMagnitude < 0.1f)
            {
                GetComponent<SphereCollider>().enabled = true;
                outOfBounds = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(groundRayOrigin.position, Vector3.down * 0.5f);
        Gizmos.DrawRay(transform.position + transform.forward * 5f + new Vector3(currentCamPosCenter.z, currentCamPosCenter.y, 0), playerCamera.transform.position - transform.position + transform.forward * 5f + new Vector3(currentCamPosCenter.z, currentCamPosCenter.y, 0));
    }

    void GhostDrive()
    {
        if (raceSpline == null)
        {
            Debug.LogWarning("RaceSpline manquante sur le Kart !");
            return;
        }

        // 1. GESTION DU PROGRÈS (Avancement sur la ligne)
        // La vitesse est divisée par la longueur de la spline pour rester cohérent
        float splineLength = raceSpline.CalculateLength();
        float progressStep = (ghostSpeed / splineLength) * Time.deltaTime;

        if (GameModes.isMapInverted)
            splineProgress -= progressStep;
        else
            splineProgress += progressStep;

        // Boucle le progrès pour que le ghost continue après un tour
        splineProgress = Mathf.Repeat(splineProgress, 1f);

        // 2. CALCUL DE LA CIBLE
        // On cherche un point un peu plus loin sur la spline pour "anticiper" le virage
        float targetProgress;
        if (GameModes.isMapInverted)
            targetProgress = Mathf.Repeat(splineProgress - lookAheadDistance, 1f);
        else
            targetProgress = Mathf.Repeat(splineProgress + lookAheadDistance, 1f);

        Vector3 targetPosition = (Vector3)raceSpline.EvaluatePosition(targetProgress);
        Vector3 directionToTarget = targetPosition - transform.position;

        // 3. LOGIQUE DE DIRECTION (Basée sur ton système actuel)
        float angle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

        // On adoucit la rotation pour éviter les coups de volant secs
        turnDirection = Mathf.Clamp(angle / 20f, -1f, 1f);

        // Gaz à fond !
        accelerate = true;
        forwardDirection = 1f;
    }
}

    /*void GhostDrive()
    {
        if (currentWaypoint == null)
        {
            currentWaypoint = firstWaypoint;
            return;
        }

        Vector3 dir = currentWaypoint.position - transform.position;

        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        Debug.Log(angle);
        if (angle < -5f)
        {
            turnDirection = -1;
            inputGlideTurn = -1;
        }
        else if (angle > 5f)
        {
            turnDirection = 1;
            inputGlideTurn = 1;
        }
        else
        {
            turnDirection = 0;
            inputGlideTurn = 0;
        }

        //turnDirection = Mathf.Clamp(angle / 30f, -1f, 1f);
        if (!isFlying) forwardDirection = 1f;
        else forwardDirection = 0f;

        if (dir.sqrMagnitude < 70f)
        {
            Debug.Log(currentWaypoint);
            currentWaypoint = currentWaypoint.GetComponent<Waypoints>().nextWaypoint;
            Debug.Log(currentWaypoint);
        }
    }*/

