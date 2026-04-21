using UnityEngine;
using UnityEngine.InputSystem;

public class RebindKey : MonoBehaviour
{
    public InputActionReference moveAction;

    void Start()
    {
        // Assurez-vous que l'action est activée
        moveAction.action.Enable();
    }

    public void StartRebinding()
    {
        // Démarre le processus de rebinding
        var rebindingOperation = moveAction.action.PerformInteractiveRebinding()
//            .WithControlsExcluded("<Keyboard>/escape") // Exclut la touche escape pour éviter les conflits
            .OnMatchWaitForAnother(0.5f) // Attend 0.5 seconde pour confirmer le rebinding
            .OnComplete(operation => {
                operation.Dispose();
                Debug.Log("Key rebound to: " + moveAction.action.GetBindingDisplayString());
            })
            .Start();
    }
}
