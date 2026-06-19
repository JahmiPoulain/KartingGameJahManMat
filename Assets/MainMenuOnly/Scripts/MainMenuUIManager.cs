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
    public string itemName = "New Button";
    public Sprite buttonTexture;

    [Header("Text & Font")]
    public TMP_FontAsset customFont;

    public GameObject windowToOpen;

    [Header("Customization")]
    [Tooltip("Leave at 0,0 to use the prefab's default size.")]
    public Vector2 customSize = Vector2.zero;
}

public class MainMenuUIManager : MonoBehaviour
{
    public enum MenuState { TitleScreen, MainMenu, PlayGameModes, OptionsMenu, SubWindowOpen, Loading }

    [Header("State & Navigation")]
    public MenuState currentState = MenuState.TitleScreen;
    [Tooltip("Check this if Up/Down spins the wheel the wrong way.")]
    public bool invertNavigation = false;

    [SerializeField] private GameObject leaderboardCanvas;

    private static bool hasSeenTitleScreen = false;

    [Header("Wheels - General")]
    public GameObject buttonPrefab;
    public float customAnglePerOption = 45f;
    [Tooltip("On: spread buttons over 360 (endless rotation, buttons go all around). Off (default): keep the arc using the angle above.")]
    public bool fillFullCircle = false;
    public float startAngleOffset = 0f;
    public bool reverseSpawnDirection = false;
    public float wheelRadius = 150f;
    public float wheelRotationSpeed = 10f;
    public bool keepButtonsUpright = true;

    [Tooltip("Neighbor size = normalScale * shrink^distance (0.7 = each step 30% smaller). 1 = all neighbors at normalScale.")]
    public float neighborShrink = 0.7f;

    [Header("Vertical Picker")]
    [Tooltip("Stacked button list (previous on top, next below, smaller) while the steering wheel keeps spinning. Off = old arc wheel.")]
    public bool verticalCarousel = true;
    [Tooltip("Vertical spacing between buttons.")]
    public float verticalSpacing = 7f;
    [Tooltip("Horizontal (left/right) spacing between buttons. 0 = straight stack.")]
    public float horizontalSpacing = 0f;
    [Tooltip("Depth (Z) push of neighbors by distance: farther buttons go further back for a round, wheel-like silhouette. 0 = flat.")]
    public float neighborDepthPerStep = 0f;

    [Tooltip("Invert the vertical scroll direction, per wheel (if Down makes buttons go up).")]
    public bool invertMainScroll = false;
    public bool invertSettingsScroll = true;
    public bool invertPlayScroll = true;
    [Tooltip("Number of buttons visible on each side of the selected one (rest is hidden).")]
    public int visibleEachSide = 1;
    [Tooltip("Vertical scroll animation speed.")]
    public float carouselLerpSpeed = 10f;
    [Tooltip("Steering wheel rotation per navigation step, in degrees.")]
    public float volantDegreesPerStep = 20f;
    [Tooltip("Steering wheel rotation speed.")]
    public float volantSpinSpeed = 8f;
    [Tooltip("Invert the steering wheel spin direction, per wheel.")]
    public bool invertMainVolant = false;
    public bool invertSettingsVolant = false;
    public bool invertPlayVolant = false;

    [Header("Per-Wheel Button Offset (X = left/right, Y = up/down)")]
    [Tooltip("Offset for the Main menu wheel buttons. Negative X = left.")]
    public Vector2 mainButtonsOffset = Vector2.zero;
    [Tooltip("Offset for the Options wheel buttons. Positive X = right.")]
    public Vector2 settingsButtonsOffset = Vector2.zero;
    [Tooltip("Offset for the Play wheel buttons. Positive X = right.")]
    public Vector2 playButtonsOffset = Vector2.zero;

    private Transform mainVolant, settingsVolant, playVolant;
    private Quaternion mainVolantBase, settingsVolantBase, playVolantBase;

    [Header("Camera Movement")]
    public Transform CameraTransform;
    public float Zoffset = 10;
    public Transform titleScreenPosition;
    public Transform mainMenuPosition;
    public Transform optionsPosition;

