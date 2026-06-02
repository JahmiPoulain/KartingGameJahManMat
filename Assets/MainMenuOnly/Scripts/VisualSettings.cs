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

    // --- NOUVEAU : Paramètres pour l'animation ---
    [Header("Animation Curseur (Flottaison)")]
    [Tooltip("Vitesse du mouvement de haut en bas")]
    public float floatingSpeed = 5f;
    [Tooltip("Hauteur du mouvement (en pixels si c'est de l'UI, ou unités monde)")]
    public float floatingAmount = 15f;

    [Header("Feedback Visuel")]
    public float selectedScale = 1.15f; // L'objet grossit de 15%
    public Color selectedColor = Color.yellow;

    // hexadecimal : #6D86A2
    private Color normalColor = new Color(109f / 255f, 134f / 255f, 162f / 255f);

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

    [SerializeField] private Sprite checkboxempty;
    [SerializeField] private Sprite checkboxfull;

    private bool isVerticalAxisInUse = false;
    private bool isHorizontalAxisInUse = false;

    void Start()
    {
        if (MainMenuUIManager.Instance != null)
        {
            UpdateUI();
        }
    }

    void OnEnable()
    {
        index = 0;
        AnimatePointer(true);
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
        AnimatePointer(false);
    }
    void AnimatePointer(bool snapImmediately)
    {
        if (pointeur == null || selectables == null || selectables.Length == 0 || index >= selectables.Length)
            return;

        Vector3 basePosition = selectables[index].transform.position + Offset;

        if (snapImmediately)
        {
            pointeur.position = basePosition;
            return;
        }
        float waveY = Mathf.Sin(Time.time * floatingSpeed) * floatingAmount;

        pointeur.position = new Vector3(basePosition.x, basePosition.y + waveY, basePosition.z);
    }

    void UpdateVisualFeedback()
    {
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] == null) continue;

            if (i == index)
            {
                selectables[i].transform.localScale = Vector3.one * selectedScale;
                SetColorRecursive(selectables[i], selectedColor);
            }
            else
            {
                selectables[i].transform.localScale = Vector3.one;
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
            UpdateVisualFeedback();
            PlaySfx(soundNav);
        }
    }

    IEnumerator PulseEffect(Transform t)
    {
        /*Vector3 m = Vector3.one * selectedScale;
        t.localScale = Vector3.one * (selectedScale + 0.1f);*/
        yield return new WaitForSeconds(0.05f);
       // t.localScale = m;
    }

    void ChangeSettingValue(int dir)
    {
        bool changed = false;
        GameObject selectedObject = selectables[index];
        var manager = MainMenuUIManager.Instance;
        if (manager == null) return;

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
           // StartCoroutine(PulseEffect(selectables[index].transform));
        }
    }

    void InteractWithCurrentSelection()
    {
        PlaySfx(soundSubmit);
        var manager = MainMenuUIManager.Instance;
        if (manager == null) return;

        if (selectables[index] == itemFullscreen) manager.isFullscreen = !manager.isFullscreen;
        else if (selectables[index] == itemVsync) manager.isVsync = !manager.isVsync;
        else if (selectables[index] == itemApply)
        {
            manager.ApplySettings(true);
            manager.GoBack();
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        var m = MainMenuUIManager.Instance;
        if (m == null) return;
        resText.text = m.resolutions[m.currentResIndex];
        fpsText.text = m.fpsLabels[m.currentFpsIndex];
        fullscreenToggle.sprite = m.isFullscreen ? checkboxfull : checkboxempty;
        VsyncToggle.sprite = m.isVsync ? checkboxfull : checkboxempty;
    }
}