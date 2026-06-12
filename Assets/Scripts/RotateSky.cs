using UnityEngine;

public class RotateSky : MonoBehaviour
{
    public float rotationSpeed = 1.0f; // Vitesse de rotation
    private Material skyboxMat;

    void Start()
    {
        // Récupère le matériau de skybox actif
        skyboxMat = RenderSettings.skybox;
    }

    void Update()
    {
        if (skyboxMat != null)
        {
            // Augmente la rotation à chaque frame
            float currentRotation = skyboxMat.GetFloat("_Rotation");
            currentRotation += rotationSpeed * Time.deltaTime;
            skyboxMat.SetFloat("_Rotation", currentRotation % 360); // Assure que la rotation reste entre 0-360
        }
    }
}