    [Header("Dynamic Transition")]
    [Tooltip("Curve rising to 1.1 then back to 1.0 for a momentum effect.")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float transitionDuration = 0.8f;

    private float transitionTimer = 0f;
    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 targetPos;
    private Quaternion targetRot;
    private bool isTransitioning = false;

    [Header("Advanced Navigation")]
    public float initialRepeatDelay = 0.4f;
    public float minRepeatInterval = 0.1f;
    public float accelerationFactor = 0.02f;

    [Tooltip("Minimum delay between two scroll steps (higher = less sensitive wheel).")]
    public float scrollStepCooldown = 0.15f;
    private float nextScrollTime = 0f;

    [Tooltip("Hold time (seconds) before buttons hide. Higher = must hold longer.")]
    public float hideButtonsHoldDelay = 0.6f;
    private float holdStartTime = 0f;

    private float nextActionTime = 0f;
    private float currentRepeatInterval;
    private int lastDirection = 0;
    private bool isHolding = false;
    private bool isAutoRepeating = false;

    private float mainAnglePer, settingsAnglePer, playAnglePer;
    private int mainAccum = 0, settingsAccum = 0, playAccum = 0;

    [Header("Scene Transition")]
    public CanvasGroup transitionScreen;
    public float sceneTransitionDuration = 1f;

    public static MainMenuUIManager Instance;

    [Header("UI Panels")]
    public GameObject titleScreenPanel;
    public GameObject roze;

    [Header("Hover Visuals")]
    public float selectedScale = 1.3f;
    public float normalScale = 1.0f;
    public float scaleAnimSpeed = 12f;

    [Header("Main Menu Wheel")]
    public RectTransform mainWheelRect;
    public WheelItem[] mainMenuOptions;
    private int currentMainIndex = 0;
    private float targetMainAngle = 0f;
    private float initialMainAngle = 0f;
    private List<RectTransform> mainButtonsGenerated = new List<RectTransform>();

    [Header("Settings Wheel")]
    public RectTransform settingsWheelRect;
    public WheelItem[] settingsOptions;
    private int currentSettingsIndex = 0;
    private float targetSettingsAngle = 0f;
    private float initialSettingsAngle = 0f;
    private List<RectTransform> settingsButtonsGenerated = new List<RectTransform>();

    [Header("Play Wheel")]
    public RectTransform playWheelRect;
    public WheelItem[] playButtons;
    private int currentPlayIndex = 0;
    private float targetPlayAngle = 0f;
    private float InitialPlayAngle = 0f;
    private List<RectTransform> playButtonsGenerated = new List<RectTransform>();

    [Header("Off-Screen Positions")]
    [Tooltip("X/Y coordinates when the wheel is hidden (e.g. X = -1500 to push it fully left).")]
    public Vector2 mainWheelInactivePos;
    public Vector2 settingsWheelInactivePos;
    public Vector2 playWheelInactivePos;

    [Tooltip("Wheel slide speed.")]
    public float wheelMoveSpeed = 8f;

    private Vector2 mainWheelActivePos;
    private Vector2 settingsWheelActivePos;
    private Vector2 playWheelActivePos;

    [Header("Audio")]
    public AudioMixer mainAudioMixer;
    public int masterVol = 10;
    public int musicVol = 10;
    public int sfxVol = 10;

    [Header("Gameplay")]
    public bool isMapInverted = false;

    public string controlsSaveKey = ("Controles");

    private GameObject currentActiveWindow = null;
    private MenuState stateBeforeSubWindow = MenuState.MainMenu;

    [Header("Video")]
    public string[] resolutions = { "1920x1080", "1600x900", "1280x720", "800x600" };
    public int[] fpsValues = { 30, 60, 120, -1 };
    public string[] fpsLabels = { "30", "60", "120", "Unlimited" };

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

    private void UpdateVerticalCarousels()
    {
        UpdateVerticalCarousel(mainButtonsGenerated, currentMainIndex, mainButtonsOffset, invertMainScroll);
        UpdateVerticalCarousel(settingsButtonsGenerated, currentSettingsIndex, settingsButtonsOffset, invertSettingsScroll);
        UpdateVerticalCarousel(playButtonsGenerated, currentPlayIndex, playButtonsOffset, invertPlayScroll);

        SpinVolant(mainVolant, mainVolantBase, mainAccum, invertMainVolant);
        SpinVolant(settingsVolant, settingsVolantBase, settingsAccum, invertSettingsVolant);
        SpinVolant(playVolant, playVolantBase, playAccum, invertPlayVolant);
    }

    private Transform FindVolant(RectTransform wheelParent)
    {
        if (wheelParent == null) return null;
        foreach (Transform child in wheelParent)
            if (child.name.ToLower().Contains("wheel")) return child;
        return null;
    }

    private void SpinVolant(Transform volant, Quaternion baseRot, int accum, bool invert)
    {
        if (volant == null) return;
        float dir = invert ? -1f : 1f;
        Quaternion target = baseRot * Quaternion.Euler(0f, -accum * volantDegreesPerStep * dir, 0f);
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

            int off = SignedOffset(i - selectedIndex, n);
            int dist = Mathf.Abs(off);
            bool visible = dist <= visibleEachSide;

            Vector2 targetPos = buttonsOffset + new Vector2(off * horizontalSpacing, -off * verticalSpacing * ySign);
            float targetScale = visible ? ((off == 0) ? selectedScale : normalScale * Mathf.Pow(neighborShrink, dist)) : 0f;

            if (isAutoRepeating) targetScale = 0f;

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

            float targetZ = Mathf.Abs(off) * neighborDepthPerStep;
            Vector3 lp = rt.localPosition;
            lp.z = Mathf.Lerp(lp.z, targetZ, Time.deltaTime * carouselLerpSpeed);
            rt.localPosition = lp;

            rt.localRotation = Quaternion.identity;
        }
    }

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
                    isAutoRepeating = false;
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

    private float AnglePer(int count) => (fillFullCircle && count > 0) ? 360f / count : customAnglePerOption;

    private static int Mod(int a, int b) => b <= 0 ? 0 : ((a % b) + b) % b;

    private void RotateWheel(int direction)
    {
        if (currentState == MenuState.MainMenu) { mainAccum += direction; ApplyMainWheel(); }
        else if (currentState == MenuState.OptionsMenu) { settingsAccum += direction; ApplySettingsWheel(); }
        else if (currentState == MenuState.PlayGameModes) { playAccum += direction; ApplyPlayWheel(); }
    }

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
                Debug.Log("Map inverted is now: " + isMapInverted);

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

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        if (canvas.worldCamera != null) return canvas.worldCamera;
        if (CameraTransform != null)
        {
            Camera c = CameraTransform.GetComponent<Camera>();
            if (c != null) return c;
        }
        return Camera.main;
    }

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

        Debug.LogWarning($"GameSceneManager missing. Loading {sceneName} directly from the menu.");
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
