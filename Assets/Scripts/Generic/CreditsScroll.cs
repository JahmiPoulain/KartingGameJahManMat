using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CreditsScroll : MonoBehaviour
{
    public RectTransform content;
    public float speed = 50f;

    [SerializeField] private float delayBeforeStart = 2f;
    [SerializeField] private float delayAfterEnd = 2f;
    [SerializeField] private float endPositionY = 1000f;

    void Start()
    {
        StartCoroutine(ScrollCredits());
    }

    private IEnumerator ScrollCredits()
    {
        yield return new WaitForSeconds(delayBeforeStart); 
        
        while (content.anchoredPosition.y < endPositionY)
        {
            content.anchoredPosition += speed * Time.deltaTime * Vector2.up;
            yield return null;
        }
        
        yield return new WaitForSeconds(delayAfterEnd);
        SceneManager.LoadScene("MainMenu"); // Load the main menu scene
    }
}