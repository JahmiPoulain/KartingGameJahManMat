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

    [Header("--- Transition de Scène ---")]
    [Tooltip("Un CanvasGroup noir (ou autre) qui va faire un fondu au noir.")]
    public CanvasGroup transitionScreen;
    public float transitionDuration = 1f;

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
        Time.timeScale = 1f;
        Instance = this;
    }

    void Start()
    {
        // 1. VÉRIFICATION DU SKIP DE L'ÉCRAN TITRE
        if (hasSeenTitleScreen)
        {
            currentState = MenuState.MainMenu;

            // On téléporte la caméra direct au menu principal pour éviter qu'elle "vole" depuis l'écran titre au chargement
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
        }

        // 2. ASSURER QUE L'ÉCRAN DE TRANSITION EST INVISIBLE AU DÉMARRAGE
        if (transitionScreen != null)
        {
            transitionScreen.alpha = 0f;
            transitionScreen.blocksRaycasts = false;
        }

        if (mainWheelRect != null) initialMainAngle = mainWheelRect.localEulerAngles.z;
        if (settingsWheelRect != null) initialSettingsAngle = settingsWheelRect.localEulerAngles.z;

        targetMainAngle = initialMainAngle;
        targetSettingsAngle = initialSettingsAngle;

        GenerateWheel(mainMenuOptions, mainWheelRect, mainButtonsGenerated, true);
        GenerateWheel(settingsOptions, settingsWheelRect, settingsButtonsGenerated, false);

        // Chargement et application des paramètres au démarrage
        LoadSettings();
        ApplySettings(false);
    }

    void Update()
    {
        // Si on est en train de charger, on bloque tous les inputs et animations
        if (currentState == MenuState.Loading) return;

        HandleInputs();
        SmoothTransitions();
        UpdateButtonsRotation();
        UpdateHoverEffects();
    }

    // ==========================================
    // 0. GESTION DES PARAMÈTRES (SAUVEGARDE / CHARGEMENT)
    // ==========================================
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
            hasSeenTitleScreen = true; // <--- ON MÉMORISE QU'ON A PASSÉ L'ÉCRAN TITRE
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

            if (selectedName.Contains("play") || selectedName.Contains("jouer"))
            {
                if (currentItem.windowToOpen != null) OpenWindow(currentItem.windowToOpen);
                else LaunchScene("TaSceneDeJeuIci"); // Remplacer par le nom de ta scène
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

    // ==========================================
    // 5. CHARGEMENT DE SCÈNE (TRANSITION)
    // ==========================================
    public void LaunchScene(string sceneName)
    {
        StartCoroutine(TransitionAndLoad(sceneName));
    }

    private IEnumerator TransitionAndLoad(string sceneName)
    {
        // On passe en état Loading pour bloquer les inputs
        ChangeState(MenuState.Loading);

        // 1. Fondu au noir (ou autre écran de transition)
        if (transitionScreen != null)
        {
            transitionScreen.blocksRaycasts = true; // Bloque les clics souris au cas où
            float time = 0;
            while (time < transitionDuration)
            {
                transitionScreen.alpha = Mathf.Lerp(0, 1, time / transitionDuration);
                time += Time.deltaTime;
                yield return null; // Attend la prochaine frame
            }
            transitionScreen.alpha = 1;
        }

        // 2. Lancement de la scène en arrière-plan
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // On attend que la scène soit complètement chargée
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}