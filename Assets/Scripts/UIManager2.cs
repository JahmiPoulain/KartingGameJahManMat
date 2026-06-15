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

    [Header("Input System")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("Navigation (0: Continuer, 1: Quitter)")]
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
    }

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

        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveSelection(-1);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(1);
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
        Time.timeScale = 1f;


        if (KartScriptV2.instance != null)
        {
            AudioSource[] kartSources = KartScriptV2.instance.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource src in kartSources)
            {
                if (src.loop) src.Stop();
            }
        }

        if (depthOfField != null) depthOfField.active = false;


        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMainMenu()
    {
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
