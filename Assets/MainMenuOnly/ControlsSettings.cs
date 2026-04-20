using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class ControlsSettings : MonoBehaviour
{
    [System.Serializable]
    public class ActionRow
    {
        public string actionName;
        public InputActionReference actionRef;
        public TMP_Text textKeyboard;
        public TMP_Text textGamepad;
        public GameObject rowObject;

        [HideInInspector]
        public Vector3 defaultScale; // Mémorise la vraie taille initiale !
    }

    [Header("Configuration des Actions")]
    public ActionRow[] actionRows;
    public GameObject itemApply;

    [Header("Navigation")]
    public Transform pointeur;
    public Vector3 Offset;
    private int rowIndex = 0;
    private bool isRebinding = false;

    [Header("Feedback Visuel")]
    public float selectedScale = 1.15f;
    public Color selectedColor = Color.yellow;
    private Color normalColor = Color.white;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip soundNav;
    public AudioClip soundSubmit;

    private const string NomFichierSauvegarde = "MesTouchesCustom";

    private bool isVerticalAxisInUse = false;
    private Vector3 applyDefaultScale;
    private bool isInitialized = false;

    void Awake()
    {
        // 1. SAUVEGARDE DES TAILLES INITIALES (FINI LE VECTOR3.ONE !)
        foreach (var row in actionRows)
        {
            if (row.rowObject != null)
                row.defaultScale = row.rowObject.transform.localScale;
        }
        if (itemApply != null)
            applyDefaultScale = itemApply.transform.localScale;

        isInitialized = true;
    }

    void OnEnable()
    {
        // Sécurité pour éviter les bugs d'initialisation si OnEnable se lance trop tôt
        if (!isInitialized) Awake();

        rowIndex = 0;
        isRebinding = false;
        ChargerToutesLesTouches();
        UpdateVisualFeedback();
        UpdatePointerPosition();
    }

    void Update()
    {
        if (isRebinding) return;

        HandleNavigation();

        if (Input.GetButtonDown("Submit"))
        {
            InteractWithCurrentSelection();
        }
    }

    void HandleNavigation()
    {
        // NAVIGATION UNIQUEMENT HAUT/BAS (beaucoup plus propre)
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

        // 2. DÉTECTION INTELLIGENTE DU PÉRIPHÉRIQUE
        // Si on a validé en appuyant sur Entrée ou Espace, c'est le clavier !
        bool isKeyboardSubmit = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);

        // Sinon, c'est qu'on a cliqué avec la manette (Croix, A, etc.)
        bool isGamepad = !isKeyboardSubmit;

        LancerLeChangement(actionRows[rowIndex], isGamepad);
    }

    private void LancerLeChangement(ActionRow row, bool pourManette)
    {
        if (row.actionRef == null) return;

        isRebinding = true;

        TMP_Text texteCible = pourManette ? row.textGamepad : row.textKeyboard;
        texteCible.text = "...";

        row.actionRef.action.Disable();

        // 3. ON CIBLE DIRECTEMENT LE BON INDEX DE BINDING DANS L'INPUT SYSTEM
        int bindingIndexToModify = ObtenirIndexDeBinding(row.actionRef.action, pourManette);

        var operation = row.actionRef.action.PerformInteractiveRebinding(bindingIndexToModify);

        // 4. ANNULATION AVEC ÉCHAP (ANNULE TOUT)
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
            // Si on annule, on remet tout à la normale sans sauvegarder
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
        // Petite sécurité pour trouver automatiquement quel emplacement modifier dans l'Input System
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (pourManette && action.bindings[i].path.Contains("<Gamepad>")) return i;
            if (!pourManette && (action.bindings[i].path.Contains("<Keyboard>") || action.bindings[i].path.Contains("<Mouse>"))) return i;
        }
        // Par défaut s'il trouve pas le nom du path
        return pourManette ? 1 : 0;
    }

    IEnumerator UnlockNavigation()
    {
        yield return null;
        isRebinding = false;
    }

    private void SauvegarderLesTouches(InputActionReference actionRef)
    {
        var asset = actionRef.action.actionMap.asset;
        string donnees = asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(NomFichierSauvegarde, donnees);
        PlayerPrefs.Save();
    }

    private void ChargerToutesLesTouches()
    {
        string donneesSauvegardees = PlayerPrefs.GetString(NomFichierSauvegarde);

        foreach (var row in actionRows)
        {
            if (!string.IsNullOrEmpty(donneesSauvegardees) && row.actionRef != null)
            {
                row.actionRef.action.actionMap.asset.LoadBindingOverridesFromJson(donneesSauvegardees);
            }
            ActualiserAffichageAction(row);
        }
    }

    private void ActualiserAffichageAction(ActionRow row)
    {
        if (row.actionRef != null)
        {
            int indexClavier = ObtenirIndexDeBinding(row.actionRef.action, false);
            int indexManette = ObtenirIndexDeBinding(row.actionRef.action, true);

            if (row.textKeyboard != null) row.textKeyboard.text = row.actionRef.action.GetBindingDisplayString(indexClavier);
            if (row.textGamepad != null) row.textGamepad.text = row.actionRef.action.GetBindingDisplayString(indexManette);
        }
    }

    void UpdateVisualFeedback()
    {
        // Reset de tout le monde avec leur VRAIE taille (defaultScale)
        foreach (var row in actionRows)
        {
            if (row.rowObject != null)
                row.rowObject.transform.localScale = row.defaultScale;

            if (row.textKeyboard) row.textKeyboard.color = normalColor;
            if (row.textGamepad) row.textGamepad.color = normalColor;
        }

        if (itemApply != null)
        {
            itemApply.transform.localScale = applyDefaultScale;
            SetColorRecursive(itemApply, normalColor);
        }

        // Highlight de la ligne entière (on change la couleur des deux textes)
        if (rowIndex < actionRows.Length)
        {
            ActionRow selectedRow = actionRows[rowIndex];
            if (selectedRow.rowObject != null)
                selectedRow.rowObject.transform.localScale = selectedRow.defaultScale * selectedScale;

            if (selectedRow.textGamepad) selectedRow.textGamepad.color = selectedColor;
            if (selectedRow.textKeyboard) selectedRow.textKeyboard.color = selectedColor;
        }
        else // Bouton Apply
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
        // Le pointeur se place par rapport au parent de la ligne !
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