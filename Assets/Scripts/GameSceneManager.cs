using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    [Header("Configuration des Scènes")]
    public string mainMenuSceneName = "MainMenu2_0";
    public string graphSceneName = "GraphScene";

    [Header("Transition UI")]
    public CanvasGroup transitionScreen;
    public float transitionDuration = 1f;

    [Header("Loading UI")]
    public RectTransform loadingIcon; // Glisse ton image ici
    public float rotationSpeed = -360f; // Vitesse de rotation (négatif = sens horaire)

    private List<string> loadedGameplayScenes = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (transitionScreen != null)
            {
                transitionScreen.alpha = 1f;
                transitionScreen.blocksRaycasts = true;
            }
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        StartCoroutine(Fade(0f));
    }

    public void LoadGame(string gameplaySceneName)
    {
        StartCoroutine(TransitionRoutine(gameplaySceneName, true));
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(TransitionRoutine(mainMenuSceneName, false));
    }

    private IEnumerator TransitionRoutine(string targetScene, bool isLoadingGame)
    {
        // 1. On affiche le fade
        yield return StartCoroutine(Fade(1f));

        // 2. On lance l'icône de chargement
        Coroutine spinCoroutine = null;
        if (loadingIcon != null)
        {
            loadingIcon.gameObject.SetActive(true);
            spinCoroutine = StartCoroutine(SpinIconRoutine());
        }

        // ASTUCE ANTI-FREEZE : On dit à Unity de ralentir le chargement pour laisser l'UI respirer
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        if (isLoadingGame)
        {
            yield return LoadAdditiveScene(targetScene);
            yield return LoadAdditiveScene(graphSceneName);

            Scene sceneToActivate = SceneManager.GetSceneByName(targetScene);
            if (sceneToActivate.IsValid() && sceneToActivate.isLoaded)
            {
                SceneManager.SetActiveScene(sceneToActivate);
            }

            if (IsSceneLoaded(mainMenuSceneName))
            {
                yield return SceneManager.UnloadSceneAsync(mainMenuSceneName);
            }
        }
        else
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Single);

            if (op != null)
            {
                // ASTUCE : On empêche la scène de s'activer tout de suite (c'est ça qui freeze !)
                op.allowSceneActivation = false;

                // Unity bloque le progrès à 0.9f tant que allowSceneActivation est false
                while (op.progress < 0.9f)
                {
                    yield return null;
                }

                // La scène est prête en arrière-plan, on autorise le gros "hit" final
                op.allowSceneActivation = true;
                while (!op.isDone) yield return null;
            }

            loadedGameplayScenes.Clear();
        }

        // On remet la priorité normale pour le jeu
        Application.backgroundLoadingPriority = ThreadPriority.Normal;

        // 3. On arrête l'icône et on la cache
        if (spinCoroutine != null) StopCoroutine(spinCoroutine);
        if (loadingIcon != null) loadingIcon.gameObject.SetActive(false);

        // 4. On enlève le fade
        yield return StartCoroutine(Fade(0f));
    }

    private IEnumerator LoadAdditiveScene(string sceneName)
    {
        if (!IsSceneLoaded(sceneName))
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            if (op == null) yield break;

            // Même technique ici pour les scènes additives
            op.allowSceneActivation = false;
            while (op.progress < 0.9f)
            {
                yield return null;
            }
            op.allowSceneActivation = true;

            while (!op.isDone) yield return null;
            if (!loadedGameplayScenes.Contains(sceneName)) loadedGameplayScenes.Add(sceneName);
        }
    }

    private bool IsSceneLoaded(string name)
    {
        Scene s = SceneManager.GetSceneByName(name);
        // CORRECTIF 4 : Toujours vérifier si la scène est "Valid" avant de demander son état.
        return s.IsValid() && s.isLoaded;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (transitionScreen == null) yield break;

        transitionScreen.blocksRaycasts = true;
        float startAlpha = transitionScreen.alpha;
        float time = 0;

        while (time < transitionDuration)
        {
            transitionScreen.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / transitionDuration);
            // CORRECTIF 2 : unscaledDeltaTime permet à la transition de fonctionner même si Time.timeScale = 0 (jeu en pause)
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        transitionScreen.alpha = targetAlpha;
        if (targetAlpha <= 0) transitionScreen.blocksRaycasts = false;
    }

    private IEnumerator SpinIconRoutine()
    {
        if (loadingIcon == null) yield break;

        while (true)
        {
            // On fait tourner l'icône sur l'axe Z en utilisant unscaledDeltaTime
            loadingIcon.Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
    }
}