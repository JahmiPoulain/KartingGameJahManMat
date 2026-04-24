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

    private List<string> loadedGameplayScenes = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
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
        yield return StartCoroutine(Fade(1f));

        if (isLoadingGame)
        {
            // 1. CHARGER d'abord les nouvelles scènes en additif
            yield return LoadAdditiveScene(targetScene);
            yield return LoadAdditiveScene(graphSceneName);

            // 2. Définir la scène active (important pour la physique et la lumière)
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(targetScene));

            // 3. DÉCHARGER le menu seulement maintenant que le reste est prêt
            if (IsSceneLoaded(mainMenuSceneName))
            {
                yield return SceneManager.UnloadSceneAsync(mainMenuSceneName);
            }
        }
        else
        {
            // Pour le retour au menu, on utilise LoadSceneMode.Single.
            // C'est le "bouton nucléaire" : ça décharge TOUT automatiquement (gameplay + graphismes)
            // et ça ne garde que le menu. C'est beaucoup plus propre et sans erreurs.
            AsyncOperation op = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;

            loadedGameplayScenes.Clear();
        }

        yield return StartCoroutine(Fade(0f));
    }

    private IEnumerator LoadAdditiveScene(string sceneName)
    {
        // On vérifie si la scène n'est pas déjà là pour éviter les doublons
        if (!IsSceneLoaded(sceneName))
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            if (!loadedGameplayScenes.Contains(sceneName)) loadedGameplayScenes.Add(sceneName);
        }
    }

    // Petite fonction de sécurité pour vérifier l'état d'une scène
    private bool IsSceneLoaded(string name)
    {
        Scene s = SceneManager.GetSceneByName(name);
        return s.isLoaded;
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
            time += Time.deltaTime;
            yield return null;
        }
        transitionScreen.alpha = targetAlpha;
        if (targetAlpha <= 0) transitionScreen.blocksRaycasts = false;
    }
}