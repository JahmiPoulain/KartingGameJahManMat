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
        if (!isInitialized) Awake();

        rowIndex = 0;
        IsRebinding = false;
        ChargerToutesLesTouches();
        UpdateVisualFeedback();
        UpdatePointerPosition();
    }

    void Update()
    {
        if (IsRebinding) return;

        HandleNavigation();

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
            .Replace(" / ", "/")
            .Trim();
    }

    void UpdateVisualFeedback()
    {
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

        if (rowIndex < actionRows.Length)
        {
            ActionRow selectedRow = actionRows[rowIndex];
            if (selectedRow.rowObject != null)
                selectedRow.rowObject.transform.localScale = selectedRow.defaultScale * selectedScale;

            if (selectedRow.textGamepad) selectedRow.textGamepad.color = selectedColor;
            if (selectedRow.textKeyboard) selectedRow.textKeyboard.color = selectedColor;
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