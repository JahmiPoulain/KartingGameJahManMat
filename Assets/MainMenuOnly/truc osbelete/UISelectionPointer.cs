using UnityEngine;
using UnityEngine.EventSystems;

public class UISelectionPointer : MonoBehaviour
{
    public RectTransform pointer;
    public Vector3 offset = new Vector3(0f, 10f, 0f);
    public float speed = 15f;

    private bool isUsingGamepad = false;

    void Update()
    {
        // 1. Détection de la Souris / Clavier
        // Si la souris bouge ou qu'on tape au clavier
        if (Input.anyKeyDown || Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f)
        {
            // On vérifie que ce n'est pas un bouton de manette qui déclenche le "anyKeyDown"
            if (!IsAnyJoystickButton())
            {
                isUsingGamepad = false;
            }
        }

        // 2. Détection de la Manette
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

    bool IsAnyJoystickButton()
    {
        // On teste les boutons 0 à 19 de manière générique
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                return true;
        }
        return false;
    }

    bool IsJoystickMoving()
    {
        // On check les axes classiques (souvent 1 et 2 pour le stick gauche)
        // Mais aussi 4 et 5 qui correspondent souvent au D-Pad ou au stick droit
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.2f ||
               Mathf.Abs(Input.GetAxis("Vertical")) > 0.2f;
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