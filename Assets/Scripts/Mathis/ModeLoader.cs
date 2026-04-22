using UnityEngine;

public class ModeLoader : MonoBehaviour
{
    void Start() { GameManager.Instance().SetupGameMode(FindFirstObjectByType<KartScriptV2>().gameObject, FindFirstObjectByType<LapManager>()); }
}
