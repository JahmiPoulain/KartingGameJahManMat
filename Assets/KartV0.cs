using UnityEngine;

public class KartMario : MonoBehaviour
{
    [Header("Stats")]
    public float acceleration = 2000f;
    public float maxSpeed = 30f;
    public float steering = 8f;
    public float driftGrip = 0.4f;
    public float normalGrip = 0.95f;
    public float driftBoost = 15f;
    public float downforce = 50f;

    [Header("References")]
    public Transform centerOfMass;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public Transform cameraTarget;
    public float cameraSmooth = 0.1f;
    public Transform[] frontWheels;
    public Transform[] rearWheels;

    Rigidbody rb;
    bool grounded;
    bool drifting;
    bool driftReady;
    float steerInput;
    float moveInput;
    Vector3 camVel;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 4f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (centerOfMass)
            rb.centerOfMass = centerOfMass.localPosition;
    }

    void Update()
    {
        moveInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        drifting = Input.GetKey(KeyCode.LeftShift);

        CameraFollow();
    }

    void FixedUpdate()
    {
        CheckGround();
        Move();
        Grip();
        DownForce();
        ClampSpeed();
        RotateWheels();
    }

    void CheckGround()
    {
        grounded = Physics.SphereCast(
            groundCheck.position,
            0.25f,
            Vector3.down,
            out RaycastHit hit,
            0.6f,
            groundLayer
        );
    }

    void Move()
    {
        if (!grounded) return;

        // Arcade acceleration
        float speedPercent = rb.linearVelocity.magnitude / maxSpeed;
        float accel = acceleration * (1 - speedPercent);
        rb.AddForce(transform.forward * moveInput * accel * Time.fixedDeltaTime, ForceMode.Acceleration);

        // Steering
        float turnPower = steering * (drifting ? 1.6f : 1f);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, steerInput * turnPower, 0f));

        // Drift boost
        if (drifting && moveInput != 0)
            driftReady = true;
        else if (driftReady)
        {
            rb.AddForce(transform.forward * driftBoost, ForceMode.VelocityChange);
            driftReady = false;
        }
    }

    void Grip()
    {
        if (!grounded) return;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= drifting ? driftGrip : normalGrip;
        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    void DownForce()
    {
        if (grounded)
            rb.AddForce(-transform.up * downforce);
    }

    void ClampSpeed()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    void RotateWheels()
    {
        float rot = rb.linearVelocity.magnitude * 8f * Time.fixedDeltaTime;

        foreach (Transform w in rearWheels)
            w.Rotate(Vector3.right, rot);

        foreach (Transform w in frontWheels)
        {
            w.Rotate(Vector3.right, rot);
            Vector3 e = w.localEulerAngles;
            e.y = steerInput * 30f;
            w.localEulerAngles = e;
        }
    }

    void CameraFollow()
    {
        if (!cameraTarget) return;

        Vector3 desired = transform.position - transform.forward * 7f + Vector3.up * 3f;
        cameraTarget.position = Vector3.SmoothDamp(cameraTarget.position, desired, ref camVel, cameraSmooth);
        cameraTarget.LookAt(transform.position + Vector3.up * 1.5f);
    }
}