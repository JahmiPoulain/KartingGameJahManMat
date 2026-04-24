using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Audio;

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
    public enum MenuState { TitleScreen, MainMenu, OptionsMenu, SubWindowOpen, Loading }

    [Header("--- États & Navigation ---")]
    public MenuState currentState = MenuState.TitleScreen;
    [Tooltip("Coche ça si tu trouves que Haut/Bas fait tourner la roue dans le mauvais sens !")]
    public bool invertNavigation = false;

    private static bool hasSeenTitleScreen = false;

    [Header("--- Configuration Générale des Roues ---")]
    public GameObject buttonPrefab;
    public float customAnglePerOption = 45f;
    public float startAngleOffset = 0f;
    public bool reverseSpawnDirection = false;
    public float wheelRadius = 150f;
    public float wheelRotationSpeed = 10f;
    public bool keepButtonsUpright = true;

    [Header("--- Mouvement Caméra/Écran ---")]
    public Transform CameraTransform;
    public float Zoffset = 10;
    public Transform titleScreenPosition;
    public Transform mainMenuPosition;
    public Transform optionsPosition;

<<<<<<< HEAD:Assets/MainMenuOnly/MainMenuUIManager.cs
=======
    [Header("--- Transition Dynamique ---")]
    [Tooltip("Crée une courbe qui monte à 1.1 puis redescend à 1.0 pour l'effet d'élan !")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float transitionDuration = 0.8f;

    private float transitionTimer = 0f;
    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 targetPos;
    private Quaternion targetRot;
    private bool isTransitioning = false;

>>>>>>> Build-Complet-2:Assets/MainMenuOnly/Scripts/MainMenuUIManager.cs
    [Header("--- Navigation Avancée ---")]
    public float initialRepeatDelay = 0.4f;
    public float minRepeatInterval = 0.1f;
    public float accelerationFactor = 0.02f;

    private float nextActionTime = 0f;
    private float currentRepeatInterval;
    private int lastDirection = 0;
    private bool isHolding = false;

    [Header("--- Transition de Scène ---")]
<<<<<<< HEAD:Assets/MainMenuOnly/MainMenuUIManager.cs
    [Tooltip("Un CanvasGroup noir (ou autre) qui va faire un fondu au noir.")]
    public CanvasGroup transitionScreen;
    public float transitionDuration = 1f;
=======
    public CanvasGroup transitionScreen;
    public float sceneTransitionDuration = 1f;
>>>>>>> Build-Complet-2:Assets/MainMenuOnly/Scripts/MainMenuUIManager.cs

    public static MainMenuUIManager Instance;

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

    [Header("--- Paramètres Audio ---")]
    public AudioMixer mainAudioMixer;
    public int masterVol = 10;
    public int musicVol = 10;
    public int sfxVol = 10;

    public string controlsSaveKey = ("Controles");

    private GameObject currentActiveWindow = null;
    private MenuState stateBeforeSubWindow = MenuState.MainMenu;

    [Header("--- Paramètres Vidéo ---")]
    public string[] resolutions = { "1920x1080", "1600x900", "1280x720", "800x600" };
    public int[] fpsValues = { 30, 60, 120, -1 };
    public string[] fpsLabels = { "30", "60", "120", "Illimité" };

    [HideInInspector] public int currentResIndex = 0;
    [HideInInspector] public int currentFpsIndex = 1;
    [HideInInspector] public bool isFullscreen = true;
    [HideInInspector] public bool isVsync = false;

<<<<<<< HEAD:Assets/MainMenuOnly/MainMenuUIManager.cs
    private void Awake()
    {

        Instance = this;
    }
=======
    private void Awake() { Instance = this; }
>>>>>>> Build-Complet-2:Assets/MainMenuOnly/Scripts/MainMenuUIManager.cs

    void Start()
    {
        Time.timeScale = 1f;

        if (mainWheelRect != null) initialMainAngle = mainWheelRect.localEulerAngles.z;
        if (settingsWheelRect != null) initialSettingsAngle = settingsWheelRect.localEulerAngles.z;
        targetMainAngle = initialMainAngle;
        targetSettingsAngle = initialSettingsAngle;

        GenerateWheel(mainMenuOptions, mainWheelRect, mainButtonsGenerated, true);
        GenerateWheel(settingsOptions, settingsWheelRect, settingsButtonsGenerated, false);

        if (hasSeenTitleScreen)
        {
            currentState = MenuState.MainMenu;
            if (titleScreenPanel != null) titleScreenPanel.SetActive(false);
            SetCameraPositionImmediate(mainMenuPosition);
        }
        else
        {
            currentState = MenuState.TitleScreen;
            if (titleScreenPanel != null) titleScreenPanel.SetActive(true);
            SetCameraPositionImmediate(titleScreenPosition);
        }

        if (transitionScreen != null)
        {
            transitionScreen.gameObject.SetActive(true);
            transitionScreen.alpha = 0f;
        }

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


    public void ChangeState(MenuState newState)
    {
        if (newState == currentState) return;

        startPos = CameraTransform.position;
        startRot = CameraTransform.rotation;

        Transform targetAnchor = GetTargetTransform(newState);
        if (targetAnchor != null)
        {
            targetPos = targetAnchor.position;
            targetPos.z += Zoffset;
            targetRot = targetAnchor.rotation;
        }

        currentState = newState;
        transitionTimer = 0f;
        isTransitioning = true;
    }

    private void SmoothTransitions()
    {
        UpdateWheelsRotation();

        if (!isTransitioning || CameraTransform == null) return;

        transitionTimer += Time.deltaTime;
        float t = transitionTimer / transitionDuration;
        float curveValue = transitionCurve.Evaluate(t);

        CameraTransform.position = Vector3.LerpUnclamped(startPos, targetPos, curveValue);
        CameraTransform.rotation = Quaternion.LerpUnclamped(startRot, targetRot, curveValue);

        if (t >= 1f)
        {
            isTransitioning = false;
            CameraTransform.position = targetPos;
            CameraTransform.rotation = targetRot;
        }
    }

    private Transform GetTargetTransform(MenuState state)
    {
        switch (state)
        {
            case MenuState.TitleScreen: return titleScreenPosition;
            case MenuState.MainMenu: return mainMenuPosition;
            case MenuState.OptionsMenu: return optionsPosition;
            case MenuState.SubWindowOpen:
                return (stateBeforeSubWindow == MenuState.OptionsMenu) ? optionsPosition : mainMenuPosition;
            default: return mainMenuPosition;
        }
    }

    private void SetCameraPositionImmediate(Transform anchor)
    {
        if (anchor == null || CameraTransform == null) return;
        Vector3 pos = anchor.position;
        pos.z += Zoffset;
        CameraTransform.position = pos;
        CameraTransform.rotation = anchor.rotation;
        targetPos = pos;
        targetRot = anchor.rotation;
    }

    private void UpdateWheelsRotation()
    {
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
            float v = Input.GetAxisRaw("Vertical");
            float h = Input.GetAxisRaw("Horizontal");
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            float combinedInput = Mathf.Abs(v) > Mathf.Abs(h) ? v : h;

<<<<<<< HEAD:Assets/MainMenuOnly/MainMenuUIManager.cs
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
=======
            if (Mathf.Abs(scroll) > 0.01f) inputDirection = scroll > 0 ? -1 : 1;
            else if (Mathf.Abs(combinedInput) > 0.6f)
            {
                int currentDir = combinedInput > 0 ? -1 : 1;
                if (!isHolding || currentDir != lastDirection)
                {
>>>>>>> Build-Complet-2:Assets/MainMenuOnly/Scripts/MainMenuUIManager.cs
                    inputDirection = currentDir;
                    lastDirection = currentDir;
                    isHolding = true;
                    currentRepeatInterval = initialRepeatDelay;
                    nextActionTime = Time.unscaledTime + initialRepeatDelay;
                }
                else if (Time.unscaledTime >= nextActionTime)
                {
<<<<<<< HEAD:Assets/MainMenuOnly/MainMenuUIManager.cs
                    // DÉFILEMENT CONTINU
                    inputDirection = currentDir;

                    // On accélère doucement
=======
                    inputDirection = currentDir;
>>>>>>> Build-Complet-2:Assets/MainMenuOnly/Scripts/MainMenuUIManager.cs
                    currentRepeatInterval = Mathf.Max(minRepeatInterval, currentRepeatInterval - accelerationFactor);
                    nextActionTime = Time.unscaledTime + currentRepeatInterval;
                }
            }
<<<<<<< HEAD:Assets/MainMenuOnly/MainMenuUIManager.cs
            else
            {
                // Reset quand on lâche
                isHolding = false;
                lastDirection = 0;
            }
=======
            else { isHolding = false; lastDirection = 0; }
>>>>>>> Build-Complet-2:Assets/MainMenuOnly/Scripts/MainMenuUIManager.cs

            // Application du mouvement
            if (inputDirection != 0)
            {
                if (invertNavigation) inputDirection = -inputDirection;
                RotateWheel(inputDirection);
            }

<<<<<<< HEAD:Assets/MainMenuOnly/MainMenuUIManager.cs
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


=======
            if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return)) SelectCurrentWheelOption();
        }

        if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.Escape)) GoBack();
    }

