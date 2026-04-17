using UnityEngine;

public class TimedUI : MonoBehaviour
{
    protected float timer;
    [SerializeField] protected float time;
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer < 0) gameObject.SetActive(false);
    }
}
