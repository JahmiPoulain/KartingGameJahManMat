using UnityEngine;

public class Aiguille : MonoBehaviour
{
    // Pivot vide à faire tourner. Si non assigné, on utilise l'aiguille directement.
    [SerializeField] private RectTransform pivot;
    // Référence vers l'image de l'aiguille, conservée pour compatibilité avec l'ancienne scène.
    [SerializeField] private RectTransform aiguille;
    // Référence vers le kart pour connaître sa vitesse et son état de boost
    [SerializeField] private KartScriptV2 kart;
    
    // Valeur entre 0 et 1 représentant la progression du boost (0 = pas de boost, 1 = boost max)
    private float boostBlend = 0f;
    // Vitesse de lissage pour les transitions de boost et de vitesse
    [SerializeField] private float smoothSpeed = 5f;

    // Valeur affichée de la vitesse, lissée pour éviter les à-coups
    private float displayedSpeed = 0f;
    // Vitesse de lissage de l’aiguille par rapport à la vraie vitesse
    [SerializeField] private float speedSmooth = 5f;
    // Seuil à partir duquel l’aiguille commence à trembler
    [SerializeField] private float shakeThreshold = 0.98f;
    // Amplitude maximale du tremblement, en degrés
    [SerializeField] private float shakeAmount = 2f;
    // Fréquence du tremblement. Plus la valeur est haute, plus ça tremble vite
    [SerializeField] private float shakeFrequency = 35f;
    // Rotation configurée dans Unity pour la position visuelle "vitesse 0"
    private float zeroSpeedAngle;

    void Start()
    {
        if (pivot == null)
            pivot = aiguille;

        // La rotation posée dans l'inspector devient la position de repos.
        zeroSpeedAngle = pivot.eulerAngles.z;
    }

    void Update()
    {
        // Si le kart est en train de voler, on utilise la vitesse de vol pour le calcul, sinon la vitesse au sol
        float realSpeed = kart.isFlying ?
                          (kart.flightSpeed + kart.currentTurboForce) / kart.maxSpeed :
                          (kart.currentSpeed + kart.currentTurboForce) / kart.maxSpeed;
        // Force realSpeed à rester entre 0 et 1
        realSpeed = Mathf.Clamp01(realSpeed);

        // Au lieu de faire un changement brutal de la position de l’aiguille, on lisse la transition pour un effet plus fluide
        displayedSpeed = Mathf.Lerp(displayedSpeed, realSpeed, Time.deltaTime * speedSmooth);

        float target = kart.currentTurboForce > 0 ? 1f : 0f;
        // On fait un blend qui va de 0 à 1 en fonction de si le boost est actif ou pas, avec un lissage pour éviter les changements brusques
        boostBlend = Mathf.Lerp(boostBlend, target, Time.deltaTime * smoothSpeed);

        // On calcule l'angle de l'aiguille en fonction de la vitesse affichée
        // À 0% de la vitesse, l'aiguille reste sur sa rotation initiale.
        float maxAngle = Mathf.Lerp(-160f, -180f, boostBlend);
        float angle = zeroSpeedAngle + Mathf.Lerp(0f, maxAngle, displayedSpeed);
        
        // Si la vitesse affichée dépasse le seuil, on ajoute un tremblement à l'aiguille pour renforcer l'impression de vitesse extrême
        float shakeIntensity = Mathf.InverseLerp(shakeThreshold, 1f, displayedSpeed);
        // Le tremblement est basé sur du Perlin Noise pour un effet plus organique, et il est multiplié par l'intensité calculée
        float shake = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f;
        angle += shake * shakeAmount * shakeIntensity;

        // On applique la rotation finale à l'aiguille
        pivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
