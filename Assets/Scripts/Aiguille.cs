using UnityEngine;
using UnityEngine.AI;

public class Aiguille : MonoBehaviour
{
    [SerializeField] private RectTransform aiguille;
    [SerializeField] private KartScriptV2 kart;
    [SerializeField] private float value;

    void Start()
    {
        aiguille.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    // Update is called once per frame
    void Update()
    {
        float normalizedSpeed = (kart.currentSpeed + kart.currentTurboForce) / kart.maxSpeed;
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        float angle = Mathf.Lerp(90f, -90f, normalizedSpeed);
        aiguille.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
