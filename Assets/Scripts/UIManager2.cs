using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager2 : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuUI;
    public GameObject mainCanva;
    private bool isPaused = false;

    public bool canPause = true;

    public static UIManager2 instance;

    [SerializeField] private GameObject f3Canvas; // for F3 menu, show fps, debug stuff, coords, etc.
    [SerializeField] private TMP_Text f3StatsText; // le texte dans le f3Canvas qui affiche les stats
    [SerializeField] private float f3RefreshRate = 0.25f; // fréquence de mise à jour de l'affichage (secondes)

    // Internes pour le calcul du FPS et le rafraîchissement
    private float f3Timer = 0f;
    private int f3FrameCount = 0;
    private float f3FpsAccumulator = 0f;
    private float f3FpsMin = float.MaxValue;
    private float f3FpsMax = 0f;


    [Header("Input System")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("Navigation (0: Continuer, 1: Restart, 2: Quitter)")]
    public Transform pointeur;
    public int index = 0;
    public GameObject[] selectables;
    public Vector3 Offset;

    [Header("Feedback Visuel")]
    public float selectedScale = 1.15f;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    private Vector3[] defaultScales;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip soundNav;
    public AudioClip soundSubmit;

    [Header("Post Processing")]
    public Volume globalVolume;
    private DepthOfField depthOfField;

    private bool isVerticalAxisInUse = false;
    private Canvas parentCanvas;
    public float floatingSpeed = 12;
    public float floatingAmount = 0.2f;

    [Header("Resolution Setup")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);



    #region Initialisation et Inputs
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        defaultScales = new Vector3[selectables.Length];
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null) defaultScales[i] = selectables[i].transform.localScale;
        }

        if (globalVolume != null && globalVolume.profile != null)
            globalVolume.profile.TryGet(out depthOfField);

        if (depthOfField != null) depthOfField.active = false;

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && !parentCanvas.isRootCanvas) parentCanvas = parentCanvas.rootCanvas;
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }
        AnimatePointer(true);
    }

    void AnimatePointer(bool snapImmediately)
    {
        if (pointeur == null || selectables == null || selectables.Length == 0 || index >= selectables.Length)
            return;

        // 1. On calcule le ratio de l'écran par rapport à ton 1920x1080
        float ratioX = Screen.width / referenceResolution.x;
        float ratioY = Screen.height / referenceResolution.y;

        // 2. On applique ce ratio directement sur ton Offset
        Vector3 dynamicOffset = new Vector3(Offset.x * ratioX, Offset.y * ratioY, Offset.z);
        Vector3 basePosition = selectables[index].transform.position + dynamicOffset;

        if (snapImmediately)
        {
            pointeur.position = basePosition;
            return;
        }

        // 3. On applique aussi le ratioY sur le flottement pour pas qu'il soit énorme sur un petit écran
        float waveY = Mathf.Sin(Time.unscaledTime * floatingSpeed) * (floatingAmount * ratioY);

        pointeur.position = new Vector3(basePosition.x, basePosition.y + waveY, basePosition.z);
    }

    
    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }
    #endregion

    void Update()
    {
        if (isPaused)
        {
            HandleNavigation();

            if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                InteractWithCurrentSelection();
            }
        }
        AnimatePointer(false);

        HandleF3Overlay();
    }

    #region Debug Overlay (F3)
    void HandleF3Overlay()
    {
        // Toggle on/off avec F3
        if (Input.GetKeyDown(KeyCode.F3) && f3Canvas != null)
        {
            f3Canvas.SetActive(!f3Canvas.activeSelf);
            // On reset les compteurs à chaque ouverture pour des valeurs propres
            if (f3Canvas.activeSelf) ResetF3Counters();
        }

        if (f3Canvas == null || !f3Canvas.activeSelf || f3StatsText == null) return;

        // On utilise unscaledDeltaTime pour que le FPS reste correct même en pause (timeScale = 0)
        float dt = Time.unscaledDeltaTime;
        f3FpsAccumulator += dt;
        f3FrameCount++;

        // Rafraîchissement de l'affichage à intervalle régulier (évite que ça clignote trop vite)
        f3Timer += dt;
        if (f3Timer >= f3RefreshRate)
        {
            float avgFrameTime = f3FpsAccumulator / Mathf.Max(1, f3FrameCount);
            float fps = avgFrameTime > 0f ? 1f / avgFrameTime : 0f;

            if (fps < f3FpsMin) f3FpsMin = fps;
            if (fps > f3FpsMax) f3FpsMax = fps;

            f3StatsText.text = BuildF3Text(fps, avgFrameTime * 1000f);

            f3Timer = 0f;
            f3FpsAccumulator = 0f;
            f3FrameCount = 0;
        }
    }

    void ResetF3Counters()
    {
        f3Timer = 0f;
        f3FpsAccumulator = 0f;
        f3FrameCount = 0;
        f3FpsMin = float.MaxValue;
        f3FpsMax = 0f;
    }

    string BuildF3Text(float fps, float frameTimeMs)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(256);

        sb.AppendLine("<b>DEBUG (F3)</b>");
        sb.AppendLine($"FPS  : {fps:0} ({frameTimeMs:0.0} ms)");
        sb.AppendLine($"Min/Max : {(f3FpsMin == float.MaxValue ? 0 : f3FpsMin):0} / {f3FpsMax:0}");

        // Coordonnées du joueur
        if (KartScriptV2.instance != null)
        {
            Vector3 p = KartScriptV2.instance.transform.position;
            float speed = KartScriptV2.instance.rb != null ? KartScriptV2.instance.rb.linearVelocity.magnitude : 0f;
            sb.AppendLine($"Pos  : X {p.x:0.0}  Y {p.y:0.0}  Z {p.z:0.0}");
            sb.AppendLine($"Speed: {speed * 3.6f:0} km/h");
        }
        else
        {
            sb.AppendLine("Pos  : (no kart)");
        }

        // Stats de rendu : batches / triangles / verts.
        // UnityStats n'existe que dans l'éditeur, donc on garde ça sous #if UNITY_EDITOR.
