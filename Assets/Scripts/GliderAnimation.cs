using UnityEngine;

public class GliderAnimation : MonoBehaviour
{
    float deployedScale = 100f;
    float growthSpeed = 580f;
    public bool activate;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (activate)
        {
            if (transform.localScale.x < deployedScale)
            {
                transform.localScale += new Vector3(growthSpeed, growthSpeed, growthSpeed) * Time.deltaTime;
            }
            else
            {
                transform.localScale = new Vector3(deployedScale, deployedScale, deployedScale);
            }
        }
        else
        {
            if (transform.localScale.x > 0f)
            {
                transform.localScale -= new Vector3(growthSpeed, growthSpeed, growthSpeed) * Time.deltaTime;
            }
            else
            {
                transform.localScale = Vector3.zero;
            }
        }
    }
}
