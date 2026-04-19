using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Audio;

public class AudioSettings : MonoBehaviour
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

    [Header("Audio SFX Menu")]
    public AudioSource audioSource;
    public AudioClip soundNav;
    public AudioClip soundChange;
    public AudioClip soundSubmit;

    [Header("Références UI (Sliders)")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Identification des lignes")]
    public GameObject itemMaster;
    public GameObject itemMusic;
    public GameObject itemSfx;
    public GameObject itemApply;

    [Header("Mixer Audio")]
    public AudioMixer mainAudioMixer;

    public int masterVol = 10;
    public int musicVol = 10;
    public int sfxVol = 10;

    private bool isVerticalAxisInUse = false;
    private bool isHorizontalAxisInUse = false;

    private Vector3[] defaultScales;
    public static AudioSettings Instance;

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

        SetupSliders();
        LoadAudioSettings();
    }

    void OnEnable()
    {
        index = 0;
        LoadAudioSettings();
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

    void SetupSliders()
    {
        if (masterSlider != null) { masterSlider.minValue = 0; masterSlider.maxValue = 10; masterSlider.interactable = false; }
        if (musicSlider != null) { musicSlider.minValue = 0; musicSlider.maxValue = 10; musicSlider.interactable = false; }
        if (sfxSlider != null) { sfxSlider.minValue = 0; sfxSlider.maxValue = 10; sfxSlider.interactable = false; }
    }

    void HandleNavigation()
    {
        float v = Input.GetAxisRaw("Vertical");
        if (v != 0)
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

        if (selectedObject == itemMaster)
        {
            int oldVol = masterVol;
            masterVol = Mathf.Clamp(masterVol + dir, 0, 10);
            if (masterVol != oldVol) changed = true;
        }
        else if (selectedObject == itemMusic)
        {
            int oldVol = musicVol;
            musicVol = Mathf.Clamp(musicVol + dir, 0, 10);
            if (musicVol != oldVol) changed = true;
        }
        else if (selectedObject == itemSfx)
        {
            int oldVol = sfxVol;
            sfxVol = Mathf.Clamp(sfxVol + dir, 0, 10);
            if (sfxVol != oldVol) changed = true;
        }

        if (changed)
        {
            UpdateUI();
            PlaySfx(soundChange);
            //StartCoroutine(PulseEffect(selectables[index].transform));
            ApplyVolumesToMixer();
        }
    }

    void InteractWithCurrentSelection()
    {
        PlaySfx(soundSubmit);

        if (selectables[index] == itemApply)
        {
            SaveAndApplySettings();
            if (MainMenuUIManager.Instance != null)
            {
                MainMenuUIManager.Instance.GoBack();
            }
        }
    }

    void LoadAudioSettings()
    {
        masterVol = PlayerPrefs.GetInt("MasterVol", 10);
        musicVol = PlayerPrefs.GetInt("MusicVol", 10);
        sfxVol = PlayerPrefs.GetInt("SfxVol", 10);
        ApplyVolumesToMixer();
    }

    void SaveAndApplySettings()
    {
        PlayerPrefs.SetInt("MasterVol", masterVol);
        PlayerPrefs.SetInt("MusicVol", musicVol);
        PlayerPrefs.SetInt("SfxVol", sfxVol);
        PlayerPrefs.Save();
    }

    void ApplyVolumesToMixer()
    {
        if (mainAudioMixer != null)
        {
            mainAudioMixer.SetFloat("masterVolume", Mathf.Log10(Mathf.Max(masterVol, 0.0001f) / 10f) * 20f);
            mainAudioMixer.SetFloat("musicVolume", Mathf.Log10(Mathf.Max(musicVol, 0.0001f) / 10f) * 20f);
            mainAudioMixer.SetFloat("sfxVolume", Mathf.Log10(Mathf.Max(sfxVol, 0.0001f) / 10f) * 20f);
        }
        else
        {
            AudioListener.volume = masterVol / 10f;
        }
    }

    void UpdateUI()
    {
        if (masterSlider != null) masterSlider.value = masterVol;
        if (musicSlider != null) musicSlider.value = musicVol;
        if (sfxSlider != null) sfxSlider.value = sfxVol;
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

        Image img = obj.GetComponent<Image>();
        if (img != null && img.gameObject.name != "Background")
        {
            img.color = c;
        }
    }

    IEnumerator PulseEffect(Transform t)
    {
        Vector3 baseScale = defaultScales[index];
        t.localScale = baseScale * (selectedScale + 0.1f);
        yield return new WaitForSeconds(0.05f);
        t.localScale = baseScale * selectedScale;
    }

    void UpdatePointerPosition()
    {
        if (selectables.Length > 0 && selectables[index] != null)
            pointeur.position = selectables[index].transform.position + Offset;
    }

    void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}