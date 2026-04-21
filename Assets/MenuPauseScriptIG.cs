using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
// Ajout des namespaces pour le Post Processing (URP)
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
    public Volume globalVolume; // Glisse ton GameObject "Global Volume" ici
    private DepthOfField depthOfField;

    private bool isVerticalAxisInUse = false;

    void Awake()
    {
        defaultScales = new Vector3[selectables.Length];
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null) defaultScales[i] = selectables[i].transform.localScale;
        }

        // Récupération de l'effet Depth of Field dans le profil du Global Volume
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out depthOfField);
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

        // Désactiver le flou
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

        // Activer le flou
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
        index = (index + dir + selectables.Length) % selectables.Length;

        if (index != oldIndex)
        {
            UpdateVisuals();
            PlaySfx(soundNav);
        }
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] == null) continue;

            if (i == index)
            {
                selectables[i].transform.localScale = defaultScales[i] * selectedScale;
                SetColorRecursive(selectables[i], selectedColor);

                if (pointeur != null)
                    pointeur.position = selectables[i].transform.position + Offset;
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