#if UNITY_EDITOR
        sb.AppendLine($"Batches : {UnityEditor.UnityStats.batches}");
        sb.AppendLine($"SetPass : {UnityEditor.UnityStats.setPassCalls}");
        sb.AppendLine($"Tris    : {UnityEditor.UnityStats.triangles:n0}");
        sb.AppendLine($"Verts   : {UnityEditor.UnityStats.vertices:n0}");
        sb.AppendLine($"DrawCalls : {UnityEditor.UnityStats.drawCalls}");
#else
        sb.AppendLine("Batches/Tris : éditeur uniquement");
#endif

        // Mémoire & système
        long monoMem = System.GC.GetTotalMemory(false) / (1024 * 1024);
        sb.AppendLine($"Mem (mono) : {monoMem} MB");
        sb.AppendLine($"Reserved : {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024)} MB");
        sb.AppendLine($"Res  : {Screen.width}x{Screen.height} @{(int)Screen.currentResolution.refreshRateRatio.value}Hz");
        sb.AppendLine($"VSync : {QualitySettings.vSyncCount}  TargetFPS : {Application.targetFrameRate}");
        sb.AppendLine($"Unity : {Application.unityVersion}");

        return sb.ToString();
    }
    #endregion

    public void Resume()
    {
        // Remplacer par ta propre logique de jeu
        // KartScriptV2.instance.CanDrive = true; 
        mainCanva.SetActive(true);
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        if (depthOfField != null) depthOfField.active = false;
    }

    void Pause()
    {
        if (canPause == false) return;
        // KartScriptV2.instance.CanDrive = false;
        mainCanva.SetActive(false);
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        index = 0;
        UpdateVisuals();
        if (depthOfField != null) depthOfField.active = true;
    }

    #region Navigation et Visuels
    void HandleNavigation()
    {
        float v = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.UpArrow)) { MoveSelection(-1); isVerticalAxisInUse = true; }
        else if (Input.GetKeyDown(KeyCode.DownArrow)) { MoveSelection(1); isVerticalAxisInUse = true; }
        else if (Mathf.Abs(v) > 0.5f)
        {
            if (!isVerticalAxisInUse)
            {
                int dir = v < -0.5f ? 1 : -1;
                MoveSelection(dir);
                isVerticalAxisInUse = true;
            }
        }
        else isVerticalAxisInUse = false;
    }

    void MoveSelection(int dir)
    {
        int oldIndex = index;
        index = Mathf.Clamp(index + dir, 0, selectables.Length - 1);

        if (index != oldIndex)
        {
            UpdateVisuals();
            PlaySfx(soundNav);
        }
    }

    void UpdateVisuals()
    {
        // On recalcule le ratio ici aussi pour l'appliquer instantanément quand tu changes de bouton
        float ratioX = Screen.width / referenceResolution.x;
        float ratioY = Screen.height / referenceResolution.y;
        Vector3 dynamicOffset = new Vector3(Offset.x * ratioX, Offset.y * ratioY, Offset.z);

        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] == null) continue;

            if (i == index)
            {
                selectables[i].transform.localScale = defaultScales[i] * selectedScale;
                SetColorRecursive(selectables[i].transform, selectedColor);
                if (pointeur != null)
                {
                    pointeur.position = selectables[i].transform.position + dynamicOffset;
                }
            }
            else
            {
                selectables[i].transform.localScale = defaultScales[i];
                SetColorRecursive(selectables[i].transform, normalColor);
            }
        }
    }
    #endregion

    void InteractWithCurrentSelection()
    {
        PlaySfx(soundSubmit);
        if (index == 0) Resume();
        else if (index == 1) Restart();
        else if (index == 2) QuitToMainMenu();
    }

    void Restart()
    {
        // 1. On remet impérativement le temps à 1 pour débloquer Unity et les Coroutines
        Time.timeScale = 1f;

        // 2. SÉCURITÉ AUDIO : On coupe les bruits en boucle (moteur, dérapage dynamique...)
        // pour éviter qu'ils continuent pendant la transition vidéo
        if (KartScriptV2.instance != null)
        {
            AudioSource[] kartSources = KartScriptV2.instance.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource src in kartSources)
            {
                if (src.loop) src.Stop();
            }
        }

        // 3. On coupe l'effet de flou du menu pause
        if (depthOfField != null) depthOfField.active = false;

        // 4. On récupère le nom de la scène de circuit (progScene) actuellement active
        string currentProgSceneName = SceneManager.GetActiveScene().name;

        // 5. On demande au GameSceneManager de relancer le duo (GraphScene + cette progScene)
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.RestartRace(currentProgSceneName);
        }
        else
        {
            // Secours brut si le manager global est introuvable
            SceneManager.LoadScene(currentProgSceneName);
        }
    }

    public void QuitToMainMenu()
    {
        InversionCatcher.instance.Inverted = false;
        GameSceneManager.Instance.ReturnToMainMenu();
    }

    void SetColorRecursive(Transform parent, Color c)
    {
        TMP_Text text = parent.GetComponent<TMP_Text>();
        if (text != null) text.color = c;

        Image img = parent.GetComponent<Image>();
        if (img != null && img.gameObject.name != "Background") img.color = c;

        foreach (Transform child in parent) SetColorRecursive(child, c);
    }

    void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
