using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EndCourseCanva : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    [Header("Navigation (0: Restart, 1: Quitter)")]
    public int index = 0;
    public GameObject[] selectables;

    [Header("Feedback Visuel")]
    public float selectedScale = 1.15f;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    private Vector3[] defaultScales;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip soundNav;
    public AudioClip soundSubmit;


    private bool isVerticalAxisInUse = false;
    private Canvas parentCanvas;

    #region Initialisation et Inputs
    void Awake()
    {
        defaultScales = new Vector3[selectables.Length];
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null) defaultScales[i] = selectables[i].transform.localScale;
        }
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && !parentCanvas.isRootCanvas) parentCanvas = parentCanvas.rootCanvas;
    }

    private void Start()
    {
        UIManager2.instance.canPause = false;
    }

    #endregion

    void Update()
    {
        
        HandleNavigation();

        if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            InteractWithCurrentSelection();
        }
        
    }

    public void Resume()
    {

        SceneManager.LoadScene("SampleScene");
        pauseMenuUI.SetActive(false);

    }

    void Pause()
    {
        KartScriptV2.instance.canDrive = false;
        pauseMenuUI.SetActive(true);
        index = 0;
        UpdateVisuals();
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