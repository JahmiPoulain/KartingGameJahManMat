using UnityEngine;

public class RespawnPointScript : MonoBehaviour
{

    void Start()
    {
        KartScriptV2.instance.respawnPoints.Add(transform);
    }

}
