using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MenuPauseScriptIG : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    [Header("Navigation (0: Continuer, 1: Quitter)")]
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

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out depthOfField);
        }

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && !parentCanvas.isRootCanvas)
        {
            parentCanvas = parentCanvas.rootCanvas;
        }
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

            if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return))
            {
                InteractWithCurrentSelection();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        if (depthOfField != null)
        {
            depthOfField.active = false;
        }
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        index = 0;
        UpdateVisuals();

        if (depthOfField != null)
        {
            depthOfField.active = true;
        }
    }

    void HandleNavigation()
    {
        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(v) > 0.5f)
        {
            if (!isVerticalAxisInUse)
            {
                int dir = v < -0.5f ? 1 : -1;
                ChangeIndex(dir);
                isVerticalAxisInUse = true;
            }
        }
        else
        {
            isVerticalAxisInUse = false;
        }
    }

    void ChangeIndex(int dir)
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
                SetColorRecursive(selectables[i], selectedColor);

                if (pointeur != null)
                {
                    pointeur.position = selectables[i].transform.position + (Offset * currentScaleFactor);
                }
            }
            else
            {
                selectables[i].transform.localScale = defaultScales[i];
                SetColorRecursive(selectables[i], normalColor);
            }
        }
    }

    void InteractWithCurrentSelection()
    {
        PlaySfx(soundSubmit);

        if (index == 0)
        {
            Resume();
        }
        else if (index == 1)
        {
            QuitToMainMenu();
        }
    }

    void QuitToMainMenu()
    {
        Time.timeScale = 1f;

        if (depthOfField != null) depthOfField.active = false;

        if (MainMenuUIManager.Instance != null)
        {
            MainMenuUIManager.Instance.LaunchScene("MainMenu2_0");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu2_0");
        }
    }

    void SetColorRecursive(GameObject obj, Color c)
    {
        if (obj.GetComponent<TMP_Text>()) obj.GetComponent<TMP_Text>().color = c;
        Image img = obj.GetComponent<Image>();
        if (img != null && img.gameObject.name != "Background") img.color = c;
    }

    void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}