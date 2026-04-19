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
    public float selectedScale = 1.15f;
    public Color selectedColor = Color.yellow;
    private Color normalColor = Color.white;

    [Header("Audio (Optionnel)")]
    public AudioSource audioSource;
    public AudioClip soundNav;
    public AudioClip soundChange;
    public AudioClip soundSubmit;

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

    private bool isVerticalAxisInUse = false;
    private bool isHorizontalAxisInUse = false;

    public static VisualSettings Instance;

    // --- LA SOLUTION EST ICI ---
    // Un tableau pour stocker la taille par défaut de chaque élément de la liste
    private Vector3[] defaultScales;

    private void Awake()
    {
        Instance = this;

        defaultScales = new Vector3[selectables.Length];

        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null)
            {
                defaultScales[i] = selectables[i].transform.localScale;
            }
        }
    }

    void OnEnable()
    {
        index = 0;
        UpdatePointerPosition();
        UpdateVisualFeedback();
        UpdateUI();
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
                selectables[i].transform.localScale = defaultScales[i] * selectedScale;
                SetColorRecursive(selectables[i], selectedColor);
            }
            else
            {
                selectables[i].transform.localScale = defaultScales[i];
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

    void HandleNavigation()
    {
        float v = Input.GetAxisRaw("Vertical");
        if (v != 0)
        {
            if (!isVerticalAxisInUse)
            {
                int dir = v < -0.3f ? 1 : -1;
                ChangeIndex(dir);
                isVerticalAxisInUse = true;
            }
        }
        else isVerticalAxisInUse = false;
    }

    void HandleModification()
    {
        float h = Input.GetAxisRaw("Horizontal");
        if (h != 0)
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
        var manager = MainMenuUIManager.Instance;

        if (selectedObject == itemResolution)
        {
            int oldRes = manager.currentResIndex;
            manager.currentResIndex = Mathf.Clamp(manager.currentResIndex + dir, 0, manager.resolutions.Length - 1);

            if (manager.currentResIndex != oldRes) changed = true;
        }
        else if (selectedObject == itemFps)
        {
            int oldFps = manager.currentFpsIndex;
            manager.currentFpsIndex = Mathf.Clamp(manager.currentFpsIndex + dir, 0, manager.fpsLabels.Length - 1);

            if (manager.currentFpsIndex != oldFps) changed = true;
        }

        if (changed)
        {
            UpdateUI();
            PlaySfx(soundChange);
            StartCoroutine(PulseEffect(selectables[index].transform));
        }
    }

    IEnumerator PulseEffect(Transform t)
    {
        Vector3 baseScale = defaultScales[index];

        t.localScale = baseScale * (selectedScale + 0.1f);
        yield return new WaitForSeconds(0.05f);
        t.localScale = baseScale * selectedScale;
    }

    void InteractWithCurrentSelection()
    {
        PlaySfx(soundSubmit);
        var manager = MainMenuUIManager.Instance;

        if (selectables[index] == itemFullscreen) manager.isFullscreen = !manager.isFullscreen;

        else if (selectables[index] == itemVsync) manager.isVsync = !manager.isVsync;

        else if (selectables[index] == itemApply) 
        { 
            manager.ApplySettings(true); 
            MainMenuUIManager.Instance.GoBack(); 
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        var manager = MainMenuUIManager.Instance;
        resText.text = manager.resolutions[manager.currentResIndex];
        fpsText.text = manager.fpsLabels[manager.currentFpsIndex];
        fullscreenToggle.color = manager.isFullscreen ? Color.green : Color.gray;
        VsyncToggle.color = manager.isVsync ? Color.green : Color.gray;
    }

    void UpdatePointerPosition()
    {
        if (selectables.Length > 0)
            pointeur.position = selectables[index].transform.position + Offset;
    }
}