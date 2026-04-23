using UnityEngine;

public class ModeLoader : MonoBehaviour
{
    void Start()
    {
        LapManager lm = FindFirstObjectByType<LapManager>();
        GameObject player = FindFirstObjectByType<KartScriptV2>().gameObject;

        // On crée le mode et on le récupère
        GameMode newMode = GameManager.Instance().SetupGameMode(player, lm);

        // On force le LapManager à utiliser CE mode précis immédiatement
        lm.SetGameMode(newMode);
    }
}