>>>>>>> Build-Complet-2:Assets/MainMenuOnly/Scripts/MainMenuUIManager.cs
    private void RotateWheel(int direction)
    {
        float spawnDirection = reverseSpawnDirection ? -1f : 1f;
        if (currentState == MenuState.MainMenu)
        {
            currentMainIndex = (currentMainIndex + direction + mainMenuOptions.Length) % mainMenuOptions.Length;
            targetMainAngle = initialMainAngle + (-currentMainIndex * customAnglePerOption * spawnDirection);
        }
        else if (currentState == MenuState.OptionsMenu)
        {
            currentSettingsIndex = (currentSettingsIndex + direction + settingsOptions.Length) % settingsOptions.Length;
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

    private void GenerateWheel(WheelItem[] options, RectTransform wheelParent, List<RectTransform> generatedList, bool invertIdx)
    {
        if (buttonPrefab == null) return;
        for (int i = 0; i < options.Length; i++)
        {
            GameObject btnObj = Instantiate(buttonPrefab, wheelParent);
            btnObj.name = "Btn_" + options[i].itemName;
            RectTransform rectT = btnObj.GetComponent<RectTransform>();
            rectT.anchorMin = rectT.anchorMax = rectT.pivot = new Vector2(0.5f, 0.5f);
            if (options[i].customSize != Vector2.zero) rectT.sizeDelta = options[i].customSize;
            rectT.localScale = Vector3.one * normalScale;

            float dirMult = reverseSpawnDirection ? -1f : 1f;
            float k = invertIdx ? -i : i;
            float angleRad = ((k * customAnglePerOption * dirMult) + startAngleOffset) * Mathf.Deg2Rad;
            rectT.anchoredPosition = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad)) * wheelRadius;

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

    private void UpdateHoverEffects()
    {
        for (int i = 0; i < mainButtonsGenerated.Count; i++)
        {
            float targetS = (currentState == MenuState.MainMenu && i == currentMainIndex) ? selectedScale : normalScale;
            mainButtonsGenerated[i].localScale = Vector3.Lerp(mainButtonsGenerated[i].localScale, Vector3.one * targetS, Time.deltaTime * scaleAnimSpeed);
        }
        for (int i = 0; i < settingsButtonsGenerated.Count; i++)
        {
            float targetS = (currentState == MenuState.OptionsMenu && i == currentSettingsIndex) ? selectedScale : normalScale;
            settingsButtonsGenerated[i].localScale = Vector3.Lerp(settingsButtonsGenerated[i].localScale, Vector3.one * targetS, Time.deltaTime * scaleAnimSpeed);
        }
    }

    private void UpdateButtonsRotation()
    {
        if (!keepButtonsUpright) return;
        if (mainWheelRect != null)
        {
            float z = mainWheelRect.localEulerAngles.z;
            foreach (RectTransform btn in mainButtonsGenerated) btn.localRotation = Quaternion.Euler(0, 0, -z);
        }
        if (settingsWheelRect != null)
        {
            float z = settingsWheelRect.localEulerAngles.z;
            foreach (RectTransform btn in settingsButtonsGenerated) btn.localRotation = Quaternion.Euler(0, 0, -z);
        }
    }

    public void LaunchScene(string sceneName) { StartCoroutine(TransitionAndLoad(sceneName)); }

    private IEnumerator TransitionAndLoad(string sceneName)
    {
        ChangeState(MenuState.Loading);
        if (transitionScreen != null)
        {
            transitionScreen.blocksRaycasts = true;
            float time = 0;
            while (time < sceneTransitionDuration)
            {
                transitionScreen.alpha = Mathf.Lerp(0, 1, time / sceneTransitionDuration);
                time += Time.deltaTime;
                yield return null;
            }
            transitionScreen.alpha = 1;
        }
        SceneManager.LoadSceneAsync(sceneName);
    }

    public void LoadSettings()
    {
        currentResIndex = PlayerPrefs.GetInt("ResIndex", 0);
        currentFpsIndex = PlayerPrefs.GetInt("FpsIndex", 1);
        isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        isVsync = PlayerPrefs.GetInt("Vsync", 0) == 1;
        masterVol = PlayerPrefs.GetInt("MasterVol", 10);
        musicVol = PlayerPrefs.GetInt("MusicVol", 10);
        sfxVol = PlayerPrefs.GetInt("SfxVol", 10);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("ResIndex", currentResIndex);
        PlayerPrefs.SetInt("FpsIndex", currentFpsIndex);
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("Vsync", isVsync ? 1 : 0);
        PlayerPrefs.SetInt("MasterVol", masterVol);
        PlayerPrefs.SetInt("MusicVol", musicVol);
        PlayerPrefs.SetInt("SfxVol", sfxVol);
        PlayerPrefs.Save();
    }

    public void ApplySettings(bool shouldSave)
    {
        string[] resParts = resolutions[currentResIndex].Split('x');
        if (resParts.Length == 2) Screen.SetResolution(int.Parse(resParts[0]), int.Parse(resParts[1]), isFullscreen);
        QualitySettings.vSyncCount = isVsync ? 1 : 0;
        Application.targetFrameRate = fpsValues[currentFpsIndex];
        ApplyAudioVolumes();
        if (shouldSave) SaveSettings();
    }

    public void ApplyAudioVolumes()
    {
        if (mainAudioMixer != null)
        {
            mainAudioMixer.SetFloat("masterVolume", Mathf.Log10(Mathf.Max(masterVol, 0.0001f) / 10f) * 20f);
            mainAudioMixer.SetFloat("musicVolume", Mathf.Log10(Mathf.Max(musicVol, 0.0001f) / 10f) * 20f);
            mainAudioMixer.SetFloat("sfxVolume", Mathf.Log10(Mathf.Max(sfxVol, 0.0001f) / 10f) * 20f);
        }
        else AudioListener.volume = masterVol / 10f;
    }
}