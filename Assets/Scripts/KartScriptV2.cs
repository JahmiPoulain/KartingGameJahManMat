using UnityEngine;

public class KartScriptV2 : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody rb;

    [Header("Inputs")]
    float forwardDirection;
    float turnDirection;

    [Header("Speed")]
    public float maxSpeed = 25f;
    public float maxBackSpeed = 10f;
    public float currentSpeed;

    [Header("Acceleration")]
    public float accelSpeed = 5f;
    public float flatAccelSpeed = 10f;

    [Header("Deceleration")]
    public float decelSpeed = 5f;
    public float flatDecelSpeed = 8f;

    [Header("Turning")]
    public float maxTurnSpeed = 3f;
    public float currentTurnSpeed;
    public float turnAccelSpeed = 5f;
    public float turnDecelSpeed = 6f;

    [Header("Turbo")]
    public float currentTurboForce;
    public float turboAccelSpeed = 20f;
    float targetTurboForce;
    public float minTurboDecel = 15f;
    bool turbo;
    float turboTimer;

    [Header("Collision")]
    public LayerMask wallLayer;
    public float bounceForce;
    public float minBounceDecelForce = 30f;
    Vector3 bounceDirection;

    [Header("Camera")]
    public GameObject playerCamera;

    [Header("Visual")]
    public GameObject visualKartBody;
    Vector3 baseScale;

    [Header("Wheels")]
    public GameObject[] turningWheels;
    public GameObject[] nonTurningWheels;
    float turningRot;
    float nonTurningRot;

    [Header("Particles")]
    public ParticleSystem fireParticlesGenerator;
    ParticleSystem.EmissionModule fireEmission;

    [Header("Gravity")]
    public float gravity = 20f;
    float currentFallSpeed;
    public LayerMask groundLayer;
    Vector3 groundNormal;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0.2f;
        rb.angularDamping = 2f;

        baseScale = visualKartBody.transform.localScale;
        fireEmission = fireParticlesGenerator.emission;
    }

    void Update()
    {
        forwardDirection = Input.GetAxisRaw("Vertical");
        turnDirection = Input.GetAxisRaw("Horizontal");

        if (Input.GetMouseButtonDown(1))
            StartTurbo(15f, 1.2f);
    }

    void FixedUpdate()
    {
        HandleSpeed();
        HandleTurning();
        HandleTurbo();
        HandleGravity();
        ApplyMovement();
        HandleCamera();
        HandleWheels();
        HandleVisual();
        HandleParticles();
    }

    void HandleSpeed()
    {
        float nextSpeed = currentSpeed;

        if (forwardDirection > 0)
            nextSpeed += flatAccelSpeed + (maxSpeed - currentSpeed) * accelSpeed * Time.fixedDeltaTime;

        else if (forwardDirection < 0)
            nextSpeed -= flatAccelSpeed + (maxBackSpeed + currentSpeed) * accelSpeed * Time.fixedDeltaTime;

        else
        {
            if (currentSpeed > 0)
                nextSpeed -= flatDecelSpeed * Time.fixedDeltaTime;
            else if (currentSpeed < 0)
                nextSpeed += flatDecelSpeed * Time.fixedDeltaTime;
        }

        nextSpeed = Mathf.Clamp(nextSpeed, -maxBackSpeed, maxSpeed);
        currentSpeed = nextSpeed;
    }

    void HandleTurning()
    {
        float speedRatio = Mathf.Abs(currentSpeed) / maxSpeed;
        float nextTurn = currentTurnSpeed;

        nextTurn += turnDirection * turnAccelSpeed * speedRatio * Time.fixedDeltaTime;

        if (turnDirection == 0)
            nextTurn = Mathf.Lerp(nextTurn, 0, turnDecelSpeed * Time.fixedDeltaTime);

        currentTurnSpeed = Mathf.Clamp(nextTurn, -maxTurnSpeed, maxTurnSpeed);
    }

    void HandleTurbo()
    {
        if (!turbo) return;

        turboTimer -= Time.fixedDeltaTime;

        if (turboTimer > 0)
            currentTurboForce = Mathf.MoveTowards(currentTurboForce, targetTurboForce, turboAccelSpeed * Time.fixedDeltaTime);
        else
            currentTurboForce = Mathf.MoveTowards(currentTurboForce, 0, minTurboDecel * Time.fixedDeltaTime);

        if (currentTurboForce <= 0)
            turbo = false;
    }

    public void StartTurbo(float force, float time)
    {
        turbo = true;
        turboTimer = Mathf.Max(turboTimer, time);
        targetTurboForce = Mathf.Max(targetTurboForce, force);
    }

    void HandleGravity()
    {
        if (IsGrounded())
            currentFallSpeed = -2f;
        else
            currentFallSpeed += gravity * Time.fixedDeltaTime;
    }

    bool IsGrounded()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.2f, groundLayer))
        {
            groundNormal = hit.normal;
            return true;
        }
        return false;
    }

    void ApplyMovement()
    {
        Vector3 velocity =
            transform.forward * (currentSpeed + currentTurboForce) +
            bounceDirection * bounceForce +
            Vector3.down * currentFallSpeed;

        rb.linearVelocity = velocity;
        transform.Rotate(0, currentTurnSpeed * 50f * Time.fixedDeltaTime, 0);

        bounceForce = Mathf.MoveTowards(bounceForce, 0, minBounceDecelForce * Time.fixedDeltaTime);
    }

    void HandleCamera()
    {
        float camX = currentSpeed * -currentTurnSpeed / 80f;
        Vector3 target = new Vector3(camX, 3.9f, -4.7f);

        playerCamera.transform.localPosition =
            Vector3.Lerp(playerCamera.transform.localPosition, target, 5f * Time.fixedDeltaTime);
    }

    void HandleWheels()
    {
        turningRot += currentSpeed * 20f * Time.fixedDeltaTime;
        nonTurningRot += currentSpeed * 20f * Time.fixedDeltaTime;

        foreach (var w in turningWheels)
            w.transform.localRotation = Quaternion.Euler(turningRot, currentTurnSpeed * 30f, 90);

        foreach (var w in nonTurningWheels)
            w.transform.localRotation = Quaternion.Euler(nonTurningRot, 0, 90);
    }

    void HandleVisual()
    {
        float tiltZ = currentTurnSpeed * currentSpeed * 0.5f;
        float tiltX = -currentSpeed * 0.2f - currentTurboForce * 1.5f;

        visualKartBody.transform.localRotation = Quaternion.Euler(tiltX, 0, tiltZ);
    }

    void HandleParticles()
    {
        fireEmission.rateOverDistance = turbo ? 3 : 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            Vector3 rawDir = transform.position - collision.contacts[0].point;
            bounceDirection = new Vector3(rawDir.x, 0, rawDir.z).normalized;

            bounceForce = Mathf.Abs(currentSpeed) * 2f;
            currentSpeed *= 0.2f;
        }
    }
}