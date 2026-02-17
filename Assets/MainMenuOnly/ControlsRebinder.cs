using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class InputSettingsManager : MonoBehaviour
{
    [Header("Réglages Stick")]
    [Range(0f, 5f)]
    public float stickSensitivity = 1f;
    public Slider sensitivitySlider;

    [Header("UI Rebinding")]
    public Transform rebindUIParent; // Parent des boutons
    public Button rebindButtonPrefab; // Préfab pour rebinding

    public PlayerControls controls; // Ton Input Actions Asset

    private Dictionary<string, InputAction> actionsDict = new Dictionary<string, InputAction>();
    private const string SensitivityKey = "StickSensitivity";

    private void Awake()
    {
        if (controls == null)
            controls = new PlayerControls();

        // Charger la sensibilité sauvegardée
        if (PlayerPrefs.HasKey(SensitivityKey))
            stickSensitivity = PlayerPrefs.GetFloat(SensitivityKey);

        if (sensitivitySlider)
        {
            sensitivitySlider.value = stickSensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        // Construire dictionnaire et UI rebinding
        foreach (var map in controls.asset.actionMaps)
        {
            foreach (var action in map.actions)
            {
                actionsDict[action.name] = action;

                // Créer un bouton UI pour chaque action
                if (rebindUIParent && rebindButtonPrefab)
                {
                    Button btn = Instantiate(rebindButtonPrefab, rebindUIParent);
                    string actionName = action.name;
                    string savedKey = PlayerPrefs.GetString(actionName, action.bindings[0].effectivePath);

                    // Appliquer la touche sauvegardée
                    action.ApplyBindingOverride(0, savedKey);

                    btn.GetComponentInChildren<Text>().text = $"{actionName}: {savedKey}";
                    btn.onClick.AddListener(() => StartRebind(actionName, btn));
                }
            }
        }
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

  

    // Callback du slider pour sauvegarder la sensibilité
    private void OnSensitivityChanged(float value)
    {
        stickSensitivity = value;
        PlayerPrefs.SetFloat(SensitivityKey, stickSensitivity);
        PlayerPrefs.Save();
    }

    // Méthode générique pour rebinding
    private void StartRebind(string actionName, Button uiButton)
    {
        if (!actionsDict.ContainsKey(actionName))
            return;

        InputAction action = actionsDict[actionName];
        action.Disable();

        uiButton.GetComponentInChildren<Text>().text = "Appuyez sur une touche...";

        action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse") // si tu veux exclure la souris
            .OnComplete(operation =>
            {
                action.Enable();
                string newKey = action.bindings[0].effectivePath;
                PlayerPrefs.SetString(actionName, newKey); // Sauvegarde
                PlayerPrefs.Save();

                uiButton.GetComponentInChildren<Text>().text = $"{actionName}: {newKey}";
                Debug.Log($"Nouvelle touche pour {actionName}: {newKey}");
                operation.Dispose();
            })
            .Start();
    }
}