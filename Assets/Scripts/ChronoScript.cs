using UnityEngine;

public class ChronoScript : MonoBehaviour
{
    [SerializeField] private ContreLaMontre contreLaMontre;

    private float delta = 0f;

    public float CurrentTime => delta;

    void Update()
    {
        if (contreLaMontre.RaceFinished)
            return;

        delta += Time.deltaTime;
    }

    public void ResetChrono()
    {
        delta = 0f;
    }
}