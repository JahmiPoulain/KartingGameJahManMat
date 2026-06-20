using UnityEngine;

public class ActiveProfileBridge : MonoBehaviour
{
    public static ActiveProfileBridge Instance { get; private set; }
    public string CurrentProfileId { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public void SetProfile(string profileId)
    {
        CurrentProfileId = profileId;
    }

    public static string Key(string baseKey)
    {
        string id = Instance?.CurrentProfileId ?? "default";
        return $"{id}_{baseKey}";
    }
}