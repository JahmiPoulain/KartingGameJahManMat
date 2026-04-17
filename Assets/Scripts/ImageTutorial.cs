using UnityEngine;

public class ImageTutorial : MonoBehaviour
{
    void Update()
    {
        if (KartScriptV2.instance.activeRespawnPoints.Count < KartScriptV2.instance.respawnPointsArr.Length - 1) gameObject.SetActive(false);
    }
}
