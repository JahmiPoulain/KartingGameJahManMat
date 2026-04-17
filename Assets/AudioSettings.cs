using UnityEngine;
public class AudioSettings : MonoBehaviour
{
    public Transform pointeur;
    public int index;
    public GameObject[] selectables;
    public Vector3 Offset;

    private bool isAxisInUse = false;

    void OnEnable()
    {
        index = 0;
        UpdatePointerPosition();
    }

    void Update()
    {
        int inputDirection = 0;
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (verticalInput != 0)
        {
            if (!isAxisInUse)
            {
                if (verticalInput < -0.3f) inputDirection = 1;
                else if (verticalInput > 0.3f) inputDirection = -1;

                isAxisInUse = true;
            }
        }
        else
        {
            isAxisInUse = false;
        }

        if (inputDirection != 0)
        {
            if (MainMenuUIManager.Instance != null && MainMenuUIManager.Instance.invertNavigation)
            {
                inputDirection = -inputDirection;
            }
            ChangeIndex(inputDirection);
        }

        if (Input.GetButtonDown("Submit"))
        {
            InteractWithCurrentSelection();
        }
    }

    void ChangeIndex(int inputDirect)
    {
        index += inputDirect;

        if (index < 0)
        {
            index = selectables.Length - 1;
        }
        else if (index >= selectables.Length)
        {
            index = 0;
        }
        UpdatePointerPosition();
    }

    void UpdatePointerPosition()
    {
        if (selectables.Length > 0 && selectables[index] != null)
        {
            pointeur.position = selectables[index].transform.position + Offset;
        }
    }

    void InteractWithCurrentSelection()
    {
        GameObject selectedObject = selectables[index];
        Debug.Log("J'interagis avec : " + selectedObject.name);

        /* * --- POUR ACTIVER TON BOUTON ---
         * Si tes "selectables" sont des vrais boutons UI d'Unity, décommente la ligne ci-dessous :
         * * selectedObject.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
         */
    }
}