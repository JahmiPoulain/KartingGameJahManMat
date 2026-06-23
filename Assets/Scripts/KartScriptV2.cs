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
    public Transform camPivot2;
    [SerializeField] Transform emptyCamSpot;
    public Vector3 thirdPersonCamPos;
    public Vector3 firstPersonCamPos;
    private Vector3 currentCamPosCenter;
    private float turboTimer;
    float cameraZoneUp;
    float camPivotZ;
    float camPivotY;

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

    [SerializeField] Transform[] firstPersonInvisible;

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
    [SerializeField] bool grounded;
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
    [SerializeField] GliderAnimation glider;
    // visual flight
    private float visualFlightRotSpeedZ;
    [Header("Wind")]
    float currentTargetWindForce;
    float currentWindForce;
    Vector3 currentWindDir;
    [SerializeField] Transform normalModeWindTarget;
    [SerializeField] Transform reverseModeWindTarget;

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

    int outOfBoundsFrames;
    [Header("Ghost & Spline Settings")]
    [SerializeField] private bool ghostMode = false;
    [SerializeField] private SplineContainer raceSpline;
    [SerializeField] [Range(0, 1)] private float splineProgress = 0f;
    [SerializeField] private float ghostSpeed = 15f;
    [SerializeField] [Range(0.005f, 0.05f)] private float lookAheadDistance = 0.01f; // Commence à 0.01 (1% de la piste)
    [SerializeField] private bool isGhost = false;


    [Header("SFX")]
    [SerializeField] private AudioClip bubbleSound; // Ton son de bulle actuel
    [SerializeField] private AudioClip driftSound; // Ton son de bulle actuel
    [SerializeField] private AudioSource audioSourceMotor; 
    [SerializeField] private AudioSource audioSourceDrift; 
    [SerializeField] private AudioSource audioSourceBounce; 
    private float bubbleSoundTimer; // Pour gérer la cadence des bruitages


    public Vector3 StartPosition { get => startPosition; set => startPosition = value; }
    public Quaternion StartRotation { get => startRotation; set => startRotation = value; }
    public bool GhostMode { get => ghostMode; set => ghostMode = value; }
    public bool CanDrive { get => canDrive; set => canDrive = value; }
    public float SplineProgress { get => splineProgress; set => splineProgress = value; }
    public bool IsGhost { get => isGhost; set => isGhost = value; }
    public GliderAnimation Glider { get => glider; set => glider = value; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            if (!IsGhost) // ← le ghost ne se détruit pas
                Destroy(gameObject);
        }



        controls = new InputSystem_Actions(); // initialiser input    

        audioSourceMotor.clip = bubbleSound;
        audioSourceMotor.loop = false; // Important : on veut entendre chaque bulle éclater individuellement
        audioSourceMotor.playOnAwake = false;

        startPosition = transform.position;
        startRotation = transform.rotation;
        currentMaxTurnSpeed = maxTurnSpeed;
    }

    void Start()
    {
        //Application.targetFrameRate = 20;
        rb = GetComponent<Rigidbody>();
        groundNormal = new Vector3(0, 1, 0);
        CanDrive = false;
        activeRespawnPoints = respawnPoints;
        if (InversionCatcher.instance.Inverted)
        {
            // On fait faire demi-tour au kart immédiatement
            transform.rotation *= Quaternion.Euler(0, 180, 0);
        }
        if (IsGhost)
        {
            // 1. Désactive tous les AudioSources pour le rendre totalement silencieux
            AudioSource[] sources = GetComponentsInChildren<AudioSource>();
            foreach (AudioSource src in sources)
            {
                src.enabled = false;
            }

            // 2. Rend le fantôme intangible (il passe à travers tout)
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Rigidbody ghostRb = GetComponent<Rigidbody>();
            if (ghostRb != null) ghostRb.isKinematic = true; // Empêche la physique de le pousser
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.R)) { Debug.Break(); }
#endif
        //Debug.Log(CanDrive);
        if (CanDrive == false)
        {
            accelerate = false;
            forwardDirection = 0f;
            turnDirection = 0f;
            inputGlideTurn = 0f;
            return; // On quitte l'Update immédiatement, le joueur ne peut rien faire !
        }
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
        if (outOfBounds)
        {
            grounded = false;
        }
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

        CheckIfOutOfBounds();

        if (outOfBounds)
        {
            Glider.activate = false;
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
            Glider.activate = false;
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
        Transform windTarget = reverseModeWindTarget;
        if (normalModeWindTarget.transform.root.gameObject.activeSelf)
        {
            windTarget = normalModeWindTarget;
            Debug.Log("ACTIVE");
            if (currentWindForce <= 0 || transform.position.x < normalModeWindTarget.position.x) return;
        }
        else if (currentWindForce <= 0) return;
        Vector3 goalDir = (windTarget.position - transform.position).normalized;
        float dot = Vector3.Dot(new Vector3(goalDir.x, 0, goalDir.z), new Vector3(transform.right.x, 0, transform.right.z));
        //Debug.Log(dot);
        if (dot > 0)
        { 
            transform.localEulerAngles += new Vector3(0, currentWindForce * 2f * Time.fixedDeltaTime, 0);
        }
        else if (dot < 0)
        {
            transform.localEulerAngles -= new Vector3(0, currentWindForce * 2f * Time.fixedDeltaTime, 0);
        }
        

    }
    private void HandleGliderFlight()
    {
        Glider.activate = true;

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
        //Debug.Log(flightDir.localEulerAngles);
        currentFlightTurnForce = 0f;
        //flightDir.transform.rotation = groundNormalT.transform.rotation;//visualKartBody.transform.localEulerAngles;//new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z);
        cocot.localEulerAngles = new Vector3(-90,0,0);
        for (int i = 0; i < cretes.Length; i++)
        {
            cretes[i].localEulerAngles = Vector3.zero;
        }
        for (int i = 0; i < turningWheels.Length; i++)
        {
            turningWheels[i].transform.localEulerAngles = new Vector3(90,0,0);
        }
    }

    public void StopFlight()
    {
        isFlying = false;
        Glider.activate = false;
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
            //nextYDriftRot = IncrementTowardsValue(nextYDriftRot, 0, 12f * Time.fixedDeltaTime);
            //driftPivot.localRotation = Quaternion.Euler(0, nextYDriftRot, 0);

            currentDriftForce = 0;
            driftCatchUp = 0;

            if (driftTurboGauge > gaugeToActivateTurbo)
            {
                StartTurbo(driftTurboGauge * 2.3f, driftTurboGauge / 2.6f);
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
                driftTurboGauge += 0.25f * Time.deltaTime;
            }
            else if (driftDir < 0 && turnDirection > 0)
            {
                nextDriftForceTarget = 2.5f;
                driftTurboGauge += 0.25f * Time.deltaTime;
            }
            else if (driftDir > 0 && turnDirection > 0)
            {
                driftTurboGauge += 2.5f * Time.deltaTime;
            }
            else if (driftDir < 0 && turnDirection < 0)
            {
                driftTurboGauge += 2.5f * Time.deltaTime;
            }
            else
            {
                driftTurboGauge += 1f * Time.deltaTime;
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
        if (audioSourceDrift != null && driftSound != null)
        {
            // Si driftDir est différent de 0, le kart dérape activement
            if (driftDir != 0)
            {
                if (!audioSourceDrift.isPlaying)
                {
                    audioSourceDrift.clip = driftSound;
                    audioSourceDrift.loop = true;
                    audioSourceDrift.Play();
                }


                float targetPitch = Mathf.Lerp(0.9f, 1.4f, driftTurboGauge / gaugeToActivateTurbo);


                audioSourceDrift.pitch = Mathf.Lerp(audioSourceDrift.pitch, targetPitch, 5f * Time.deltaTime);


                float targetVolume = Mathf.Lerp(0.7f, 1.0f, driftTurboGauge / gaugeToActivateTurbo);
                audioSourceDrift.volume = Mathf.Lerp(audioSourceDrift.volume, targetVolume, 5f * Time.deltaTime) / 1.75f;
            }
            else
            {

                if (audioSourceDrift.isPlaying)
                {

                    audioSourceDrift.pitch = 1f;
                    //audioSourceDrift.volume = 1f;
                    audioSourceDrift.Stop();
                }
            }
        }
    }



    void HandleTurning()
    {
        float nextTurnSpeed = currentTurnSpeed;
       // Debug.Log("1      " + nextTurnSpeed);
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
           // Debug.Log("2      " + nextTurnSpeed);
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
        //Debug.Log(force);
        currentSpeed = maxSpeed;
        force = Mathf.Clamp(force, 0f, 25f);
        time = Mathf.Clamp(time, 0f, 3f);
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
       /* if (!InputSystemHandler.instance.inputCameraMode)
        {
            visualKartBody.transform.forward = transform.forward;
            return; 
        }*/
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
        groundNormalT.transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, groundNormal), groundNormal); // oriente le y vers le haut de la normale et le x vers l'avant du kart ( 2 semaines de galère avant meme les cours sur le produit vectoriel)
        preOrientation.localRotation = Quaternion.RotateTowards(preOrientation.localRotation, groundNormalT.localRotation, 120f * Time.deltaTime);
        Quaternion rotTarget = Quaternion.Euler(nextTotalSpeed * 0.8f, 0, visKartZRot);
        if (!InputSystemHandler.instance.inputCameraMode)
        {
            rotTarget = Quaternion.Euler(Vector3.zero);            
        }
        visualKartBody.transform.localRotation = Quaternion.RotateTowards(visualKartBody.transform.localRotation, rotTarget, 40f * Time.deltaTime);

        cocot.localRotation = Quaternion.Slerp(cocot.localRotation, Quaternion.Euler(nextTotalSpeed * 0.3f - 90, visKartZRot * 1.2f, 0), 0.5f);
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

    /* float IncrementTowardsValue(float currentValue, float targetValue, float increment)
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
     }*/
    void HandleCameraTransform()
    {
        float camPivotX = visualKartBody.transform.localEulerAngles.x;
        if (InputSystemHandler.instance.inputCameraMode)
        {
            currentCamPosCenter = thirdPersonCamPos;
            camPivotX = 0f;
        }
        else
        {
            currentCamPosCenter = firstPersonCamPos;
        }
        
        float driftForce = Mathf.Clamp(currentDriftForce, -1f, 1f);

        if (driftForce < 0)
        {
            driftForce = -driftForce;
        }

        float targetX = Mathf.Clamp((currentSpeed * -currentTurnSpeed / 110f * forwardDirection) + (turnDirection * driftForce), -10f, 10f);

        Vector3 targetDir = Vector3.down * cameraZoneUp + (transform.forward + (transform.right * currentTurnSpeed * Mathf.Clamp(currentDriftForce, -1f, 1f) * 0.05f)).normalized;
        float rotSpeed = 0.1f + (camPivot.forward - targetDir).magnitude * 2f; // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        float camPivYSpeed = 30f;
       // if (camPivotY > 0)
       // {
            camPivYSpeed = camPivYSpeed - Mathf.Abs(camPivotY);
        //}
       // else if (camPivotY < 0)
       // {
        //    camPivYSpeed = camPivYSpeed + camPivotY;
        //}

        if (!isFlying)
        {
            if (InputSystemHandler.instance.inputCameraMode)
            {
                // faire tanguer le pivot avec les virages
                camPivot.forward = Vector3.RotateTowards(camPivot.forward, targetDir, Time.deltaTime, 0.0f);

                float multTurnSpeed = currentTurnSpeed * 16f;


                if (camPivotZ < currentTurnSpeed && driftDir == 0) { camPivotZ += (2f + (3f - currentTurnSpeed)) * Time.deltaTime; }
                else if (camPivotZ > currentTurnSpeed && driftDir == 0) { camPivotZ -= (2f + (3f - currentTurnSpeed)) * Time.deltaTime; }
                else if (driftDir < 0) { camPivotZ -= (2f + (3f - camPivotZ)) * Time.deltaTime; }
                else if (driftDir > 0) { camPivotZ += (2f + (3f - camPivotZ)) * Time.deltaTime; }

                camPivotZ = Mathf.Clamp(camPivotZ, -3f, 3f);

                //Debug.Log(multTurnSpeed);
                // faire tourner en y la cam pendant virages
                if (camPivotY < multTurnSpeed && driftDir == 0)
                {
                    camPivotY += (camPivYSpeed) * Time.deltaTime;
                    if (camPivotY > multTurnSpeed) camPivotY = multTurnSpeed;
                }
                else if (camPivotY > multTurnSpeed && driftDir == 0)
                {
                    camPivotY -= (camPivYSpeed) * Time.deltaTime;
                    if (camPivotY < multTurnSpeed) camPivotY = multTurnSpeed;
                }
                else if (driftDir < 0) { camPivotY -= (camPivYSpeed) * Time.deltaTime; }
                else if (driftDir > 0) { camPivotY += (camPivYSpeed) * Time.deltaTime; }
                /*else
                {
                    if (camPivotY > 0)
                    { camPivotY -= (2f + (3f - camPivotY)) * Time.deltaTime; if (camPivotY < 0) camPivotY = 0; }
                    else if (camPivotY < 0)
                    { camPivotY += (2f + (3f - camPivotY)) * Time.deltaTime; if (camPivotY > 0) camPivotY = 0; }
                  
                }*/
                //camPivotY *= -5f;
                //camPivotY = - camPivotY;
                camPivotY = Mathf.Clamp(camPivotY, -10f, 10f);

                if (driftDir == 0 && currentDriftForce != 0)
                {
                    if (camPivotY < camPivot2.localEulerAngles.y) { camPivotY += (camPivYSpeed) * Time.deltaTime; if (camPivotY >= camPivot2.localEulerAngles.y) camPivotY = camPivot2.localEulerAngles.y; }
                    else if (camPivotY > camPivot2.localEulerAngles.y) { camPivotY -= (camPivYSpeed) * Time.deltaTime; if (camPivotY <= camPivot2.localEulerAngles.y) camPivotY = camPivot2.localEulerAngles.y; }
                }
            }
            else
            {
               // Debug.Log("     ELSE     " + camPivotY);
                camPivot.forward = Vector3.RotateTowards(camPivot.forward, transform.forward, Time.deltaTime, 0.0f);
                if (camPivotY < 0f)
                {
                    camPivotY += (camPivYSpeed) * Time.deltaTime;
                    if (camPivotY >= 0f) camPivotY = 0f;
                }
                else if (camPivotY > 0f)
                {
                    camPivotY -= (camPivYSpeed) * Time.deltaTime;
                    if (camPivotY <= 0f) camPivotY = 0f;
                }

                if (camPivotZ < 0f)
                {
                    camPivotZ += 10f * Time.deltaTime;
                    if (camPivotZ >= 0f) camPivotZ = 0f;
                }
                else if (camPivotZ > 0f)
                {
                    camPivotZ -= 10f * Time.deltaTime;
                    if (camPivotZ <= 0f) camPivotZ = 0f;
                }
            }
        }
        else
        {
            camPivot.forward = Vector3.RotateTowards(camPivot.forward, transform.forward, Time.deltaTime, 0.0f);
            // si on vole on annule tout
            // en 3eme personne
            if (camPivotY < 0f)
            {
                camPivotY += (camPivYSpeed) * Time.deltaTime;
                if (camPivotY >= 0f) camPivotY = 0f;
            }
            else if (camPivotY > 0f)
            {
                camPivotY -= (camPivYSpeed) * Time.deltaTime;
                if (camPivotY <= 0f) camPivotY = 0f;
            }

            //Debug.Log(camPivotY + " into 3eme pers");
            // en premiere
            if (!InputSystemHandler.instance.inputCameraMode)
            {
                // Debug.Log(camPivotZ + " into 2" + visualKartBody.transform.localEulerAngles.z);
                if (camPivotZ < visualKartBody.transform.localEulerAngles.z) { camPivotZ += (3f + (3f - camPivotZ)) * Time.deltaTime; if (camPivotZ >= visualKartBody.transform.localEulerAngles.z) camPivotZ = visualKartBody.transform.localEulerAngles.z; }
                else if (camPivotZ > visualKartBody.transform.localEulerAngles.z) { camPivotZ -= (3f + (3f - camPivotZ)) * Time.deltaTime; if (camPivotZ <= visualKartBody.transform.localEulerAngles.z) camPivotZ = visualKartBody.transform.localEulerAngles.z; }
            }
            else// en 3eme personne
            {

                if (camPivotZ < 0) { camPivotZ += (3f + (3f - camPivotZ)) * Time.deltaTime; if (camPivotZ >= 0f) camPivotZ = 0f; }
                else if (camPivotZ > 0) { camPivotZ -= (3f + (3f - camPivotZ)) * Time.deltaTime; if (camPivotZ <= 0f) camPivotZ = 0f; }
            }
        }

        camPivot2.localEulerAngles = new Vector3(camPivotX, -camPivotY, camPivotZ);

        
        Vector3 rayOrigin = transform.position + transform.forward * 5f + new Vector3 (currentCamPosCenter.z, currentCamPosCenter.y, 0);

        Vector3 rayDirToCam = playerCamera.transform.position - rayOrigin;

        Vector3 camCollisionCompensation = Vector3.zero;
        if (Physics.Raycast(rayOrigin, rayDirToCam, out RaycastHit hit, 5f, wallLayer))
        {
            camCollisionCompensation = new Vector3(0, 0, rayDirToCam.x).normalized * (hit.distance - 5f);
        }

        if (!InputSystemHandler.instance.inputCameraMode) camCollisionCompensation = Vector3.zero;

        Vector3 nextDir = playerCamera.transform.localPosition - (currentCamPosCenter + camCollisionCompensation);

        if (nextDir.sqrMagnitude > 0.005f)
        {
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, currentCamPosCenter + camCollisionCompensation, 8f * Time.deltaTime);
            if (InputSystemHandler.instance.inputCameraMode && !firstPersonInvisible[0].gameObject.activeSelf)
            {
                for (int i = 0; i < firstPersonInvisible.Length; i++)
                {
                    firstPersonInvisible[i].gameObject.SetActive(true);
                }
            }
        }
        else
        {
            playerCamera.transform.localPosition = currentCamPosCenter + camCollisionCompensation;
            if (!InputSystemHandler.instance.inputCameraMode && firstPersonInvisible[0].gameObject.activeSelf)
            {
                for (int i = 0; i < firstPersonInvisible.Length; i++) 
                {
                    firstPersonInvisible[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void ReorientKart(Vector3 newDir)
    {
        Quaternion oldYRot = camPivot.transform.rotation;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, newDir.y, transform.eulerAngles.z);
        camPivot.transform.rotation = oldYRot;
    }

    void CheckIfOutOfBounds()
    {
        
        if (outOfBoundsFrames > 5)
        {
            StopFlight();

            outOfBounds = true;
            outOfBoundsFrames = 0;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        //isFlying = false;

        if (collision.gameObject.layer == 6)
        {
            StopFlight();
            bounce = true;
            Vector3 rawDir = transform.position - collision.contacts[0].point;
            bounceDirection = new Vector3(rawDir.x, 0, rawDir.z).normalized;
            float unsignedCurSpeed = currentSpeed;

            if (unsignedCurSpeed < 0)
            {
                unsignedCurSpeed = -unsignedCurSpeed;
            }

            bounceForce = Mathf.Clamp(unsignedCurSpeed * 3f, 25f, unsignedCurSpeed);
            currentSpeed *= 0.2f;
            currentTurboForce *= 0.2f;
        }
        else if (collision.gameObject.layer == 7)
        {
            StopFlight();
        }
        //else if (//collision.gameObject.layer == 9)
       // {            
        //    StopFlight();
           
        //    outOfBounds = true;
       // }

    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == 10)
        {
            cameraZoneUp = 0.20f;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 10)
        {
            cameraZoneUp = 0f;
        }
        outOfBoundsFrames = 0;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == 7)
        {
            grounded = true;
            groundNormal = collision.contacts[0].normal; // l'orientation du kart visuel
            outOfBoundsFrames = 0;
        }
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

            bounceForce = Mathf.Clamp(unsignedCurSpeed * 3f, 25f, unsignedCurSpeed);
            currentSpeed *= 0.2f;

            outOfBoundsFrames++;
        }
        if (collision.gameObject.layer == 9)
        {
            outOfBoundsFrames++;
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
        for (int i = 0; i < smokeParticlesGenerator.Length; i++)
        {
            smokeParticlesGenerator[i].startSpeed = (-(currentSpeed - maxSpeed) - maxSpeed * 0.5f) / 3f -2f; //(currentSpeed - (currentSpeed / 2f));
            var FPemission =  smokeParticlesGenerator[i].emission;
            FPemission.rateOverTime = 1.5f + currentSpeed / 4f;
            //smokeParticlesGenerator[i].velocityOverLifetime. //= new Vector3(0, 0, currentSpeed);
            //smokeParticlesGenerator[i].emission.rateOverDistance = currentSpeed;
        }

        if (bubbleSound != null && !outOfBounds)
        {
  
            float speedRatio = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);


            float bubbleDelay = Mathf.Lerp(0.4f, 0.05f, speedRatio);

            bubbleSoundTimer -= Time.deltaTime;

            if (bubbleSoundTimer <= 0f)
            {

                audioSourceMotor.pitch = Random.Range(0.60f, 0.70f);


                audioSourceMotor.volume = Mathf.Lerp(0.01f, 0.03f, speedRatio) / 1.7f;


                audioSourceMotor.PlayOneShot(bubbleSound);

                bubbleSoundTimer = bubbleDelay;
            }
        }

        if (turbo)
        {
            for (int i = 0; i < fireParticlesGenerator.Length; i++)
            {
                var FPemission = fireParticlesGenerator[i].emission;
                FPemission.rateOverDistance = 3;
            }
        }
        else
        {
            for (int i = 0; i < fireParticlesGenerator.Length ; i++)
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
        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(groundRayOrigin.position, Vector3.down * 0.5f);
        //Gizmos.DrawRay(transform.position + transform.forward * 5f + new Vector3(currentCamPosCenter.z, currentCamPosCenter.y, 0), playerCamera.transform.position - transform.position + transform.forward * 5f + new Vector3(currentCamPosCenter.z, currentCamPosCenter.y, 0));
    }

    void GhostDrive()
    {
        if (raceSpline == null) { Debug.LogWarning("RaceSpline manquante sur le Kart !"); return; }

        // ── GESTION DU VOL ──────────────────────────────────────────
        if (isFlying)
        {
            // Stabilise le nez vers l'horizon pour éviter la chute libre
            if (flightDir != null)
            {
                float noseAngle = flightDir.localEulerAngles.x;
                // Convertit l'angle en -180/+180
                if (noseAngle > 180f) noseAngle -= 360f;

                // Si le nez plonge vers le bas, on tire vers le haut
                if (noseAngle > 5f)
                    inputGlideUpDown = -1f;
                // Si le nez pointe trop haut, on relâche
                else if (noseAngle < -5f)
                    inputGlideUpDown = 1f;
                else
                    inputGlideUpDown = 0f;
            }

            // Suit la spline horizontalement pendant le vol
            inputGlideTurn = turnDirection;
            accelerate = true;
            forwardDirection = 1f;
            return; // On court-circuite le reste de GhostDrive pendant le vol
        }

        float splineLength = raceSpline.CalculateLength();
        if (splineLength <= 0) return;

        // 1. RECOLLER LE KART À LA SPLINE (Évite les désynchronisations)
        // On trouve le point le plus proche de la spline par rapport à la position actuelle du kart
        var nativeSpline = raceSpline.Spline;

        // Convertit la position du Kart dans l'espace local de la Spline
        Vector3 localKartPos = raceSpline.transform.InverseTransformPoint(transform.position);

        // Trouve le progrès exact (t entre 0 et 1) correspondant à cette position
        Unity.Mathematics.float3 nearestPoint;
        float currentSplineTime;
        SplineUtility.GetNearestPoint(nativeSpline, localKartPos, out nearestPoint, out currentSplineTime);

        // On met à jour notre progression globale sur la base de la réalité physique
        SplineProgress = currentSplineTime;

        // 2. CALCULER LA CIBLE DEVANT LE KART (Look Ahead adaptatif)
        float targetProgress;

        // On utilise MainMenuUIManager.Instance.isMapInverted au lieu de l'ancien script de mode
        bool isInverted = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted);

        if (isInverted)
        {
            // En inversé, la cible est DERRIÈRE dans le sens de la spline (donc on soustrait)
            targetProgress = Mathf.Repeat(SplineProgress - lookAheadDistance, 1f);
        }
        else
        {
            // En normal, la cible est DEVANT dans le sens de la spline (donc on ajoute)
            targetProgress = Mathf.Repeat(SplineProgress + lookAheadDistance, 1f);
        }

        // Récupère la position de cette cible dans l'espace global (World)
        Vector3 targetPosition = (Vector3)raceSpline.EvaluatePosition(targetProgress);

        // Calcul du vecteur direction vers cette cible
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0; // On ignore l'axe Y pour éviter les calculs d'angles faussés en pente


        // 3. LOGIQUE DE DIRECTION ET DE CONDUITE
        float angle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

        // Si la cible est presque alignée avec l'avant du kart (-2 à +2 degrés), on reste droit
        if (Mathf.Abs(angle) < 2f)
        {
            turnDirection = 0f;
        }
        else
        {
            // On divise l'angle par un facteur plus grand (ex: 25f ou 30f au lieu de 15f) 
            // pour que le volant tourne de manière progressive et fluide.
            turnDirection = Mathf.Clamp(angle / 25f, -1f, 1f);
        }

        inputGlideTurn = turnDirection;

        // Sécurité anti-demi-tour : Si par accident l'angle demandé est supérieur à 90 degrés,
        // cela signifie que la cible est passée derrière ou s'est décalée brutalement.
        // Dans ce cas, on calme la direction pour lui laisser le temps de se réaxer proprement.
        if (Mathf.Abs(angle) > 90f)
        {
            turnDirection = Mathf.Sign(angle) * 0.3f; // Braquage très léger pour ne pas partir en tête-à-queue
        }

        // Commandes de gaz pour avancer
        accelerate = true;
        forwardDirection = 1f;

        // Optionnel : Si le kart est trop loin de sa cible (par exemple bloqué contre un mur),
        // on le force à accélérer pour se dégager.
        if (directionToTarget.sqrMagnitude < 0.5f)
        {
            // Si on est pile sur la cible, on calme légèrement la direction pour éviter les vibrations
            turnDirection = 0f;
        }
    }
}
