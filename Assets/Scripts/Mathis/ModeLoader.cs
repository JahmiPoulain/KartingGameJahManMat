using UnityEngine;

public class ModeLoader : MonoBehaviour
{
    [SerializeField] private GameObject normalTrack;
    [SerializeField] private GameObject reverseTrack;
    void Start()
    {
        LapManager lm = FindFirstObjectByType<LapManager>();
        GameObject player = FindFirstObjectByType<KartScriptV2>().gameObject;

        // On crée le mode et on le récupère
        GameMode newMode = GameManager.Instance().SetupGameMode(player, lm);

        if(MainMenuUIManager.Instance.isMapInverted)
        {
            normalTrack.SetActive(false);
            reverseTrack.SetActive(true);
        }
        else
        {
            normalTrack.SetActive(true);
            reverseTrack.SetActive(false);
        }

        // On force le LapManager à utiliser CE mode précis immédiatement
        lm.SetGameMode(newMode);
    }
}
