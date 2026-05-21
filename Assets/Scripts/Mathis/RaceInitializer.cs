using UnityEngine;

public class RaceInitializer : MonoBehaviour
{
    public MonoBehaviour timeAttackScript;
    public MonoBehaviour contreLaMontreScript;

    void Start()
    {
        // On éteint tout par sécurité au début
        timeAttackScript.enabled = false;
        contreLaMontreScript.enabled = false;

        // On active le bon selon le GameManager
        if (GameManager.Instance().currentMode == GameManager.GameModeType.TimeAttack)
            timeAttackScript.enabled = true;
        else
            contreLaMontreScript.enabled = true;
    }
}