using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuPauseScriptIG : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuUI; // Le Panel qui contient tout le menu de pause
    private bool isPaused = false;

    [Header("Navigation (0: Continuer, 1: Quitter)")]
    public Transform pointeur;
    public int index = 0;
    public GameObject[] selectables; // Place tes deux boutons ici dans l'ordre
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

    private bool isVerticalAxisInUse = false;

    void Awake()
    {
        // Initialisation des tailles par défaut
        defaultScales = new Vector3[selectables.Length];
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null) defaultScales[i] = selectables[i].transform.localScale;
        }
    }

    void Update()
    {
        // Touche de pause (Échap ou bouton Start manette)
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

    // --- LOGIQUE DE PAUSE ---
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // Relance le temps
        isPaused = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // Fige le temps
        isPaused = true;

        // Reset l'index au bouton "Continuer" à chaque ouverture
        index = 0;
        UpdateVisuals();
    }

    // --- NAVIGATION ---
    void HandleNavigation()
    {
        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(v) > 0.5f)
        {
            if (!isVerticalAxisInUse)
            {
                // Vers le bas = index++, Vers le haut = index--
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
        // Navigation cyclique entre 0 et 1
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

                // Position du pointeur
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

    // --- ACTIONS ---
    void InteractWithCurrentSelection()
    {
        PlaySfx(soundSubmit);

        if (index == 0) // BOUTON CONTINUER
        {
            Resume();
        }
        else if (index == 1) // BOUTON QUITTER
        {
            QuitToMainMenu();
        }
    }

    void QuitToMainMenu()
    {
        // TRÈS IMPORTANT : Remettre le temps à 1 avant de changer de scène !
        // Sinon ton menu principal sera figé (TimeScale est global)
        Time.timeScale = 1f;

        if (MainMenuUIManager.Instance != null)
        {
            // On utilise ta transition "quali" que j'ai écrite avant
            MainMenuUIManager.Instance.LaunchScene("NomDeTaSceneMenu");
        }
        else
        {
            // Sécurité si tu n'as pas le manager dans la scène
            UnityEngine.SceneManagement.SceneManager.LoadScene("NomDeTaSceneMenu");
        }
    }

    // --- UTILS ---
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