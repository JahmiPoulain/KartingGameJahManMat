using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VisualSettings : MonoBehaviour
{
    [Header("Navigation")]
    public Transform pointeur;
    public int index;
    public GameObject[] selectables;
    public Vector3 Offset;

    [Header("Feedback Visuel")]
    public float selectedScale = 1.15f; // L'objet grossit de 15%
    public Color selectedColor = Color.yellow;
    private Color normalColor = Color.white;

    [Header("Audio (Optionnel)")]
    public AudioSource audioSource;
    public AudioClip soundNav;    // Son quand on change de ligne
    public AudioClip soundChange; // Son quand on change gauche/droite
    public AudioClip soundSubmit; // Son quand on valide

    [Header("Références UI")]
    public TMP_Text resText;
    public TMP_Text fpsText;
    public Image fullscreenToggle;
    public Image VsyncToggle;

    [Header("Identification")]
    public GameObject itemResolution;
    public GameObject itemFps;
    public GameObject itemFullscreen;
    public GameObject itemVsync;
    public GameObject itemApply;

    public string[] resolutions = { "1920x1080", "1600x900", "1280x720", "800x600" };
    private int currentResIndex = 0;
    public int[] fpsValues = { 30, 60, 120, -1 };
    public string[] fpsLabels = { "30 FPS", "60 FPS", "120 FPS", "Illimité" };
    private int currentFpsIndex = 1;
    private bool isFullscreen = true;
    private bool isVsync = false;

    private bool isVerticalAxisInUse = false;
    private bool isHorizontalAxisInUse = false;

    void Start()
    {
        LoadSettings();
        ApplySettings(false);
    }

    void OnEnable()
    {
        index = 0;
        UpdatePointerPosition();
        UpdateVisualFeedback();
    }

    void Update()
    {
        HandleNavigation();
        HandleModification();

        if (Input.GetButtonDown("Submit"))
        {
            InteractWithCurrentSelection();
        }
    }


    void UpdateVisualFeedback()
    {
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] == null) continue;

            if (i == index)
            {
                selectables[i].transform.localScale = selectables[i].transform.localScale * selectedScale;
                SetColorRecursive(selectables[i], selectedColor);
            }
            else
            {
                selectables[i].transform.localScale = selectables[i].transform.localScale;
                SetColorRecursive(selectables[i], normalColor);
            }
        }
    }
    void SetColorRecursive(GameObject obj, Color c)
    {
        if (obj.GetComponent<TMP_Text>()) obj.GetComponent<TMP_Text>().color = c;
        if (obj.GetComponent<Image>() && obj != fullscreenToggle.gameObject && obj != VsyncToggle.gameObject)
            obj.GetComponent<Image>().color = c;
    }

    void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }



    void SaveSettings()
    {
        PlayerPrefs.SetInt("ResIndex", currentResIndex);
        PlayerPrefs.SetInt("FpsIndex", currentFpsIndex);
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("Vsync", isVsync ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log("Paramètres sauvegardés !");
    }

    void LoadSettings()
    {
        currentResIndex = PlayerPrefs.GetInt("ResIndex", 0);
        currentFpsIndex = PlayerPrefs.GetInt("FpsIndex", 1);
        isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        isVsync = PlayerPrefs.GetInt("Vsync", 0) == 1;

        UpdateUI();
    }

    public void ApplySettings(bool shouldSave)
    {
        string[] resParts = resolutions[currentResIndex].Split('x');
        int width = int.Parse(resParts[0]);
        int height = int.Parse(resParts[1]);
        Screen.SetResolution(width, height, isFullscreen);

        QualitySettings.vSyncCount = isVsync ? 1 : 0;

        Application.targetFrameRate = fpsValues[currentFpsIndex];

        if (shouldSave) SaveSettings();

        Debug.Log("Paramètres Appliqués !");
    }


    void HandleNavigation()
    {
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(v) > 0.5f)
        {
            if (!isVerticalAxisInUse)
            {
                int dir = v < -0.3f ? 1 : -1;
                if (MainMenuUIManager.Instance != null && MainMenuUIManager.Instance.invertNavigation) dir = -dir;

                ChangeIndex(dir);
                isVerticalAxisInUse = true;
            }
        }
        else isVerticalAxisInUse = false;
    }

    void HandleModification()
    {
        float h = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(h) > 0.5f) 
        {
            if (!isHorizontalAxisInUse)
            {
                int dir = h > 0.3f ? 1 : -1;
                ChangeSettingValue(dir);
                isHorizontalAxisInUse = true;
            }
        }
        else isHorizontalAxisInUse = false;
    }

    void ChangeIndex(int dir)
    {
        int oldIndex = index;

        index = Mathf.Clamp(index + dir, 0, selectables.Length - 1);

        if (index != oldIndex)
        {
            UpdatePointerPosition();
            UpdateVisualFeedback();
            PlaySfx(soundNav);
        }
    }

    void ChangeSettingValue(int dir)
    {
        bool changed = false;
        GameObject selectedObject = selectables[index];

        if (selectedObject == itemResolution)
        {
            int oldRes = currentResIndex;
            currentResIndex = Mathf.Clamp(currentResIndex + dir, 0, resolutions.Length - 1);

            if (currentResIndex != oldRes) changed = true;
        }


        else if (selectedObject == itemFps)
        {
            int oldFps = currentFpsIndex;
            currentFpsIndex = Mathf.Clamp(currentFpsIndex + dir, 0, fpsLabels.Length - 1);

            if (currentFpsIndex != oldFps) changed = true;
        }

        if (changed)
        {
            UpdateUI();
            PlaySfx(soundChange);
           // StartCoroutine(PulseEffect(selectables[index].transform));
        }
    }

    IEnumerator PulseEffect(Transform t)
    {
        Vector3 m = t.localScale;
        t.localScale = m * (selectedScale + 0.1f);
        yield return new WaitForSeconds(0.05f);
        t.localScale = m * selectedScale;
    }

    void InteractWithCurrentSelection()
    {
        PlaySfx(soundSubmit);
        if (selectables[index] == itemFullscreen) isFullscreen = !isFullscreen;
        else if (selectables[index] == itemVsync) isVsync = !isVsync;
        else if (selectables[index] == itemApply) { ApplySettings(true); MainMenuUIManager.Instance.GoBack(); }

        UpdateUI();
    }

    void UpdateUI()
    {
        resText.text = resolutions[currentResIndex];
        fpsText.text = fpsLabels[currentFpsIndex];
        fullscreenToggle.color = isFullscreen ? Color.green : Color.gray;
        VsyncToggle.color = isVsync ? Color.green : Color.gray;
    }

    void UpdatePointerPosition()
    {
        if (selectables.Length > 0)
            pointeur.position = selectables[index].transform.position + Offset;
    }
}