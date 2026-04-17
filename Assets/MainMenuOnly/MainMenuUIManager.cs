using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.Build.Content;
using UnityEngine.SceneManagement;

[System.Serializable]
public class WheelItem
{
    public string itemName = "Nouveau Bouton";
    public Sprite buttonTexture;

    [Header("--- Texte & Police ---")]
    public TMP_FontAsset customFont;

    public GameObject windowToOpen;

    [Header("--- Personnalisation ---")]
    [Tooltip("Laisse à 0,0 pour utiliser la taille par défaut du prefab.")]
    public Vector2 customSize = Vector2.zero;
}

public class MainMenuUIManager : MonoBehaviour
{
    public enum MenuState { TitleScreen, MainMenu, OptionsMenu, SubWindowOpen }

    [Header("--- États & Navigation ---")]
    public MenuState currentState = MenuState.TitleScreen;
    [Tooltip("Coche ça si tu trouves que Haut/Bas fait tourner la roue dans le mauvais sens !")]
    public bool invertNavigation = false;

    [Header("--- Configuration Générale des Roues ---")]
    public GameObject buttonPrefab;
    public float customAnglePerOption = 45f;
    [Tooltip("L'angle de départ du premier bouton (ex: 90 ou -90 pour commencer sur un côté)")]
    public float startAngleOffset = 0f;
    [Tooltip("Coche ça pour que les boutons s'alignent dans le sens inverse !")]
    public bool reverseSpawnDirection = false;
    public float wheelRadius = 150f;
    public float wheelRotationSpeed = 10f;
    public bool keepButtonsUpright = true;

    [Header("--- Mouvement Caméra/Écran ---")]
    public Transform viewContainer;
    public float transitionSpeed = 5f;
    public float Zoffset = 10;
    public Transform titleScreenPosition;
    public Transform mainMenuPosition;
    public Transform optionsPosition;

    //---
    public static MainMenuUIManager Instance;
    //---

    [Header("--- Effets Visuels (Hover) ---")]
    public float selectedScale = 1.3f;
    public float normalScale = 1.0f;
    public float scaleAnimSpeed = 12f;

    [Header("--- Roue Menu Principal ---")]
    public RectTransform mainWheelRect;
    public WheelItem[] mainMenuOptions;
    private int currentMainIndex = 0;
    private float targetMainAngle = 0f;
    private float initialMainAngle = 0f;
    private List<RectTransform> mainButtonsGenerated = new List<RectTransform>();

    [Header("--- Roue Paramètres (Settings) ---")]
    public RectTransform settingsWheelRect;
    public WheelItem[] settingsOptions;
    private int currentSettingsIndex = 0;
    private float targetSettingsAngle = 0f;
    private float initialSettingsAngle = 0f;
    private List<RectTransform> settingsButtonsGenerated = new List<RectTransform>();

    private GameObject currentActiveWindow = null;
    private bool isAxisInUse = false;

    // NOUVELLE VARIABLE : Pour savoir d'où on a ouvert la sous-fenêtre
    private MenuState stateBeforeSubWindow = MenuState.MainMenu;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (mainWheelRect != null) initialMainAngle = mainWheelRect.localEulerAngles.z;
        if (settingsWheelRect != null) initialSettingsAngle = settingsWheelRect.localEulerAngles.z;

        targetMainAngle = initialMainAngle;
        targetSettingsAngle = initialSettingsAngle;

