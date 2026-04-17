using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenuScript : MonoBehaviour
{
    public static MainMenuScript instance;

    [Header("Scenes Management")]
    public string SceneName;

    [Header("UI and Panels")]
    public GameObject MainCanva;
    public GameObject MainPanel;
    public GameObject SettingsPanel;
    public GameObject CreditsPanel;
    public GameObject GameModesPanel;

    public GameObject ControlsPanel;
    public GameObject AudioPanel;
    public GameObject VisualsPanel;

    [Header("FadeInOut effect")]
    public float fadeDuration = 1f;
    public CanvasGroup FadeCanva;

    private bool isFading = false;

    private Coroutine currentFade;

    private void Start()
    {
        instance = this;
        FadeCanva.gameObject.SetActive(true);
        FadeOut();
    }
    public void FadeIn()
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(Fade(0f, 1f));
    }

    public void FadeOut()
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(Fade(1f, 0f));
    }

    public void TogglePanel(GameObject panel)
    {
        panel.SetActive(!panel.activeSelf);
    }

    IEnumerator Fade(float start, float end)
    {
        isFading = true;
        float time = 0f;
        FadeCanva.alpha = start;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            FadeCanva.alpha = Mathf.Lerp(start, end, time / fadeDuration);
            yield return null;
        }

        FadeCanva.alpha = end;
        isFading= false;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    IEnumerator StartGameRoutine()
    {
        FadeIn();
        yield return new WaitUntil(() => isFading == false);
        SceneManager.LoadScene(SceneName);
    }

    public void StartGame()
    {
        StartCoroutine(StartGameRoutine());
    }

    public void StartTimeTrial()
    {
        GameManager.instance.currentMode = GameManager.GameMode.TimeTrial;
        StartGame();
    }

    public void StartTimeAttack()
    {
        GameManager.instance.currentMode = GameManager.GameMode.TimeAttack;
        StartGame();
    }

    public void OpenSettings()
    {
        if (SettingsPanel == null || CreditsPanel == null) return;
        SettingsPanel.SetActive(true);
        CreditsPanel.SetActive(false);
    }

    public void OpenCredits()
    {
        if (SettingsPanel == null || CreditsPanel == null) return;
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(true);
    }
}
