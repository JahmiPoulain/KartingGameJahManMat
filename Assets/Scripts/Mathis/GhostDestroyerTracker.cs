using UnityEngine;

public class GhostDestroyTracker : MonoBehaviour
{
    private void OnDestroy()
    {
        Debug.LogError("[Ghost] Destroyed! Stack trace:");
        Debug.LogError(System.Environment.StackTrace);
    }

    private void OnDisable()
    {
        Debug.LogWarning("[Ghost] Disabled!");
        Debug.LogWarning(System.Environment.StackTrace);
    }
}