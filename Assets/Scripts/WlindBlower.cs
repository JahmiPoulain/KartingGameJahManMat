using UnityEngine;

public class WlindBlower : MonoBehaviour
{
    public float force;
    [SerializeField]  bool fast;
    
    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.layer == 8)
        {
            KartScriptV2.instance.StartWindBlow(transform.forward,force, fast);
            //if (turnsKart) KartScriptV2.instance.ReorientKart(turnDir.eulerAngles);
            //if (!activateFlight) return;
            //KartScriptV2.instance.StartFlight(35f);
            //KartScriptV2.instance.flightSpeed = ;
        }
    }
}
