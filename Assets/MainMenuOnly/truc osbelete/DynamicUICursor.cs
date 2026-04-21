using UnityEngine;
using UnityEngine.EventSystems;

public class DynamicUICursor : MonoBehaviour
{
    public static DynamicUICursor Instance; // Permet d'y accéder facilement depuis le UIManager

    public RectTransform pointer;
    public Vector3 offset = new Vector3(0f, 10f, 0f);
    public float speed = 15f;

    private bool isUsingGamepad = false;
    private bool isCursorAllowed = false; // Géré par le UIManager !

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Si le UIManager dit qu'on n'est pas dans un panneau, on force la désactivation
        if (!isCursorAllowed)
        {
            pointer.gameObject.SetActive(false);
            return;
        }

        // 1. Détection Souris / Clavier
        if (Input.anyKeyDown || Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f)
        {
            if (!IsAnyJoystickButton()) isUsingGamepad = false;
        }

        // 2. Détection Manette
        if (IsAnyJoystickButton() || IsJoystickMoving())
        {
            isUsingGamepad = true;
        }

        // 3. Affichage et mouvement
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (isUsingGamepad && selected != null)
        {
            pointer.gameObject.SetActive(true);
            MovePointer(selected);
        }
        else
        {
            pointer.gameObject.SetActive(false);
        }
    }

    public void SetCursorActive(bool state)
    {
        isCursorAllowed = state;
    }

    bool IsAnyJoystickButton()
    {
        for (int i = 0; i < 20; i++)
            if (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + i))) return true;
        return false;
    }

    bool IsJoystickMoving()
    {
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.2f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.2f;
    }

    void MovePointer(GameObject target)
    {
        RectTransform targetRect = target.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            Vector3 targetPos = targetRect.position + offset;
            pointer.position = Vector3.Lerp(pointer.position, targetPos, Time.unscaledDeltaTime * speed);
        }
    }
}