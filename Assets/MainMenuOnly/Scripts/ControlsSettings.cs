using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public class ControlsSettings : MonoBehaviour
{
    [System.Serializable]
    public class ActionRow
    {
        public string actionName;
        public InputActionReference actionRef;
        public TMP_Text textKeyboard;
        public TMP_Text textGamepad;
        public TMP_Text label;
        public GameObject rowObject;

        [Header("Configuration du Slider (Optionnel)")]
        public bool isSlider; // Coche cette case dans l'inspecteur pour ton 7ème élément
        public Slider sliderComponent; // Glisse ton composant UI Slider ici
        public TMP_Text sliderValueText; // Text pour afficher la valeur actuelle (ex: "1.25")

        [HideInInspector]
        public Vector3 defaultScale;
    }

    [Header("Configuration des Actions")]
    public ActionRow[] actionRows;
    public GameObject itemApply;

    [Header("Navigation")]
    public Transform pointeur;
    public Vector3 Offset;
    private int rowIndex = 0;

    public static bool IsRebinding = false;

    [Header("Feedback Visuel")]
    public float selectedScale = 1.15f;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip soundNav;
    public AudioClip soundSubmit;

    private bool isVerticalAxisInUse = false;
    private Vector3 applyDefaultScale;
    private bool isInitialized = false;

    public float floatingSpeed = 12f;
    public float floatingAmount = 0.1f;

    void Awake()
    {
        foreach (var row in actionRows)
        {
            if (row.rowObject != null)
                row.defaultScale = row.rowObject.transform.localScale;

            // AJOUT : Permet de gérer les changements si le joueur utilise la souris sur le slider
            if (row.isSlider && row.sliderComponent != null)
            {
                row.sliderComponent.minValue = 0.5f;
                row.sliderComponent.maxValue = 2.0f;
                row.sliderComponent.onValueChanged.AddListener((val) => {
                    AppliquerSensibilite(row, val);
                    SauvegarderLesTouches(row.actionRef);
                });
            }
        }
        if (itemApply != null)
            applyDefaultScale = itemApply.transform.localScale;

        isInitialized = true;
    }

    void OnEnable()
    {
        if (!isInitialized) Awake();

        rowIndex = 0;
        IsRebinding = false;
        ChargerToutesLesTouches();
        UpdateVisualFeedback();
        UpdatePointerPosition();
        AnimatePointer(true);
    }

    void Update()
    {
        if (IsRebinding) return;

        HandleNavigation();
        HandleSliderInput(); // AJOUT : Gère les pressions Gauche/Droite pour le slider

        if (Input.GetButtonDown("Submit"))
        {
            InteractWithCurrentSelection();
        }
        AnimatePointer(false);
    }

    void HandleNavigation()
    {
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(v) > 0.5f)
        {
            if (!isVerticalAxisInUse)
            {
                int dir = v < 0f ? 1 : -1;
                if (MainMenuUIManager.Instance != null && MainMenuUIManager.Instance.invertNavigation) dir = -dir;

                ChangeRow(dir);
                isVerticalAxisInUse = true;
            }
        }
        else isVerticalAxisInUse = false;
    }

    // AJOUT : Permet de modifier le slider avec les flèches directionnelles ou le stick gauche
    void HandleSliderInput()
    {
        if (rowIndex >= actionRows.Length) return;

        ActionRow currentRow = actionRows[rowIndex];
        if (currentRow.isSlider && currentRow.sliderComponent != null)
        {
            float h = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(h) > 0.5f)
            {
                // Ajuste la vitesse de défilement du slider ici (ici 1.0f par seconde)
                float direction = h > 0f ? 1f : -1f;
                currentRow.sliderComponent.value += direction * Time.unscaledDeltaTime * 1.0f;
            }
        }
    }

    void ChangeRow(int dir)
    {
        int oldIndex = rowIndex;
        rowIndex = Mathf.Clamp(rowIndex + dir, 0, actionRows.Length);

        if (rowIndex != oldIndex)
        {
            UpdatePointerPosition();
            UpdateVisualFeedback();
            PlaySfx(soundNav);
        }
    }

    void InteractWithCurrentSelection()
    {
        PlaySfx(soundSubmit);

        if (rowIndex == actionRows.Length)
        {
            if (MainMenuUIManager.Instance != null) MainMenuUIManager.Instance.GoBack();
            return;
        }

        // AJOUT : Si on clique sur un slider, on ne veut pas lancer un Rebinding classique
        if (actionRows[rowIndex].isSlider) return;

        bool isKeyboardSubmit = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
        bool isGamepad = !isKeyboardSubmit;

        StartCoroutine(LancerLeChangementDiffere(actionRows[rowIndex], isGamepad));
    }

    private IEnumerator LancerLeChangementDiffere(ActionRow row, bool pourManette)
    {
        if (row.actionRef == null) yield break;

        IsRebinding = true;

        TMP_Text texteCible = pourManette ? row.textGamepad : row.textKeyboard;
        texteCible.text = "...";

        yield return new WaitForSecondsRealtime(0.2f);

        row.actionRef.action.Disable();

        int bindingIndexToModify = ObtenirIndexDeBinding(row.actionRef.action, pourManette);

        var operation = row.actionRef.action.PerformInteractiveRebinding(bindingIndexToModify);

        operation.WithCancelingThrough("<Keyboard>/escape");

        if (pourManette)
        {
            operation.WithControlsExcluding("<Keyboard>").WithControlsExcluding("<Mouse>");
        }
        else
        {
            operation.WithControlsExcluding("<Gamepad>");
        }

        operation.OnComplete(op => {
            TerminerRebinding(row, op);
        });

        operation.OnCancel(op => {
            TerminerRebinding(row, op);
        });

        operation.Start();
    }

    private void TerminerRebinding(ActionRow row, InputActionRebindingExtensions.RebindingOperation op)
    {
        ActualiserAffichageAction(row);
        SauvegarderLesTouches(row.actionRef);
        row.actionRef.action.Enable();
        op.Dispose();
        StartCoroutine(UnlockNavigation());
    }

    private int ObtenirIndexDeBinding(InputAction action, bool pourManette)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].isComposite) continue;

            string path = action.bindings[i].path.ToLower();
            if (pourManette && (path.Contains("<gamepad>") || path.Contains("<joystick>"))) return i;
            if (!pourManette && (path.Contains("<keyboard>") || path.Contains("<mouse>"))) return i;
        }
        return pourManette ? 1 : 0;
    }

    IEnumerator UnlockNavigation()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        IsRebinding = false;
    }

    private void SauvegarderLesTouches(InputActionReference actionRef)
    {
        if (actionRef == null) return;
        var asset = actionRef.action.actionMap.asset;
        string donnees = asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(MainMenuUIManager.Instance.controlsSaveKey, donnees);
        PlayerPrefs.Save();
    }

    private void ChargerToutesLesTouches()
    {
        string donneesSauvegardees = PlayerPrefs.GetString(MainMenuUIManager.Instance.controlsSaveKey);

        foreach (var row in actionRows)
        {
            if (!string.IsNullOrEmpty(donneesSauvegardees) && row.actionRef != null)
            {
                row.actionRef.action.actionMap.asset.LoadBindingOverridesFromJson(donneesSauvegardees);
            }

            // AJOUT : Si c'est un slider, on récupère sa valeur et on l'applique au processeur
            if (row.isSlider)
            {
                float sensiSauvegardee = PlayerPrefs.GetFloat("StickSensitivity_" + row.actionRef.action.name, 1.0f);
                if (row.sliderComponent != null)
                {
                    row.sliderComponent.value = sensiSauvegardee;
                }
                AppliquerSensibilite(row, sensiSauvegardee);
            }
            else
            {
                ActualiserAffichageAction(row);
            }
        }
    }

    // AJOUT : Calcule et applique le processeur "scale" sur l'action ciblée
    private void AppliquerSensibilite(ActionRow row, float valeur)
    {
        if (row.actionRef == null) return;

        int indexManette = ObtenirIndexDeBinding(row.actionRef.action, true);
        if (indexManette == -1) return;

        // On récupère le chemin actuel (modifié ou par défaut) pour ne pas écraser une touche rebondie
        string pathActuel = row.actionRef.action.bindings[indexManette].overridePath;
        if (string.IsNullOrEmpty(pathActuel))
            pathActuel = row.actionRef.action.bindings[indexManette].path;

        // IMPORTANT : Utilisation de InvariantCulture pour forcer le '.' au lieu de la ',' (sinon le input system bug en français)
        string valeurFormatee = valeur.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        row.actionRef.action.ApplyBindingOverride(indexManette, new InputBinding
        {
            path = pathActuel,
            overrideProcessors = $"scale(factor={valeurFormatee})"
        });

        if (row.sliderValueText != null)
            row.sliderValueText.text = valeur.ToString("F2");

        // Sauvegarde de secours de la valeur brute pour l'initialisation du Slider UI au démarrage
        PlayerPrefs.SetFloat("StickSensitivity_" + row.actionRef.action.name, valeur);
    }

    private void ActualiserAffichageAction(ActionRow row)
    {
        if (row.actionRef != null && !row.isSlider)
        {
            int indexClavier = ObtenirIndexDeBinding(row.actionRef.action, false);
            int indexManette = ObtenirIndexDeBinding(row.actionRef.action, true);

            if (row.textKeyboard != null)
                row.textKeyboard.text = NettoyerNomTouche(row.actionRef.action.GetBindingDisplayString(indexClavier));

            if (row.textGamepad != null)
                row.textGamepad.text = NettoyerNomTouche(row.actionRef.action.GetBindingDisplayString(indexManette));
        }
    }

    private string NettoyerNomTouche(string nomBrut)
    {
        if (string.IsNullOrEmpty(nomBrut)) return "";

        return nomBrut
            .Replace("Right Stick", "R Stick")
            .Replace("Left Stick", "L Stick")
            .Replace("D-Pad", "DPad")
            .Replace("Press", "")
            .Replace("Left Button", "LB")
            .Replace("Right Button", "RB")
            .Replace("Left Trigger", "LT")
            .Replace("Right Trigger", "RT")
            .Replace("/", " ")
            .Trim();
    }

    void AnimatePointer(bool snapImmediately)
    {
        if (pointeur == null || actionRows == null || actionRows.Length == 0 || rowIndex >= actionRows.Length)
            return;

        if (actionRows[rowIndex].label == null) return;

        Vector3 basePosition = actionRows[rowIndex].label.transform.position + Offset;

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
        foreach (var row in actionRows)
        {
            if (row.rowObject != null)
                row.rowObject.transform.localScale = row.defaultScale;

            if (row.label) row.label.color = normalColor;
            if (row.sliderValueText) row.sliderValueText.color = normalColor;
        }

        if (itemApply != null)
        {
            itemApply.transform.localScale = applyDefaultScale;
            SetColorRecursive(itemApply, normalColor);
        }

        if (rowIndex < actionRows.Length)
        {
            ActionRow selectedRow = actionRows[rowIndex];
            if (selectedRow.rowObject != null)
                selectedRow.rowObject.transform.localScale = selectedRow.defaultScale * selectedScale;

            if (selectedRow.label) selectedRow.label.color = selectedColor;
            if (selectedRow.sliderValueText) selectedRow.sliderValueText.color = selectedColor;
        }
        else
        {
            if (itemApply != null)
            {
                itemApply.transform.localScale = applyDefaultScale * selectedScale;
                SetColorRecursive(itemApply, selectedColor);
            }
        }
    }

    void UpdatePointerPosition()
    {
        if (rowIndex < actionRows.Length)
        {
            ActionRow selectedRow = actionRows[rowIndex];
            if (selectedRow.rowObject != null)
                pointeur.position = selectedRow.rowObject.transform.position + Offset;
        }
        else if (itemApply != null)
        {
            pointeur.position = itemApply.transform.position + Offset;
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