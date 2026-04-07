using UnityEngine;

public class BoosterScript : MonoBehaviour
{
    public float boostForce;
    public float boostTime;
    public bool activateFlight;
    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.layer == 8)
        {
            KartScriptV2.instance.StartTurbo(boostForce, boostTime);
            if (!activateFlight) return;
            KartScriptV2.instance.StartFlight(30f);
            //KartScriptV2.instance.flightSpeed = ;
        }
    }
    
}
