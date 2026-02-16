using UnityEngine;
using UnityEngine.UI;
public class RainbowColorChangeImageUiRigoloScript : MonoBehaviour
{
    public float speed = 1f;
    private Image img;
    private float hue = 0f;

    void Start()
    {
        img = GetComponent<Image>();
    }

    void Update()
    {
        hue += Time.deltaTime * speed;
        if (hue > 1f) hue = 0f;

        img.color = Color.HSVToRGB(hue, 1f, 1f);
    }
}
