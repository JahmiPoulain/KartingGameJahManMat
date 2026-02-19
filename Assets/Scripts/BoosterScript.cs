using UnityEngine;

public class BoosterScript : MonoBehaviour
{
    public float boostForce;
    public float boostTime;
    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.layer == 8)
        {
            KartScriptV2.instance.StartTurbo(boostForce, boostTime);
        }
    }
    
}
