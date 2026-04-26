using UnityEngine;

public class BoosterScript : MonoBehaviour
{
    public float boostForce;
    public float boostTime;
    public bool activateFlight;
    public bool turnsKart;
    public Transform turnDir;
    public GameObject windBox;
    //public bool turnForward;

    private void FixedUpdate()
    {
        if (windBox != null && !KartScriptV2.instance.isFlying) windBox.SetActive(false);
    }
    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.layer == 8)
        {
            KartScriptV2.instance.StartTurbo(boostForce, boostTime);
            if (turnsKart) KartScriptV2.instance.ReorientKart(turnDir.eulerAngles);                 
            if (!activateFlight) return;
            KartScriptV2.instance.StartFlight(35f);
            if (windBox != null) { windBox.SetActive(true); }
            //KartScriptV2.instance.flightSpeed = ;
        }
    }
    
}
