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

    void Start()
    {
        aiguille.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    // Update is called once per frame
    void Update()
    {

        float realSpeed = (kart.currentSpeed + kart.currentTurboForce) / kart.maxSpeed;
        realSpeed = Mathf.Clamp01(realSpeed);

        // --- smoothing vitesse ---
        displayedSpeed = Mathf.Lerp(displayedSpeed, realSpeed, Time.deltaTime * speedSmooth);

        float target = kart.currentTurboForce > 0 ? 1f : 0f;
        boostBlend = Mathf.Lerp(boostBlend, target, Time.deltaTime * smoothSpeed);

        // --- angle ---
        float maxAngle = Mathf.Lerp(-70f, -90f, boostBlend);
        float angle = Mathf.Lerp(90f, maxAngle, displayedSpeed);

        aiguille.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
