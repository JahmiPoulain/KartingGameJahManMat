using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

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
    public enum MenuState { TitleScreen, MainMenu, PlayGameModes, OptionsMenu, SubWindowOpen, Loading }

    [Header("--- États & Navigation ---")]
    public MenuState currentState = MenuState.TitleScreen;
    [Tooltip("Coche ça si tu trouves que Haut/Bas fait tourner la roue dans le mauvais sens !")]
    public bool invertNavigation = false;

    [SerializeField] private GameObject leaderboardCanvas;

    private static bool hasSeenTitleScreen = false;

    [Header("--- Configuration Générale des Roues ---")]
    public GameObject buttonPrefab;
    public float customAnglePerOption = 45f;
    [Tooltip("Coché : répartit les boutons sur 360° (rotation continue infinie, mais étale les boutons tout autour). Décoché (par défaut) : garde ton arc avec l'angle ci-dessus.")]
    public bool fillFullCircle = false;
    public float startAngleOffset = 0f;
    public bool reverseSpawnDirection = false;
    public float wheelRadius = 150f;
    public float wheelRotationSpeed = 10f;
    public bool keepButtonsUpright = true;

    [Tooltip("Taille des boutons voisins = normalScale * shrink^distance (0.7 = chaque cran 30% plus petit que le précédent). 1 = tous les voisins à normalScale.")]
    public float neighborShrink = 0.7f;

    [Header("--- Picker Vertical (boutons empilés + volant qui tourne) ---")]
    [Tooltip("Boutons en liste verticale (précédent en haut, suivant en bas, plus petits) ET le volant 'wheel' continue de tourner. Décoché = ancienne roue en arc.")]
    public bool verticalCarousel = true;
    [Tooltip("Espacement vertical entre deux boutons (mode vertical uniquement).")]
    public float verticalSpacing = 7f;
    [Tooltip("Espacement horizontal (gauche/droite) entre deux boutons. 0 = boutons empilés droits. Indépendant du reste.")]
    public float horizontalSpacing = 0f;
    [Tooltip("Recul en profondeur (Z) des voisins selon leur éloignement : plus un bouton est loin du centre, plus il part en arrière -> galbe rond façon volant. 0 = tout à plat.")]
    public float neighborDepthPerStep = 0f;

    [Tooltip("Inverse le sens de défilement vertical (si descendre fait monter les boutons). Un réglage par roue.")]
    public bool invertMainScroll = false;
    public bool invertSettingsScroll = true;
    public bool invertPlayScroll = true;
    [Tooltip("Nombre de boutons visibles de chaque côté du sélectionné (le reste est masqué).")]
    public int visibleEachSide = 1;
    [Tooltip("Vitesse d'animation du défilement vertical.")]
    public float carouselLerpSpeed = 10f;
    [Tooltip("Rotation du volant ('wheel') par cran de navigation, en degrés.")]
    public float volantDegreesPerStep = 20f;
    [Tooltip("Vitesse de rotation du volant.")]
    public float volantSpinSpeed = 8f;

    [Header("--- Décalage des boutons par roue (X = gauche/droite, Y = haut/bas) ---")]
    [Tooltip("Décale les boutons de la roue Menu principal. X négatif = vers la gauche.")]
    public Vector2 mainButtonsOffset = Vector2.zero;
    [Tooltip("Décale les boutons de la roue Options. X positif = vers la droite.")]
    public Vector2 settingsButtonsOffset = Vector2.zero;
    [Tooltip("Décale les boutons de la roue Play. X positif = vers la droite.")]
    public Vector2 playButtonsOffset = Vector2.zero;

    private Transform mainVolant, settingsVolant, playVolant;
    private Quaternion mainVolantBase, settingsVolantBase, playVolantBase;

    [Header("--- Mouvement Caméra/Écran ---")]
    public Transform CameraTransform;
    public float Zoffset = 10;
    public Transform titleScreenPosition;
    public Transform mainMenuPosition;
    public Transform optionsPosition;

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

    [Header("--- Navigation Avancée ---")]
    public float initialRepeatDelay = 0.4f;
    public float minRepeatInterval = 0.1f;
    public float accelerationFactor = 0.02f;

    [Tooltip("Délai minimum entre deux crans de molette (plus grand = molette moins sensible).")]
    public float scrollStepCooldown = 0.15f;
    private float nextScrollTime = 0f;

    [Tooltip("Durée de maintien (en secondes) avant que les boutons disparaissent. Plus grand = il faut maintenir plus longtemps.")]
    public float hideButtonsHoldDelay = 0.6f;
    private float holdStartTime = 0f;

    private float nextActionTime = 0f;
    private float currentRepeatInterval;
    private int lastDirection = 0;
    private bool isHolding = false;
    private bool isAutoRepeating = false;   // true quand la touche est maintenue (défilement rapide -> boutons masqués)

    // Angle entre deux boutons, calculé par roue (360/nb si fillFullCircle, sinon customAnglePerOption).
    private float mainAnglePer, settingsAnglePer, playAnglePer;
    // Compteurs de crans non bornés -> rotation continue/infinie (drift-free car les boutons font 360°).
    private int mainAccum = 0, settingsAccum = 0, playAccum = 0;

    [Header("--- Transition de Scène ---")]
    public CanvasGroup transitionScreen;
    public float sceneTransitionDuration = 1f;

    public static MainMenuUIManager Instance;

    [Header("--- UI Panels ---")]
    public GameObject titleScreenPanel;
    public GameObject roze;

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

    [Header("--- Roue Play ---")]
    public RectTransform playWheelRect;
    public WheelItem[] playButtons;
    private int currentPlayIndex = 0;
    private float targetPlayAngle = 0f;
    private float InitialPlayAngle = 0f;
    private List<RectTransform> playButtonsGenerated = new List<RectTransform>();

    [Header("--- Positions Hors-Écran (À la main) ---")]
    [Tooltip("Coordonnées X/Y quand la roue est masquée (ex: X = -1500 pour la mettre à gauche toute)")]
    public Vector2 mainWheelInactivePos;
    public Vector2 settingsWheelInactivePos;
    public Vector2 playWheelInactivePos;

    [Tooltip("Vitesse de glissement de la roue")]
    public float wheelMoveSpeed = 8f;

    private Vector2 mainWheelActivePos;
    private Vector2 settingsWheelActivePos;
    private Vector2 playWheelActivePos;

    [Header("--- Paramètres Audio ---")]
    public AudioMixer mainAudioMixer;
    public int masterVol = 10;
    public int musicVol = 10;
    public int sfxVol = 10;

    [Header("--- Paramètres de Jeu (Play) ---")]
    public bool isMapInverted = false;

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

    private void Awake() { if (Instance == null) Instance = this; else Destroy(this); }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        Time.timeScale = 1f;
        roze.SetActive(true);
        if (mainWheelRect != null) mainWheelActivePos = mainWheelRect.anchoredPosition;
        if (settingsWheelRect != null) settingsWheelActivePos = settingsWheelRect.anchoredPosition;
        if (playWheelRect != null) playWheelActivePos = playWheelRect.anchoredPosition;

        if (mainWheelRect != null) initialMainAngle = mainWheelRect.localEulerAngles.z;
        if (settingsWheelRect != null) initialSettingsAngle = settingsWheelRect.localEulerAngles.z;
        if (playWheelRect != null) InitialPlayAngle = playWheelRect.localEulerAngles.z;

        targetMainAngle = initialMainAngle;
        targetSettingsAngle = initialSettingsAngle;
        targetPlayAngle = InitialPlayAngle;

        // Le volant visuel est l'objet "wheel" déjà présent sous chaque conteneur (avant qu'on génère les boutons).
        mainVolant = FindVolant(mainWheelRect);
        settingsVolant = FindVolant(settingsWheelRect);
        playVolant = FindVolant(playWheelRect);
        if (mainVolant != null) mainVolantBase = mainVolant.localRotation;
        if (settingsVolant != null) settingsVolantBase = settingsVolant.localRotation;
        if (playVolant != null) playVolantBase = playVolant.localRotation;

        mainAnglePer = AnglePer(mainMenuOptions.Length);
        settingsAnglePer = AnglePer(settingsOptions.Length);
        playAnglePer = AnglePer(playButtons.Length);

        GenerateWheel(mainMenuOptions, mainWheelRect, mainButtonsGenerated, true, mainAnglePer);
        GenerateWheel(settingsOptions, settingsWheelRect, settingsButtonsGenerated, false, settingsAnglePer);
        GenerateWheel(playButtons, playWheelRect, playButtonsGenerated, false, playAnglePer);
        UpdateInvertedButtonText();

        if (verticalCarousel)
        {
            // Roues droites dès le départ pour que le picker vertical soit bien d'aplomb.
            if (mainWheelRect != null) mainWheelRect.localRotation = Quaternion.identity;
            if (settingsWheelRect != null) settingsWheelRect.localRotation = Quaternion.identity;
            if (playWheelRect != null) playWheelRect.localRotation = Quaternion.identity;
        }

        if (hasSeenTitleScreen)
        {
            currentState = MenuState.MainMenu;
            if (titleScreenPanel != null) titleScreenPanel.SetActive(false);
            SetCameraPositionImmediate(mainMenuPosition);

            if (mainWheelRect != null) mainWheelRect.anchoredPosition = mainWheelActivePos;
            if (settingsWheelRect != null) settingsWheelRect.anchoredPosition = settingsWheelInactivePos;
            if (playWheelRect != null) playWheelRect.anchoredPosition = playWheelInactivePos;
        }
        else
        {
            currentState = MenuState.TitleScreen;
            if (titleScreenPanel != null) titleScreenPanel.SetActive(true);
            SetCameraPositionImmediate(titleScreenPosition);

            if (mainWheelRect != null) mainWheelRect.anchoredPosition = mainWheelInactivePos;
            if (settingsWheelRect != null) settingsWheelRect.anchoredPosition = settingsWheelInactivePos;
            if (playWheelRect != null) playWheelRect.anchoredPosition = playWheelInactivePos;
        }

        if (transitionScreen != null)
        {
            transitionScreen.gameObject.SetActive(true);
            transitionScreen.alpha = 0f;
        }

        LoadSettings();
        ApplySettings(false);
        StartCoroutine(retirRoze());
    }

    void Update()
    {
        if (currentState == MenuState.Loading) return;
        if (ControlsSettings.IsRebinding) return;

        HandleInputs();
        SmoothTransitions();
        if (verticalCarousel)
        {
            UpdateVerticalCarousels();
        }
        else
        {
            UpdateButtonsRotation();
            UpdateHoverEffects();
        }
    }

    IEnumerator retirRoze()
    {
        yield return new WaitForSeconds(0.225f);
        roze.SetActive(false);

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

        Vector2 targetMainPos = mainWheelInactivePos;
        Vector2 targetSettingsPos = settingsWheelInactivePos;
        Vector2 targetPlayPos = playWheelInactivePos;

        switch (currentState)
        {
            case MenuState.MainMenu:
                targetMainPos = mainWheelActivePos;
                break;
            case MenuState.OptionsMenu:
                targetSettingsPos = settingsWheelActivePos;
                break;
            case MenuState.PlayGameModes:
                targetPlayPos = playWheelActivePos;
                break;
            case MenuState.SubWindowOpen:
                if (stateBeforeSubWindow == MenuState.OptionsMenu) targetSettingsPos = settingsWheelActivePos;
                else if (stateBeforeSubWindow == MenuState.PlayGameModes) targetPlayPos = playWheelActivePos;
                else targetMainPos = mainWheelActivePos;
                break;
        }

        if (mainWheelRect != null) mainWheelRect.anchoredPosition = Vector2.Lerp(mainWheelRect.anchoredPosition, targetMainPos, Time.deltaTime * wheelMoveSpeed);
        if (settingsWheelRect != null) settingsWheelRect.anchoredPosition = Vector2.Lerp(settingsWheelRect.anchoredPosition, targetSettingsPos, Time.deltaTime * wheelMoveSpeed);
        if (playWheelRect != null) playWheelRect.anchoredPosition = Vector2.Lerp(playWheelRect.anchoredPosition, targetPlayPos, Time.deltaTime * wheelMoveSpeed);

        if (!isTransitioning || CameraTransform == null) return;
        transitionTimer += Time.deltaTime;
        float t = transitionTimer / transitionDuration;
        float curveValue = transitionCurve.Evaluate(t);
        CameraTransform.position = Vector3.LerpUnclamped(startPos, targetPos, curveValue);
        CameraTransform.rotation = Quaternion.LerpUnclamped(startRot, targetRot, curveValue);
        if (t >= 1f) { isTransitioning = false; CameraTransform.position = targetPos; CameraTransform.rotation = targetRot; }
    }

    private Transform GetTargetTransform(MenuState state)
    {
        switch (state)
        {
            case MenuState.TitleScreen: return titleScreenPosition;
            case MenuState.MainMenu: return mainMenuPosition;
            case MenuState.PlayGameModes: return optionsPosition;
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
        // En mode picker vertical, les roues ne tournent pas : on les garde droites,
        // ce sont les boutons qui se positionnent verticalement.
        if (verticalCarousel)
        {
            if (mainWheelRect != null) mainWheelRect.localRotation = Quaternion.Lerp(mainWheelRect.localRotation, Quaternion.identity, Time.deltaTime * wheelRotationSpeed);
            if (settingsWheelRect != null) settingsWheelRect.localRotation = Quaternion.Lerp(settingsWheelRect.localRotation, Quaternion.identity, Time.deltaTime * wheelRotationSpeed);
            if (playWheelRect != null) playWheelRect.localRotation = Quaternion.Lerp(playWheelRect.localRotation, Quaternion.identity, Time.deltaTime * wheelRotationSpeed);
            return;
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

        if (playWheelRect != null)
        {
            Quaternion targetPlayRot = Quaternion.Euler(0, 0, targetPlayAngle);
            playWheelRect.localRotation = Quaternion.Lerp(playWheelRect.localRotation, targetPlayRot, Time.deltaTime * wheelRotationSpeed);
        }
    }

    // ----- Picker vertical : sélectionné gros au centre, voisins plus petits au-dessus/en-dessous -----

    private void UpdateVerticalCarousels()
    {
        UpdateVerticalCarousel(mainButtonsGenerated, currentMainIndex, mainButtonsOffset, invertMainScroll);
        UpdateVerticalCarousel(settingsButtonsGenerated, currentSettingsIndex, settingsButtonsOffset, invertSettingsScroll);
        UpdateVerticalCarousel(playButtonsGenerated, currentPlayIndex, playButtonsOffset, invertPlayScroll);

        // Le volant continue de tourner (autour de son axe) en fonction de la navigation.
        SpinVolant(mainVolant, mainVolantBase, mainAccum);
        SpinVolant(settingsVolant, settingsVolantBase, settingsAccum);
        SpinVolant(playVolant, playVolantBase, playAccum);
    }

    // Cherche l'objet "wheel" (le volant) déjà présent sous un conteneur de roue.
    private Transform FindVolant(RectTransform wheelParent)
    {
        if (wheelParent == null) return null;
        foreach (Transform child in wheelParent)
            if (child.name.ToLower().Contains("wheel")) return child;
        return null;
    }

    // Fait tourner le volant autour de son axe (Y local) proportionnellement au nombre de crans parcourus.
    private void SpinVolant(Transform volant, Quaternion baseRot, int accum)
    {
        if (volant == null) return;
        Quaternion target = baseRot * Quaternion.Euler(0f, -accum * volantDegreesPerStep, 0f);
        volant.localRotation = Quaternion.Slerp(volant.localRotation, target, Time.deltaTime * volantSpinSpeed);
    }

    private void UpdateVerticalCarousel(List<RectTransform> buttons, int selectedIndex, Vector2 buttonsOffset, bool invertScroll)
    {
        int n = buttons.Count;
        if (n == 0) return;

        float ySign = invertScroll ? -1f : 1f;

        for (int i = 0; i < n; i++)
        {
            RectTransform rt = buttons[i];
            if (rt == null) continue;

            int off = SignedOffset(i - selectedIndex, n);   // 0 = sélectionné, -1 = au-dessus, +1 = en-dessous
            int dist = Mathf.Abs(off);
            bool visible = dist <= visibleEachSide;

            Vector2 targetPos = buttonsOffset + new Vector2(off * horizontalSpacing, -off * verticalSpacing * ySign);
            float targetScale = visible ? ((off == 0) ? selectedScale : normalScale * Mathf.Pow(neighborShrink, dist)) : 0f;

            // Pendant un maintien (défilement rapide), on cache TOUS les boutons : seul le volant tourne,
            // l'index continue de défiler, et les boutons réapparaissent au relâchement.
            if (isAutoRepeating) targetScale = 0f;

            // Détection du bouclage : si le bouton doit faire un grand saut (passer du haut au bas ou inverse),
            // on le téléporte INVISIBLE de l'autre côté au lieu de lui faire traverser l'écran.
            // Sinon, il glisse normalement (et continue de glisser même en sortant avant de s'effacer).
            Vector2 step = new Vector2(horizontalSpacing, verticalSpacing);
            float stepMag = step.magnitude;
            bool wrapping = stepMag > 0.001f && Vector2.Distance(rt.anchoredPosition, targetPos) > stepMag * 1.5f;

            if (wrapping)
            {
                rt.anchoredPosition = targetPos;
                rt.localScale = Vector3.zero;
            }
            else
            {
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, Time.deltaTime * carouselLerpSpeed);
                rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * targetScale, Time.deltaTime * carouselLerpSpeed);
            }

            // Recul en Z des voisins (silhouette ronde) : la position X/Y vient d'être posée, on n'ajuste que la profondeur.
            float targetZ = Mathf.Abs(off) * neighborDepthPerStep;
            Vector3 lp = rt.localPosition;
            lp.z = Mathf.Lerp(lp.z, targetZ, Time.deltaTime * carouselLerpSpeed);
            rt.localPosition = lp;

            rt.localRotation = Quaternion.identity;   // les boutons restent horizontaux
        }
    }

    // Décalage signé de delta ramené dans [-n/2, n/2] (pour savoir de combien de crans un bouton est éloigné du centre).
    private int SignedOffset(int delta, int n)
    {
        if (n <= 0) return 0;
        int o = Mod(delta, n);
        if (o > n / 2) o -= n;
        return o;
    }


    public void GoBack()
    {
        if (currentState == MenuState.SubWindowOpen)
        {
            if (currentActiveWindow != null)
            {
                PlayerManagementPage playerManagementPage = currentActiveWindow.GetComponentInChildren<PlayerManagementPage>(true);
                if (playerManagementPage != null && playerManagementPage.TryHandleCancel())
                    return;

                CloseProfileManagementWindowIfNeeded(currentActiveWindow);
                currentActiveWindow.SetActive(false);
            }
            currentActiveWindow = null;
            ChangeState(stateBeforeSubWindow);
        }
        else if (currentState == MenuState.OptionsMenu)
        {
            ChangeState(MenuState.MainMenu);
        }
        else if (currentState == MenuState.PlayGameModes)
        {
            ChangeState(MenuState.MainMenu);
        }
    }

    private void CloseProfileManagementWindowIfNeeded(GameObject window)
    {
        if (window == null) return;
        if (window.GetComponentInChildren<PlayerManagementPage>(true) == null) return;

        ProfilsManager profilsManager = FindFirstObjectByType<ProfilsManager>();
        if (profilsManager != null)
            profilsManager.HidePlayerManagement();
    }

    private void HandleInputs()
    {
        if (currentState == MenuState.TitleScreen && Input.anyKeyDown)
        {
            hasSeenTitleScreen = true;
            if (titleScreenPanel != null) titleScreenPanel.SetActive(false);
            ChangeState(MenuState.MainMenu);
            return;
        }

        if (currentState == MenuState.MainMenu || currentState == MenuState.OptionsMenu || currentState == MenuState.PlayGameModes)
        {
            int inputDirection = 0;
            float v = Input.GetAxisRaw("Vertical");
            float h = Input.GetAxisRaw("Horizontal");
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            float combinedInput = Mathf.Abs(v) > Mathf.Abs(h) ? v : h;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Cooldown : on ignore les crans de molette trop rapprochés (molette moins sensible).
                if (Time.unscaledTime >= nextScrollTime)
                {
                    inputDirection = scroll > 0 ? -1 : 1;
                    nextScrollTime = Time.unscaledTime + scrollStepCooldown;
                }
            }
            else if (Mathf.Abs(combinedInput) > 0.6f)
            {
                int currentDir = combinedInput > 0 ? -1 : 1;
                if (!isHolding || currentDir != lastDirection)
                {
                    inputDirection = currentDir;
                    lastDirection = currentDir;
                    isHolding = true;
                    isAutoRepeating = false;   // simple appui : les boutons restent visibles
                    holdStartTime = Time.unscaledTime;
                    currentRepeatInterval = initialRepeatDelay;
                    nextActionTime = Time.unscaledTime + initialRepeatDelay;
                }
                else if (Time.unscaledTime >= nextActionTime)
                {
                    inputDirection = currentDir;
                    currentRepeatInterval = Mathf.Max(minRepeatInterval, currentRepeatInterval - accelerationFactor);
                    nextActionTime = Time.unscaledTime + currentRepeatInterval;
                }

                // On ne masque les boutons qu'après un vrai maintien prolongé (et pas juste un appui un peu long).
                if (Time.unscaledTime - holdStartTime >= hideButtonsHoldDelay) isAutoRepeating = true;
            }
            else { isHolding = false; isAutoRepeating = false; lastDirection = 0; }

            if (inputDirection != 0)
            {
                if (invertNavigation) inputDirection = -inputDirection;
                RotateWheel(inputDirection);
            }

            if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return)) SelectCurrentWheelOption();

            HandleMouseClick();
        }

        if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.Escape)) GoBack();
    }

    // Espacement angulaire d'une roue : 360/nb pour un cercle complet (rotation continue), sinon l'angle fixe.
    private float AnglePer(int count) => (fillFullCircle && count > 0) ? 360f / count : customAnglePerOption;

    private static int Mod(int a, int b) => b <= 0 ? 0 : ((a % b) + b) % b;

    private void RotateWheel(int direction)
    {
        if (currentState == MenuState.MainMenu) { mainAccum += direction; ApplyMainWheel(); }
        else if (currentState == MenuState.OptionsMenu) { settingsAccum += direction; ApplySettingsWheel(); }
        else if (currentState == MenuState.PlayGameModes) { playAccum += direction; ApplyPlayWheel(); }
    }

    // Met à jour l'angle cible (à partir du compteur non borné -> tours complets continus) et l'index sélectionné.
    // En mode arc (fillFullCircle off), on borne le compteur pour éviter toute dérive.
    private void ApplyMainWheel()
    {
        if (!fillFullCircle && !verticalCarousel) mainAccum = Mod(mainAccum, mainMenuOptions.Length);
        float spawnDirection = reverseSpawnDirection ? -1f : 1f;
        currentMainIndex = Mod(mainAccum, mainMenuOptions.Length);
        targetMainAngle = initialMainAngle + (-mainAccum * mainAnglePer * spawnDirection);
    }

    private void ApplySettingsWheel()
    {
        if (!fillFullCircle && !verticalCarousel) settingsAccum = Mod(settingsAccum, settingsOptions.Length);
        float spawnDirection = reverseSpawnDirection ? -1f : 1f;
        currentSettingsIndex = Mod(settingsAccum, settingsOptions.Length);
        targetSettingsAngle = initialSettingsAngle - (-settingsAccum * settingsAnglePer * spawnDirection);
    }

    private void ApplyPlayWheel()
    {
        if (!fillFullCircle && !verticalCarousel) playAccum = Mod(playAccum, playButtons.Length);
        float spawnDirection = reverseSpawnDirection ? -1f : 1f;
        currentPlayIndex = Mod(playAccum, playButtons.Length);
        targetPlayAngle = InitialPlayAngle - (-playAccum * playAnglePer * spawnDirection);
    }

    // Nombre de crans (signé) pour rejoindre targetIndex par le plus court chemin (utilisé au clic souris).
    private int ShortestStep(int accum, int targetIndex, int length)
    {
        if (length <= 0) return 0;
        int diff = targetIndex - Mod(accum, length);
        if (diff > length / 2) diff -= length;
        if (diff < -length / 2) diff += length;
        return diff;
    }

    private void SelectCurrentWheelOption()
    {
        if (currentState == MenuState.MainMenu)
        {
            string selectedName = mainMenuOptions[currentMainIndex].itemName.ToLower();
            WheelItem currentItem = mainMenuOptions[currentMainIndex];

            if (selectedName.Contains("play") || selectedName.Contains("jouer"))
            {
                ChangeState(MenuState.PlayGameModes);
            }
            else if (selectedName.Contains("setting") || selectedName.Contains("option"))
            {
                ChangeState(MenuState.OptionsMenu);
            }
            else if (selectedName.Contains("quit") || selectedName.Contains("quitter"))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(); 
#endif
            }
            else if (selectedName.Contains("leaderboard") || selectedName.Contains("leaderboard"))
            {
                OpenWindow(currentItem.windowToOpen);
                LeaderboardManager.Instance.ForceFocusOnNextButton();
            }
            else if (currentItem.windowToOpen != null)
            {
                OpenWindow(currentItem.windowToOpen);
            }
        }
        else if (currentState == MenuState.OptionsMenu)
        {
            WheelItem currentItem = settingsOptions[currentSettingsIndex];
            if (currentItem.windowToOpen != null) OpenWindow(currentItem.windowToOpen);
        }
        else if (currentState == MenuState.PlayGameModes)
        {
            string selectedName = playButtons[currentPlayIndex].itemName.ToLower();

            if (selectedName.Contains("inverted") || selectedName.Contains("toggle"))
            {
                isMapInverted = !isMapInverted;
                InversionCatcher.instance.CatchInversion(isMapInverted);
                Debug.Log("Map inversée est maintenant sur : " + isMapInverted);

                TextMeshProUGUI btnText = playButtonsGenerated[currentPlayIndex].GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = GetInvertedButtonLabel();
                }
            }
            else if (selectedName.Contains("time trial") || selectedName.Contains("contre la montre"))
            {
                GameManager.Instance().currentMode = GameManager.GameModeType.TimeTrial;
                LaunchScene("ProgScene"); 
            }
            else if (selectedName.Contains("time attack") || selectedName.Contains("time"))
            {
                GameManager.Instance().currentMode = GameManager.GameModeType.TimeAttack;
                LaunchScene("ProgScene");
            }
        }
    }

    private void OpenWindow(GameObject window)
    {
        if (window == null)
            return;

        currentActiveWindow = window;
        currentActiveWindow.SetActive(true);

        PlayerManagementPage playerManagementPage = currentActiveWindow.GetComponentInChildren<PlayerManagementPage>(true);
        if (playerManagementPage != null)
        {
            ProfilsManager profilsManager = currentActiveWindow.GetComponentInChildren<ProfilsManager>(true);
            if (profilsManager == null)
                profilsManager = FindFirstObjectByType<ProfilsManager>();

            if (profilsManager != null)
                profilsManager.OpenPlayerManagement();
        }
        stateBeforeSubWindow = currentState;
        ChangeState(MenuState.SubWindowOpen);
        StartCoroutine(FocusOpenedWindowNextFrame(currentActiveWindow));
    }

    private IEnumerator FocusOpenedWindowNextFrame(GameObject window)
    {
        yield return null;

        if (window == null || !window.activeInHierarchy)
            yield break;

        PlayerManagementPage playerManagementPage = window.GetComponentInChildren<PlayerManagementPage>(true);
        if (playerManagementPage != null && playerManagementPage.gameObject.activeInHierarchy)
        {
            playerManagementPage.FocusFirstSelectable();
            yield break;
        }

        LeaderboardManager leaderboardManager = window.GetComponentInChildren<LeaderboardManager>(true);
        if (leaderboardManager == null)
            leaderboardManager = LeaderboardManager.Instance;

        if (leaderboardManager != null)
        {
            leaderboardManager.FocusFirstControl();
            yield break;
        }

        Selectable selectable = window.GetComponentInChildren<Selectable>(false);
        if (selectable != null && selectable.interactable)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            else
                selectable.Select();
        }
    }

    private void GenerateWheel(WheelItem[] options, RectTransform wheelParent, List<RectTransform> generatedList, bool invertIdx, float anglePerOption)
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
            float angleRad = ((k * anglePerOption * dirMult) + startAngleOffset) * Mathf.Deg2Rad;
            rectT.anchoredPosition = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad)) * wheelRadius;

            Image img = btnObj.GetComponent<Image>();
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (img != null && options[i].buttonTexture != null) img.sprite = options[i].buttonTexture;
            if (txt != null)
            {
                txt.text = GetButtonLabel(options[i].itemName);
                txt.alignment = TextAlignmentOptions.Center;
                if (options[i].customFont != null) txt.font = options[i].customFont;
            }
            if (options[i].windowToOpen != null) options[i].windowToOpen.SetActive(false);
            generatedList.Add(rectT);
        }
    }

    // ----- Clic souris (détection manuelle, cohérente avec l'Input legacy du reste du script) -----

    private void HandleMouseClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        List<RectTransform> buttons;
        if (currentState == MenuState.MainMenu) buttons = mainButtonsGenerated;
        else if (currentState == MenuState.OptionsMenu) buttons = settingsButtonsGenerated;
        else if (currentState == MenuState.PlayGameModes) buttons = playButtonsGenerated;
        else return;

        if (buttons.Count == 0) return;

        Camera cam = GetCanvasCamera(buttons[0]);
        Vector2 mousePos = Input.mousePosition;

        int hitIndex = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < buttons.Count; i++)
        {
            RectTransform rt = buttons[i];
            if (rt == null || !rt.gameObject.activeInHierarchy) continue;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, cam)) continue;

            // En cas de chevauchement, on garde le bouton dont le centre est le plus proche du curseur.
            Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
            float d = (screenCenter - mousePos).sqrMagnitude;
            if (d < bestDist) { bestDist = d; hitIndex = i; }
        }

        if (hitIndex >= 0) OnWheelButtonClicked(hitIndex);
    }

    private Camera GetCanvasCamera(RectTransform anyButton)
    {
        Canvas canvas = anyButton.GetComponentInParent<Canvas>();
        if (canvas == null) return CameraTransform != null ? CameraTransform.GetComponent<Camera>() : Camera.main;
        canvas = canvas.rootCanvas;

        // Overlay -> pas de caméra. Sinon (World Space / Screen Space - Camera) il faut la vraie caméra
        // qui rend le canvas ; sur un canvas World Space, worldCamera est souvent null -> on prend celle de la scène.
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        if (canvas.worldCamera != null) return canvas.worldCamera;
        if (CameraTransform != null)
        {
            Camera c = CameraTransform.GetComponent<Camera>();
            if (c != null) return c;
        }
        return Camera.main;
    }

    // Recentre la roue active sur le bouton cliqué par le plus court chemin, puis valide
    // en réutilisant exactement la logique de SelectCurrentWheelOption().
    private void OnWheelButtonClicked(int index)
    {
        if (currentState == MenuState.MainMenu) { mainAccum += ShortestStep(mainAccum, index, mainMenuOptions.Length); ApplyMainWheel(); }
        else if (currentState == MenuState.OptionsMenu) { settingsAccum += ShortestStep(settingsAccum, index, settingsOptions.Length); ApplySettingsWheel(); }
        else if (currentState == MenuState.PlayGameModes) { playAccum += ShortestStep(playAccum, index, playButtons.Length); ApplyPlayWheel(); }
        else return;

        SelectCurrentWheelOption();
    }

    private string GetButtonLabel(string itemName)
    {
        string buttonName = itemName.ToLower();
        if (buttonName.Contains("inverted") || buttonName.Contains("toggle")) return GetInvertedButtonLabel();
        if (buttonName.Contains("time attack")) return "TIME\nATTACK";

        return itemName;
    }

    private string GetInvertedButtonLabel()
    {
        return isMapInverted ? "INVERTED\n(YES)" : "INVERTED\n(NO)";
    }

    private void UpdateInvertedButtonText()
    {
        for (int i = 0; i < playButtons.Length && i < playButtonsGenerated.Count; i++)
        {
            string buttonName = playButtons[i].itemName.ToLower();
            if (!buttonName.Contains("inverted") && !buttonName.Contains("toggle")) continue;

            TextMeshProUGUI btnText = playButtonsGenerated[i].GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = GetInvertedButtonLabel();
            }
        }
    }

    private void UpdateHoverEffects()
    {
        ApplyWheelScales(mainButtonsGenerated, currentMainIndex, currentState == MenuState.MainMenu);
        ApplyWheelScales(settingsButtonsGenerated, currentSettingsIndex, currentState == MenuState.OptionsMenu);
        ApplyWheelScales(playButtonsGenerated, currentPlayIndex, currentState == MenuState.PlayGameModes);
    }

    // Taille dégressive : le bouton sélectionné est gros (selectedScale), ses voisins de plus en plus petits
    // selon leur éloignement (normalScale * neighborShrink^distance) -> les "prochains" boutons paraissent plus petits.
    private void ApplyWheelScales(List<RectTransform> buttons, int selectedIndex, bool isActiveState)
    {
        int n = buttons.Count;
        for (int i = 0; i < n; i++)
        {
            if (buttons[i] == null) continue;

            float targetS;
            if (!isActiveState) targetS = normalScale;
            else
            {
                int dist = Mathf.Abs(SignedOffset(i - selectedIndex, n));
                targetS = (dist == 0) ? selectedScale : normalScale * Mathf.Pow(neighborShrink, dist);
            }
            buttons[i].localScale = Vector3.Lerp(buttons[i].localScale, Vector3.one * targetS, Time.deltaTime * scaleAnimSpeed);
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
        if (playWheelRect != null)
        {
            float z = playWheelRect.localEulerAngles.z;
            foreach (RectTransform btn in playButtonsGenerated) btn.localRotation = Quaternion.Euler(0, 0, -z);
        }
    }

    public void LaunchScene(string sceneName)
    {
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadGame(sceneName);
            return;
        }

        Debug.LogWarning($"GameSceneManager absent. Chargement direct de {sceneName} depuis le menu.");
        StartCoroutine(TransitionAndLoad(sceneName));
    }
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
        SceneManager.LoadSceneAsync(sceneName,LoadSceneMode.Additive);
        SceneManager.LoadSceneAsync("GraphScene", LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("MainMenu2_0",UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
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
