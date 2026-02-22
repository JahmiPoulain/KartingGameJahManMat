
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class KartScriptV2 : MonoBehaviour
{
    public static KartScriptV2 instance;
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
    bool keepDrifting;
    float currentDriftForce;
    float driftCatchUp;
    int driftDir;
    public float highDrift;
    public float lowDrift;
    float driftTurboGauge;
    public float gaugeToActivateTurbo;

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

        //rb.AddForce(transform.forward * (currentSpeed + currentTurboForce) + bounceDirection * bounceForce, ForceMode.Acceleration);
        // rb.linearVelocity = transform.forward * (currentSpeed + currentTurboForce) + bounceDirection * bounceForce;
        //if (IsGrounded())
        if (grounded)
        {
            currentFallSpeed = 0;
        }
        else
        {          
            currentFallSpeed += gravity * Time.fixedDeltaTime;
        }
       
        rb.linearVelocity = (transform.forward * (currentSpeed + currentTurboForce) + bounceDirection * bounceForce) + Vector3.down * (0.1f +currentFallSpeed);
        transform.Rotate(0, currentTurnSpeed + currentDriftForce, 0);
        //rb.linearVelocity += Vector3.down * currentFallSpeed;

        HandleWholeKartRotationXZ();
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
        tryToDrift = Input.GetMouseButtonDown(0);
        keepDrifting = Input.GetMouseButton(0);
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
        if (forwardDirection == 0) 
        {
            driftDir = 0;
            currentDriftForce = 0;
            driftCatchUp = 0;
            driftTurboGauge = 0;
            for (int i = 0; i < fireWheelEffects.Length; i++)
            {
                fireWheelEffects[i].SetActive(false);
            }
            return; 
        }
        if (tryToDrift)
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
            }
        }
        if (!keepDrifting) 
        {
            driftDir = 0;
            currentDriftForce = 0;
            driftCatchUp = 0;
            if (driftTurboGauge > gaugeToActivateTurbo)
            {
                StartTurbo(driftTurboGauge * 2.2f, driftTurboGauge / 2.6f);
                driftTurboGauge = 0;
            }
            for (int i = 0; i < fireWheelEffects.Length; i++)
            {
                fireWheelEffects[i].SetActive(false);
            }
            for (int i = 0; i < driftParticlesGenerators.Length; i++)
            {
                driftParticlesGenerators[i].gameObject.SetActive(false);
            }
            return; 
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
                driftCatchUp += 6f * Time.deltaTime;
            }
            else if (driftCatchUp > nextDriftForceTarget)
            {
                driftCatchUp -= 6f * Time.deltaTime;
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
        if (keepDrifting)
        {
            if (nextTurnSpeed > maxTurnSpeed)
            {
                nextTurnSpeed = maxTurnSpeed;
            }
            else if (nextTurnSpeed < -maxTurnSpeed)
            {
                nextTurnSpeed = -maxTurnSpeed;
            }
        }

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

    

    public void StartTurbo(float force ,float time)
    {
        turbo = true;
        turboTimer = time;
        targetTurboForce = force;
        //float nextTForce = targetTurboForce + force;
        //if (nextTForce > targetTurboForce)
        //{
        //    targetTurboForce = nextTForce;
        //}
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
        
        
        if (currentSpeed > 0)
        {            
            if (forwardDirection > 0)
            { 
                visKartXRotCatchUp = (maxSpeed - currentSpeed) / 3f;
            }
            else
            {
                visKartXRotCatchUp = -(maxSpeed - currentSpeed) / 3f;
            }
        }
        else if (currentSpeed < 0)
        {            
            if (forwardDirection < 0)
            {
                visKartXRotCatchUp = (maxSpeed + currentSpeed) / 5f;
            }
            else
            {
                visKartXRotCatchUp = -(maxSpeed + currentSpeed) / 5f;
            }            
        }
        else if (visKartXRotCatchUp < 0.05f && visKartXRotCatchUp > -0.05f)
        {            
            visKartXRotCatchUp = 0;            
        }                    

        if (visKartXRotCatchUpBis < visKartXRotCatchUp)
        {
            visKartXRotCatchUpBis += 8f * Time.fixedDeltaTime;
        }
        else if (visKartXRotCatchUpBis > visKartXRotCatchUp)
        {
            visKartXRotCatchUpBis -= 8f * Time.fixedDeltaTime;
        }
            visKartXRot = (-currentSpeed / 2) * visKartXRotCatchUpBis;
        visKartZRot = currentTurnSpeed * (currentSpeed / 4.5f);
        float nextTotalSpeed = visKartXRot + -currentTurboForce * 2;
        nextTotalSpeed = Mathf.Clamp(nextTotalSpeed, -(maxSpeed + 12f), maxSpeed + 12f);
        preOrientation.up = Vector3.RotateTowards(preOrientation.up, groundNormal, 1.5f * Time.fixedDeltaTime, 0.0f);
        visualKartBody.transform.localRotation = Quaternion.Euler(nextTotalSpeed, transform.eulerAngles.y, visKartZRot + (driftCatchUp * 7f * driftDir));
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
        visualKartWheelsParent.transform.localRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
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
            playerCamera.transform.localPosition = new Vector3(nextXpos, 2.46f, -4.75f);
        }
        else if (playerCamera.transform.localPosition.x < camXpos)
        {
            float nextXpos = playerCamera.transform.localPosition.x + 1f * Time.fixedDeltaTime;
            if ( nextXpos > camXpos)
            {
                nextXpos = camXpos;
            }
            playerCamera.transform.localPosition = new Vector3(nextXpos, 2.46f, -4.75f);
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
}
