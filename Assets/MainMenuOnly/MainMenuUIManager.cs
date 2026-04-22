using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections; // <-- Important pour la Coroutine

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
    // Ajout de l'état "Loading" pour bloquer les inputs pendant la transition
    public enum MenuState { TitleScreen, MainMenu, OptionsMenu, SubWindowOpen, Loading }

    [Header("--- États & Navigation ---")]
    public MenuState currentState = MenuState.TitleScreen;
    [Tooltip("Coche ça si tu trouves que Haut/Bas fait tourner la roue dans le mauvais sens !")]
    public bool invertNavigation = false;

    // LA VARIABLE MAGIQUE : "static" fait qu'elle ne se réinitialise pas quand on recharge la scène !
    private static bool hasSeenTitleScreen = false;

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

    [Header("--- Navigation Avancée ---")]
    public float initialRepeatDelay = 0.4f;
    public float minRepeatInterval = 0.1f;
    public float accelerationFactor = 0.02f;

    private float nextActionTime = 0f;
    private float currentRepeatInterval;
    private int lastDirection = 0;
    private bool isHolding = false;

    [Header("--- Transition de Scène ---")]
    [Tooltip("Un CanvasGroup noir (ou autre) qui va faire un fondu au noir.")]
    public CanvasGroup transitionScreen;
    public float transitionDuration = 1f;

    //---
    public static MainMenuUIManager Instance;
    //---

    [Header("--- UI Panels ---")]
    public GameObject titleScreenPanel;

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

    private MenuState stateBeforeSubWindow = MenuState.MainMenu;

    [Header("--- Paramètres Vidéo ---")]
    public string[] resolutions = { "1920x1080", "1600x900", "1280x720", "800x600" };
    public int[] fpsValues = { 30, 60, 120, -1 };
    public string[] fpsLabels = { "30 FPS", "60 FPS", "120 FPS", "Illimité" };

    [HideInInspector] public int currentResIndex = 0;
    [HideInInspector] public int currentFpsIndex = 1;
    [HideInInspector] public bool isFullscreen = true;
    [HideInInspector] public bool isVsync = false;

    private void Awake()
    {
        
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;
        if (hasSeenTitleScreen)
        {
            currentState = MenuState.MainMenu;

            if (titleScreenPanel != null) titleScreenPanel.SetActive(false);

            if (viewContainer != null && mainMenuPosition != null)
            {
                Vector3 targetPosition = mainMenuPosition.position;
                targetPosition.z += Zoffset;
                viewContainer.position = targetPosition;
                viewContainer.rotation = mainMenuPosition.rotation;
            }
        }
        else
        {
            currentState = MenuState.TitleScreen;
            if (titleScreenPanel != null) titleScreenPanel.SetActive(true);
        }
        if (transitionScreen != null)
        {
            transitionScreen.alpha = 0f;
            transitionScreen.blocksRaycasts = false;
        }

        if (mainWheelRect != null) initialMainAngle = mainWheelRect.localEulerAngles.z;
        if (settingsWheelRect != null) initialSettingsAngle = settingsWheelRect.localEulerAngles.z;

        targetMainAngle = initialMainAngle;
        targetSettingsAngle = initialSettingsAngle;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GenerateWheel(mainMenuOptions, mainWheelRect, mainButtonsGenerated, true);
        GenerateWheel(settingsOptions, settingsWheelRect, settingsButtonsGenerated, false);

        LoadSettings();
        ApplySettings(false);
    }

    void Update()
    {
        if (currentState == MenuState.Loading) return;

        if (ControlsSettings.IsRebinding) return;

        HandleInputs();
        SmoothTransitions();
        UpdateButtonsRotation();
        UpdateHoverEffects();
    }

    public void LoadSettings()
    {
        currentResIndex = PlayerPrefs.GetInt("ResIndex", 0);
        currentFpsIndex = PlayerPrefs.GetInt("FpsIndex", 1);
        isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        isVsync = PlayerPrefs.GetInt("Vsync", 0) == 1;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("ResIndex", currentResIndex);
        PlayerPrefs.SetInt("FpsIndex", currentFpsIndex);
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("Vsync", isVsync ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log("Paramètres sauvegardés !");
    }

    public void ApplySettings(bool shouldSave)
    {
        string[] resParts = resolutions[currentResIndex].Split('x');
        if (resParts.Length == 2)
        {
            int width = int.Parse(resParts[0]);
            int height = int.Parse(resParts[1]);
            Screen.SetResolution(width, height, isFullscreen);
        }

        QualitySettings.vSyncCount = isVsync ? 1 : 0;
        Application.targetFrameRate = fpsValues[currentFpsIndex];

        if (shouldSave) SaveSettings();

        Debug.Log("Paramètres Appliqués !");
    }
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
    private void HandleInputs()
    {
        // 1. Écran de titre
        if (currentState == MenuState.TitleScreen && Input.anyKeyDown)
        {
            hasSeenTitleScreen = true;
            if (titleScreenPanel != null) titleScreenPanel.SetActive(false);
            ChangeState(MenuState.MainMenu);
            return;
        }

        // 2. Navigation Roue
        if (currentState == MenuState.MainMenu || currentState == MenuState.OptionsMenu)
        {
            int inputDirection = 0;

            // LECTURE : On mixe Vertical (Z/S/Haut/Bas) et Horizontal (Q/D/Gauche/Droite)
            // pour que le joueur puisse naviguer comme il veut sur la roue
            float v = Input.GetAxisRaw("Vertical");
            float h = Input.GetAxisRaw("Horizontal");
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            float combinedInput = Mathf.Abs(v) > Mathf.Abs(h) ? v : h;

            // Priorité Molette (mouvement sec)
            if (Mathf.Abs(scroll) > 0.01f)
            {
                inputDirection = scroll > 0 ? -1 : 1;
            }

            else if (Mathf.Abs(combinedInput) > 0.6f)
            {
                int currentDir = combinedInput > 0 ? -1 : 1;

                if (!isHolding || currentDir != lastDirection)
                {
                    // PREMIER CLIC (Instantané)
                    inputDirection = currentDir;
                    lastDirection = currentDir;
                    isHolding = true;
                    currentRepeatInterval = initialRepeatDelay;
                    nextActionTime = Time.unscaledTime + initialRepeatDelay;
                }
                else if (Time.unscaledTime >= nextActionTime)
                {
                    // DÉFILEMENT CONTINU
                    inputDirection = currentDir;

                    // On accélère doucement
                    currentRepeatInterval = Mathf.Max(minRepeatInterval, currentRepeatInterval - accelerationFactor);
                    nextActionTime = Time.unscaledTime + currentRepeatInterval;
                }
            }
            else
            {
                // Reset quand on lâche
                isHolding = false;
                lastDirection = 0;
            }

            // Application du mouvement
            if (inputDirection != 0)
            {
                if (invertNavigation) inputDirection = -inputDirection;
                RotateWheel(inputDirection);
            }

            // 3. Validation
            if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return))
            {
                SelectCurrentWheelOption();
            }
        }

        // 4. Retour
        if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }
    }


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

            if (selectedName.Contains("play") || selectedName.Contains("jouer"))
            {
                if (currentItem.windowToOpen != null) OpenWindow(currentItem.windowToOpen);
                else LaunchScene("TaSceneDeJeuIci");
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
            ChangeState(stateBeforeSubWindow);
        }
        else if (currentState == MenuState.OptionsMenu)
        {
            ChangeState(MenuState.MainMenu);
        }
    }
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
                if (stateBeforeSubWindow == MenuState.MainMenu)
                {
                    targetPosition = mainMenuPosition.position;
                    targetRotation = mainMenuPosition.rotation;
                }
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
        StartCoroutine(TransitionAndLoad(sceneName));
    }

    private IEnumerator TransitionAndLoad(string sceneName)
    {
        ChangeState(MenuState.Loading);

        if (transitionScreen != null)
        {
            transitionScreen.blocksRaycasts = true;
            float time = 0;
            while (time < transitionDuration)
            {
                transitionScreen.alpha = Mathf.Lerp(0, 1, time / transitionDuration);
                time += Time.deltaTime;
                yield return null;
            }
            transitionScreen.alpha = 1;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}