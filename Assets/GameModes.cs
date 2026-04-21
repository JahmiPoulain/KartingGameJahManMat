using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameModes : MonoBehaviour
{
    [Header("Configuration des Modes")]
    public string sceneMode1;
    public string sceneMode2;

    [Header("Map Inversée (Index 0)")]
    public static bool isMapInverted = false;
    public TMP_Text texteEtatMap;
    public string texteActif = "OUI";
    public string texteInactif = "NON";

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

    private bool isVerticalAxisInUse = false;
    private bool isHorizontalAxisInUse = false;

    public static GameModes Instance;

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
        ActualiserAffichageMapInversee();
        UpdatePointerPosition();
        UpdateVisualFeedback();
    }

    void Update()
    {
        if (MainMenuUIManager.Instance != null && MainMenuUIManager.Instance.currentState == MainMenuUIManager.MenuState.Loading)
            return;

        HandleNavigation();
        HandleModification();

        if (Input.GetButtonDown("Submit"))
        {
            InteractWithCurrentSelection();
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

                if (index == 0)
                {
                    ToggleMapInverted();
                    PlaySfx(soundChange);
                }

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

    void InteractWithCurrentSelection()
    {
        PlaySfx(soundSubmit);
      // StartCoroutine(PulseEffect(selectables[index].transform));

        if (index == 2)
        {
            ToggleMapInverted();
        }
        else if (index == 0)
        {
            if (MainMenuUIManager.Instance != null)
                MainMenuUIManager.Instance.LaunchScene(sceneMode1);
        }
        else if (index == 1)
        {
            if (MainMenuUIManager.Instance != null)
                MainMenuUIManager.Instance.LaunchScene(sceneMode2);
        }
    }

    private void ToggleMapInverted()
    {
        isMapInverted = !isMapInverted;
        ActualiserAffichageMapInversee();
    }

    private void ActualiserAffichageMapInversee()
    {
        if (texteEtatMap != null)
        {
            texteEtatMap.text = isMapInverted ? texteActif : texteInactif;
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

        Image img = obj.GetComponent<Image>();
        if (img != null && img.gameObject.name != "Background")
        {
            img.color = c;
        }
    }

    IEnumerator PulseEffect(Transform t)
    {
        if (t == null) yield break;

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