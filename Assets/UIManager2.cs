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
    private bool isPaused = false;

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

    #region Initialisation et Inputs
    void Awake()
    {
        // Setup des échelles par défaut
        defaultScales = new Vector3[selectables.Length];
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null) defaultScales[i] = selectables[i].transform.localScale;
        }

        // Setup du Post-Po
        if (globalVolume != null && globalVolume.profile != null)
            globalVolume.profile.TryGet(out depthOfField);

        // Setup du Canvas
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
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    // Cette fonction est appelée par le New Input System
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
    }

    public void Resume()
    {
        KartScriptV2.instance.canDrive = true;
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        if (depthOfField != null) depthOfField.active = false;
    }

    void Pause()
    {
        KartScriptV2.instance.canDrive = false;
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
        float currentScaleFactor = parentCanvas != null ? parentCanvas.scaleFactor : 1f;

        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] == null) continue;

            if (i == index)
            {
                selectables[i].transform.localScale = defaultScales[i] * selectedScale;
                SetColorRecursive(selectables[i].transform, selectedColor);
                if (pointeur != null)
                    pointeur.position = selectables[i].transform.position + (Offset * currentScaleFactor);
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
        else if (index == 1) QuitToMainMenu();
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        if (depthOfField != null) depthOfField.active = false;
        SceneManager.LoadScene("MainMenu2_0");
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