        GenerateWheel(mainMenuOptions, mainWheelRect, mainButtonsGenerated, true);
        GenerateWheel(settingsOptions, settingsWheelRect, settingsButtonsGenerated, false);
    }

    void Update()
    {
        HandleInputs();
        SmoothTransitions();
        UpdateButtonsRotation();
        UpdateHoverEffects();
    }

    // ==========================================
    // 1. GÉNÉRATION PROCÉDURALE
    // ==========================================
    private void GenerateWheel(WheelItem[] options, RectTransform wheelParent, List<RectTransform> generatedList, bool boolean)
    {
        int k;
        if (buttonPrefab == null) return;

        for (int i = 0; i < options.Length; i++)
        {
            GameObject btnObj = Instantiate(buttonPrefab, wheelParent);
            btnObj.name = "Btn_" + options[i].itemName;

            RectTransform rectT = btnObj.GetComponent<RectTransform>();

            rectT.anchorMin = new Vector2(0.5f, 0.5f);
            rectT.anchorMax = new Vector2(0.5f, 0.5f);
            rectT.pivot = new Vector2(0.5f, 0.5f);

            if (options[i].customSize != Vector2.zero) rectT.sizeDelta = options[i].customSize;

            rectT.localScale = Vector3.one * normalScale;
            rectT.localPosition = Vector3.zero;

            float directionMultiplier = reverseSpawnDirection ? -1f : 1f;
            if (boolean) k = -i;
            else
                k = i;
            float currentAngle = (k * customAnglePerOption * directionMultiplier);
            float angleRad = (currentAngle + startAngleOffset) * Mathf.Deg2Rad;

            Vector2 anchoredPos = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad)) * wheelRadius;
            rectT.anchoredPosition = anchoredPos;

            Image img = btnObj.GetComponent<Image>();
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (img != null && options[i].buttonTexture != null) img.sprite = options[i].buttonTexture;

            if (txt != null)
            {
                txt.text = options[i].itemName;
                if (options[i].customFont != null) txt.font = options[i].customFont;
            }

            if (options[i].windowToOpen != null) options[i].windowToOpen.SetActive(false);

            generatedList.Add(rectT);
        }
    }

    // ==========================================
    // 2. GESTION DES INPUTS 
    // ==========================================
    private void HandleInputs()
    {
        if (currentState == MenuState.TitleScreen && Input.anyKeyDown)
        {
            ChangeState(MenuState.MainMenu);
            return;
        }

        if (currentState == MenuState.MainMenu || currentState == MenuState.OptionsMenu)
        {
            int inputDirection = 0;

            float verticalInput = Input.GetAxisRaw("Vertical");

            if (verticalInput != 0)
            {
                if (!isAxisInUse)
                {
                    if (verticalInput < -0.3f) inputDirection = 1;
                    else if (verticalInput > 0.3f) inputDirection = -1;

                    isAxisInUse = true;
                }
            }
            else
            {
                isAxisInUse = false;
            }

            if (inputDirection != 0)
            {
                if (invertNavigation) inputDirection = -inputDirection;
                RotateWheel(inputDirection);
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit"))
            {
                SelectCurrentWheelOption();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel"))
        {
            GoBack();
        }
    }

    // ==========================================
    // 3. LOGIQUE INTERNE DES ROUES
    // ==========================================
    private void RotateWheel(int direction)
    {
        float spawnDirection = reverseSpawnDirection ? -1f : 1f;

        if (currentState == MenuState.MainMenu)
        {
            currentMainIndex += direction;
            if (currentMainIndex >= mainMenuOptions.Length) currentMainIndex = 0;
            if (currentMainIndex < 0) currentMainIndex = mainMenuOptions.Length - 1;

            targetMainAngle = initialMainAngle + (-currentMainIndex * customAnglePerOption * spawnDirection);
        }
        else if (currentState == MenuState.OptionsMenu)
        {
            currentSettingsIndex += direction;
            if (currentSettingsIndex >= settingsOptions.Length) currentSettingsIndex = 0;
            if (currentSettingsIndex < 0) currentSettingsIndex = settingsOptions.Length - 1;

            targetSettingsAngle = initialSettingsAngle - (-currentSettingsIndex * customAnglePerOption * spawnDirection);
        }
    }

    private void SelectCurrentWheelOption()
    {
        if (currentState == MenuState.MainMenu)
        {
            string selectedName = mainMenuOptions[currentMainIndex].itemName.ToLower();
            WheelItem currentItem = mainMenuOptions[currentMainIndex];

            // Si le bouton PLAY a une sous-fenêtre, on l'ouvre, sinon on peut lancer direct
            if (selectedName.Contains("play") || selectedName.Contains("jouer"))
            {
                if (currentItem.windowToOpen != null) OpenWindow(currentItem.windowToOpen);
                else Debug.Log("Lancement direct du jeu !");
            }
            else if (selectedName.Contains("setting") || selectedName.Contains("option")) ChangeState(MenuState.OptionsMenu);
            else if (selectedName.Contains("quit") || selectedName.Contains("quitter"))
            {
                Debug.Log("Fermeture du jeu !");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(); 
#endif
            }
            else if (currentItem.windowToOpen != null) OpenWindow(currentItem.windowToOpen);
        }
        else if (currentState == MenuState.OptionsMenu)
        {
            WheelItem currentItem = settingsOptions[currentSettingsIndex];
            if (currentItem.windowToOpen != null) OpenWindow(currentItem.windowToOpen);
        }
    }

    private void OpenWindow(GameObject window)
    {
        currentActiveWindow = window;
        currentActiveWindow.SetActive(true);

        // Magie ici : On sauvegarde le menu dans lequel on était AVANT d'ouvrir !
        stateBeforeSubWindow = currentState;

        ChangeState(MenuState.SubWindowOpen);
    }

    public void ChangeState(MenuState newState)
    {
        currentState = newState;
    }

    public void GoBack()
    {
        if (currentState == MenuState.SubWindowOpen)
        {
            if (currentActiveWindow != null) currentActiveWindow.SetActive(false);
            currentActiveWindow = null;

            // On retourne exactement d'où on vient (MainMenu ou OptionsMenu)
            ChangeState(stateBeforeSubWindow);
        }
        else if (currentState == MenuState.OptionsMenu)
        {
            ChangeState(MenuState.MainMenu);
        }
    }

    // ==========================================
    // 4. ANIMATIONS & HOVER
    // ==========================================
    private void SmoothTransitions()
    {
        Vector3 targetPosition = Vector3.zero;
        Quaternion targetRotation = Quaternion.identity;

        switch (currentState)
        {
            case MenuState.TitleScreen:
                targetPosition = titleScreenPosition.position;
                targetRotation = titleScreenPosition.rotation;
                break;
            case MenuState.MainMenu:
                targetPosition = mainMenuPosition.position;
                targetRotation = mainMenuPosition.rotation;
                break;
            case MenuState.OptionsMenu:
                targetPosition = optionsPosition.position;
                targetRotation = optionsPosition.rotation;
                break;
            case MenuState.SubWindowOpen:
                // Si on a ouvert depuis le MainMenu, la caméra reste sur le MainMenu !
                if (stateBeforeSubWindow == MenuState.MainMenu)
                {
                    targetPosition = mainMenuPosition.position;
                    targetRotation = mainMenuPosition.rotation;
                }
                // Si on a ouvert depuis les Options, la caméra reste sur les Options !
                else
                {
                    targetPosition = optionsPosition.position;
                    targetRotation = optionsPosition.rotation;
                }
                break;
        }

        targetPosition.z += Zoffset;

        if (viewContainer != null)
        {
            viewContainer.position = Vector3.Lerp(viewContainer.position, targetPosition, Time.deltaTime * transitionSpeed);
            viewContainer.rotation = Quaternion.Lerp(viewContainer.rotation, targetRotation, Time.deltaTime * transitionSpeed);
        }

        if (mainWheelRect != null)
        {
            Quaternion targetMainRot = Quaternion.Euler(0, 0, targetMainAngle);
            mainWheelRect.localRotation = Quaternion.Lerp(mainWheelRect.localRotation, targetMainRot, Time.deltaTime * wheelRotationSpeed);
        }

        if (settingsWheelRect != null)
        {
            Quaternion targetSettingsRot = Quaternion.Euler(0, 0, targetSettingsAngle);
            settingsWheelRect.localRotation = Quaternion.Lerp(settingsWheelRect.localRotation, targetSettingsRot, Time.deltaTime * wheelRotationSpeed);
        }
    }

    private void UpdateHoverEffects()
    {
        for (int i = 0; i < mainButtonsGenerated.Count; i++)
        {
            float targetS = (currentState == MenuState.MainMenu && i == currentMainIndex) ? selectedScale : normalScale;
            Vector3 targetScale = new Vector3(targetS, targetS, targetS);
            mainButtonsGenerated[i].localScale = Vector3.Lerp(mainButtonsGenerated[i].localScale, targetScale, Time.deltaTime * scaleAnimSpeed);
        }

        for (int i = 0; i < settingsButtonsGenerated.Count; i++)
        {
            float targetS = (currentState == MenuState.OptionsMenu && i == currentSettingsIndex) ? selectedScale : normalScale;
            Vector3 targetScale = new Vector3(targetS, targetS, targetS);
            settingsButtonsGenerated[i].localScale = Vector3.Lerp(settingsButtonsGenerated[i].localScale, targetScale, Time.deltaTime * scaleAnimSpeed);
        }
    }

    private void UpdateButtonsRotation()
    {
        if (!keepButtonsUpright) return;

        float mainWheelZ = mainWheelRect != null ? mainWheelRect.localEulerAngles.z : 0f;
        foreach (RectTransform btn in mainButtonsGenerated)
        {
            btn.localRotation = Quaternion.Euler(0, 0, -mainWheelZ);
        }

        float settingsWheelZ = settingsWheelRect != null ? settingsWheelRect.localEulerAngles.z : 0f;
        foreach (RectTransform btn in settingsButtonsGenerated)
        {
            btn.localRotation = Quaternion.Euler(0, 0, -settingsWheelZ);
        }
    }

    public void LaunchScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}