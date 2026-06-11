using UnityEngine;

public class InversionCatcher : MonoBehaviour
{
    public static InversionCatcher instance;
    private bool inverted = false;

    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void CatchInversion(bool oui)
    {
        inverted = oui;
    }

}
