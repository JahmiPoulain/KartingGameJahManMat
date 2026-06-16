using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement; // REQUIS pour recharger la scène

public class MenuPauseScriptIG : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    // CONFIGURATION : Ajuste la taille dans l'inspecteur à 3 (0: Continuer, 1: Recommencer, 2: Quitter)
    [Header("Navigation (0: Continuer, 1: Recommencer, 2: Quitter)")]
    public Transform pointeur;
    public int index = 0;
    public GameObject[] selectables;
    public Vector3 Offset;

    [Header("Feedback Visuel")]
    public float selectedScale = 1.15f;
    public Color selectedColor = Color.yellow;
    private Color normalColor = Color.white;
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

    void Awake()
    {
        defaultScales = new Vector3[selectables.Length];
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null) defaultScales[i] = selectables[i].transform.localScale;
        }

        if (globalVolume != null && globalVolume.profile.TryGet<DepthOfField>(out var dof))
        {
            depthOfField = dof;
        }

        parentCanvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel"))
        {
            if (isPaused) Resume();
            else Pause();
        }

        if (isPaused)
        {
            HandleNavigation();
        }
    }

    void HandleNavigation()
    {
        float v = Input.GetAxisRaw("Vertical");

        if (v != 0)
        {
            if (!isVerticalAxisInUse)
            {
                if (v < 0) index = (index + 1) % selectables.Length;
                else if (v > 0) index = (index - 1 + selectables.Length) % selectables.Length;

                PlaySfx(soundNav);
                UpdateVisuals();
                isVerticalAxisInUse = true;
            }
        }
        else
        {
            isVerticalAxisInUse = false;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit"))
        {
            InteractWithCurrentSelection();
        }
    }

    void UpdateVisuals()
    {
        float currentScaleFactor = 1f;
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            currentScaleFactor = parentCanvas.transform.localScale.x;
        }

        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] == null) continue;

            if (i == index)
            {
                selectables[i].transform.localScale = defaultScales[i] * selectedScale;
                SetColorRecursive(selectables[i].transform, selectedColor);

                if (pointeur != null)
                {
                    pointeur.position = selectables[i].transform.position + (Offset * currentScaleFactor);
                }
            }
            else
            {
                selectables[i].transform.localScale = defaultScales[i];
                SetColorRecursive(selectables[i].transform, normalColor);
            }
        }
    }

    void InteractWithCurrentSelection()
    {
        PlaySfx(soundSubmit);
        Debug.Log("Selection validée : Index " + index);

        if (index == 0) Resume();
        else if (index == 1) Restart(); // Nouvelle option !
        else if (index == 2) QuitToMainMenu();
    }

    public void Resume()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        if (depthOfField != null) depthOfField.active = false;
    }

    void Pause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        index = 0;
        UpdateVisuals();
        if (depthOfField != null) depthOfField.active = true;
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

    void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        if (depthOfField != null) depthOfField.active = false;

        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.ReturnToMainMenu();
        else
            SceneManager.LoadScene("MainMenu2_0");
    }

    void SetColorRecursive(Transform parent, Color c)
    {
        TMP_Text text = parent.GetComponent<TMP_Text>();
        if (text != null) text.color = c;

        Image img = parent.GetComponent<Image>();
        if (img != null && img.gameObject.name != "Background") img.color = c;

        foreach (Transform child in parent)
        {
            SetColorRecursive(child, c);
        }
    }

    void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}