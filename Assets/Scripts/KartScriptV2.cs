using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    private float turnDirection; // la direction de la rotation du volant
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
    public Vector3 thirdPersonCamPos;
    public Vector3 firstPersonCamPos;
    private Vector3 currentCamPosCenter;
    private float turboTimer;

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

    [Header("Bounce Animation")]
    public bool bounce;
    private float bounceTimer;

    [Header("Smoke")]
    public GameObject smokePrefab;
    public Transform smokeOrigin;
    public Material baseSmokeMat;
    public Material fireSmokeMat;
    public ParticleSystem smokeParticlesGenerator;
    public ParticleSystem fireParticlesGenerator;
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
    public bool canDrive = true;

    private float respawnCooldown = 0f;
    private Vector3 startPosition;
    private Quaternion startRotation;

    [Header("Ghost")]
    [SerializeField] ContreLaMontre contreLaMontre;
    [SerializeField] private GameObject trackPath;
    public bool ghostMode = false;
    public Transform currentWaypoint;
    public Transform firstWaypoint;

    public Vector3 StartPosition { get => startPosition; set => startPosition = value; }
    public Quaternion StartRotation { get => startRotation; set => startRotation = value; }

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
    }

    void Start()
    {
        //Application.targetFrameRate = 20;
        rb = GetComponent<Rigidbody>();
        groundNormal = new Vector3(0, 1, 0);
        activeRespawnPoints = respawnPoints;
    }

    void Update()
    {
        PlayerInputs();
        HandleDrift();
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
            groundedCoyoteTimer = 0.1f;
            gliderGO.SetActive(false);
            transform.Rotate(0, currentTurnSpeed + currentDriftForce, 0);
            rb.linearVelocity = (groundNormalT.transform.forward * (currentSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + currentFallSpeed);
        }
        else if (!isFlying)
        {
            if (groundedCoyoteTimer > 0)
            {
                Debug.Log(groundedCoyoteTimer);
                groundedCoyoteTimer -= Time.fixedDeltaTime;
                transform.Rotate(0, (currentTurnSpeed + currentDriftForce), 0);
            }
            else transform.Rotate(0, (currentTurnSpeed + currentDriftForce) / 3f, 0);
                
            rb.linearVelocity = (groundNormalT.transform.forward * (airSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + currentFallSpeed);

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
        rb.linearVelocity = (flightDir.forward * (flightSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f + (currentFallSpeed / (1f + flightSpeed / 2.5f)) / 1.2f);
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
        if (outOfBounds)
        {
            return;
        }

        if (ghostMode == true)
        {
            GhostDrive();
            return;
        }

        if (!canDrive)
        {
            forwardDirection = 0;
            turnDirection = 0;
            return;
        }

        forwardDirection = InputSystemHandler.instance.inputForwardDir;
        turnDirection = InputSystemHandler.instance.inputTurnDir;
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
            nextSpeed -= flatAccelSpeed + (maxSpeed - currentSpeed) * accelSpeed * Time.fixedDeltaTime;
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
            if (grounded && tryDriftCoyoteTime > 0f)
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
            Debug.Log("Coyote" + tryDriftCoyoteTime);
        }

        //ca mem
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! faux drift
        if (keepDrifting && grounded && driftDir != 0)
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

            //oldKeepD = keepDrifting;
            //driftCoyoteTime = 0.12f;
        }
        else // quand on lache le drift
        {
            // if (driftCoyoteTime > 0f)
            // {
            //     driftCoyoteTime -= Time.deltaTime;
            //     return;
            // }                   
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
            nextTurnSpeed = IncrementTowardsValue(nextTurnSpeed, 0, turnDecelSpeed * Time.fixedDeltaTime);
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

    void HandleWholeKartRotationXZ()
    {
        //transform.up = new Vector3(goundNormal.x, goundNormal.y, goundNormal.z);
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
        preOrientation.localRotation = Quaternion.RotateTowards(preOrientation.localRotation, groundNormalT.localRotation, 1f);
        Quaternion rotTarget = Quaternion.Euler(nextTotalSpeed, 0, visKartZRot);
        visualKartBody.transform.localRotation = Quaternion.RotateTowards(visualKartBody.transform.localRotation, rotTarget, 10f);
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
            turningWheels[i].transform.localRotation = Quaternion.Euler(turningWheelsXRot, visWheelsYRot, 90);
        }
        for (int i = 0; i < nonTurningWheels.Length; i++)
        {
            nonTurningWheels[i].transform.localRotation = Quaternion.Euler(nonTurningWheelsXRot, 0, 90);
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
        float driftForce = Mathf.Clamp(currentDriftForce, -1.5f, 1.5f);

        if (driftForce < 0)
        {
            driftForce = -driftForce;
        }

        float targetX = Mathf.Clamp((currentSpeed * -currentTurnSpeed / 110f * forwardDirection) + (turnDirection * driftForce), -10f, 10f);

        //  Debug.Log("avant " + playerCamera.transform.localPosition + camXpos);
        if (playerCamera.transform.localPosition.x > targetX)
        {
            float nextXpos = playerCamera.transform.localPosition.x - 1.2f * Time.deltaTime;

            if (nextXpos < targetX)
            {
                nextXpos = targetX;
            }

            playerCamera.transform.localPosition = currentCamPosCenter + new Vector3(nextXpos, playerCamera.transform.localPosition.y, playerCamera.transform.localPosition.z);
        }
        else if (playerCamera.transform.localPosition.x < targetX)
        {
            float nextXpos = playerCamera.transform.localPosition.x + 1.2f * Time.deltaTime;

            if (nextXpos > targetX)
            {
                nextXpos = targetX;
            }

            playerCamera.transform.localPosition = currentCamPosCenter + new Vector3(nextXpos, playerCamera.transform.localPosition.y, playerCamera.transform.localPosition.z);
        }
        // Debug.Log("apres " + playerCamera.transform.localPosition + camXpos);
        // Debug.Log("avant "+playerCamera.transform.localPosition + camXpos);
        // playerCamera.transform.localPosition = new Vector3(IncrementTowardsValue(playerCamera.transform.localPosition.x, camXpos, 0.15f * Time.deltaTime), playerCamera.transform.localPosition.y, playerCamera.transform.localPosition.z);
        // Debug.Log("apres " + playerCamera.transform.localPosition + camXpos);
        Vector3 targetDir = (transform.forward + (transform.right * currentTurnSpeed * Mathf.Clamp(currentDriftForce, -1f, 1f) * 0.05f)).normalized;
        float rotSpeed = 0.1f + (camPivot.forward - targetDir).magnitude * 2f; // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        camPivot.forward = Vector3.RotateTowards(camPivot.forward, targetDir, 1 * Time.deltaTime, 0.0f);
        
       /* if (InputSystemHandler.instance.inputCameraMode)
        {
            Vector3 nextDir = thirdPersonCamPos - currentCamPosCenter; //playerCamera.transform.localPosition;
            Vector3 nextDirNorm = nextDir.normalized;
            Vector3 nextPos = (nextDirNorm + nextDir * 8f) * Time.deltaTime;
            if (nextDir.sqrMagnitude > 0.0001f)
            {
                //playerCamera.transform.localPosition += nextPos;
                currentCamPosCenter += nextPos;
            }
            else
            {
                //playerCamera.transform.localPosition = thirdPersonCamPos;
                currentCamPosCenter = thirdPersonCamPos;
            }
        }
        else
        {
            Vector3 nextDir = firstPersonCamPos - currentCamPosCenter;//playerCamera.transform.localPosition;
            Vector3 nextDirNorm = nextDir.normalized;
            Vector3 nextPos = (nextDirNorm + nextDir * 8f) * Time.deltaTime;
            if (nextDir.sqrMagnitude > 0.0001f)
            {
                //playerCamera.transform.localPosition += nextPos;
                currentCamPosCenter += nextPos;
            }
            else
            {
                //playerCamera.transform.localPosition = firstPersonCamPos;
                currentCamPosCenter = firstPersonCamPos;
            }
        }*/
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
            var FPemission = fireParticlesGenerator.emission;
            FPemission.rateOverDistance = 3;
        }
        else
        {
            var FPemission = fireParticlesGenerator.emission;
            FPemission.rateOverDistance = 0;

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
    }
}
