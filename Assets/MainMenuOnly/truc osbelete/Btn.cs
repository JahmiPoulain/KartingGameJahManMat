using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Btn : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    public RectTransform effectedObject;
    public Graphic image;

    public bool scaledOnHover = true, rotateOnHover, scaledOnClick = true;

    public Vector3 valueScaleOnHover = new Vector3(1.1f, 1.1f, 1.1f);
    public Vector3 valueRotateOnHover = new Vector3(0, 0, 5);
    public Vector3 ValueScaleOnClick = new Vector3(.9f, .9f, .9f);

    public Color colorOnHover = Color.white, colorOnClick = Color.gray;
    public float speed = 15;

    public ParticleSystem particuleSystem;
    public AudioSource audioSource;
    public AudioClip audioOnHover, audioOnClick;

    Vector3 defaultScale;
    Quaternion defaultRotation;
    Color defaultColor;

    bool isHovering, Clicked;

    void Awake()
    {
        if (!effectedObject) effectedObject = GetComponent<RectTransform>();
        if (!image) image = GetComponent<Graphic>();

        defaultScale = effectedObject.localScale;
        defaultRotation = effectedObject.localRotation;

        if (image) defaultColor = image.color;
    }

    void Update()
    {
        Vector3 targetScale = defaultScale;
        Quaternion targetRotation = defaultRotation;
        Color targetColor = defaultColor;

        if (Clicked)
        {
            if (scaledOnClick)
                targetScale = Vector3.Scale(defaultScale, ValueScaleOnClick);

            targetColor = colorOnClick;
        }
        else if (isHovering)
        {
            if (scaledOnHover)
                targetScale = Vector3.Scale(defaultScale, valueScaleOnHover);

            if (rotateOnHover)
                targetRotation = Quaternion.Euler(valueRotateOnHover);

            targetColor = colorOnHover;
        }

        effectedObject.localScale = Vector3.Lerp(effectedObject.localScale, targetScale, Time.deltaTime * speed);
        effectedObject.localRotation = Quaternion.Lerp(effectedObject.localRotation, targetRotation, Time.deltaTime * speed);

        if (image)
            image.color = Color.Lerp(image.color, targetColor, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        isHovering = true;
        playAudio(audioOnHover);
    }

    public void OnPointerExit(PointerEventData e)
    {
        isHovering = Clicked = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        Clicked = true;
        playAudio(audioOnClick);

        if (particuleSystem)
            particuleSystem.Play();
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (Clicked && isHovering)
            Clicked = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isHovering = true;
        playAudio(audioOnHover);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isHovering = false;
        Clicked = false;
    }

    void playAudio(AudioClip c)
    {
        if (audioSource && c)
            audioSource.PlayOneShot(c);
    }
}