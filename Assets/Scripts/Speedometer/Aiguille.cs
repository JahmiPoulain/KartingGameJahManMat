using UnityEngine;
using UnityEngine.AI;

public class Aiguille : MonoBehaviour
{
    [SerializeField] private RectTransform aiguille;
    [SerializeField] private KartScriptV2 kart;
    
    private float boostBlend = 0f;
    [SerializeField] private float smoothSpeed = 5f;

    private float displayedSpeed = 0f;
    [SerializeField] private float speedSmooth = 5f;
    [SerializeField] private float shakeThreshold = 0.98f;
    [SerializeField] private float shakeAmount = 2f;
    [SerializeField] private float shakeFrequency = 35f;

    void Start()
    {
        aiguille.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    // Update is called once per frame
    void Update()
    {
        float realSpeed = kart.isFlying ?
                          (kart.flightSpeed + kart.currentTurboForce) / kart.maxSpeed :
                          (kart.currentSpeed + kart.currentTurboForce) / kart.maxSpeed;
        realSpeed = Mathf.Clamp01(realSpeed);

        // --- smoothing vitesse ---
        displayedSpeed = Mathf.Lerp(displayedSpeed, realSpeed, Time.deltaTime * speedSmooth);

        float target = kart.currentTurboForce > 0 ? 1f : 0f;
        boostBlend = Mathf.Lerp(boostBlend, target, Time.deltaTime * smoothSpeed);

        // --- angle ---
        float maxAngle = Mathf.Lerp(-70f, -90f, boostBlend);
        float angle = Mathf.Lerp(90f, maxAngle, displayedSpeed);
        
        float shakeIntensity = Mathf.InverseLerp(shakeThreshold, 1f, displayedSpeed);
        float shake = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f;
        angle += shake * shakeAmount * shakeIntensity;

        aiguille.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
