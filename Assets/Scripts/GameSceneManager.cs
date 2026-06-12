using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    [Header("Configuration des Scènes")]
    public string mainMenuSceneName = "MainMenu2_0";
    public string graphSceneName = "GraphScene";

    [Header("Ressources de Transition")]
    [SerializeField] private VideoClip transitionClip;
    [SerializeField] private Sprite loadingIconSprite;

    [Tooltip("La couleur qui cache le jeu pendant les 5 frames de chargement de la vidéo (souvent noir)")]
    public Color loadingBackgroundColor = Color.black;

    private Canvas transitionCanvas;
    private RawImage videoRenderImage;
    private VideoPlayer videoPlayer;
    private RectTransform loadingIcon;
    private Image backgroundBlocker;

    private float rotationSpeed = -360f;
    private List<string> loadedGameplayScenes = new List<string>();
    private bool isTransitioning = false;
    private string pendingGameplaySceneName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateTransitionUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        StartCoroutine(PlayVideoFromMiddleOnStart());
    }

    private void CreateTransitionUI()
    {
        GameObject canvasGo = new GameObject("TransitionCanvas");
        canvasGo.transform.SetParent(this.transform);
        transitionCanvas = canvasGo.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        transitionCanvas.sortingOrder = 32767;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject bgGo = new GameObject("BackgroundBlocker");
        bgGo.transform.SetParent(canvasGo.transform, false);
        backgroundBlocker = bgGo.AddComponent<Image>();
        backgroundBlocker.color = loadingBackgroundColor;

        RectTransform bgRect = backgroundBlocker.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        RenderTexture memoryRenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        memoryRenderTexture.Create();

        GameObject rawImageGo = new GameObject("VideoRenderImage");
        rawImageGo.transform.SetParent(canvasGo.transform, false);
        videoRenderImage = rawImageGo.AddComponent<RawImage>();

        RectTransform rect = videoRenderImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        videoRenderImage.texture = memoryRenderTexture;
        videoRenderImage.color = Color.white;

        videoPlayer = canvasGo.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = memoryRenderTexture;
        videoPlayer.clip = transitionClip;

        if (loadingIconSprite != null)
        {
            GameObject iconGo = new GameObject("LoadingIcon");
            iconGo.transform.SetParent(canvasGo.transform, false);
            Image img = iconGo.AddComponent<Image>();
            img.sprite = loadingIconSprite;
            loadingIcon = iconGo.GetComponent<RectTransform>();
            loadingIcon.sizeDelta = new Vector2(120, 120);
            iconGo.SetActive(false);
        }

        transitionCanvas.gameObject.SetActive(false);
    }

    public void LoadGame(string gameplaySceneName)
    {
        if (isTransitioning)
        {
            pendingGameplaySceneName = gameplaySceneName;
            Debug.Log($"Chargement de {gameplaySceneName} mis en attente : transition en cours.");
            return;
        }

        StartCoroutine(TransitionRoutine(gameplaySceneName, true));
    }

    public void ReturnToMainMenu()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(mainMenuSceneName, false));
    }

    private IEnumerator TransitionRoutine(string targetScene, bool isLoadingGame)
    {
        isTransitioning = true;
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        transitionCanvas.gameObject.SetActive(true);
        backgroundBlocker.enabled = false;

        yield return StartCoroutine(PlayVideoUntilMiddle());

        Coroutine spinCoroutine = null;
        if (loadingIcon != null)
        {
            loadingIcon.gameObject.SetActive(true);
            spinCoroutine = StartCoroutine(SpinIconRoutine());
        }

        if (isLoadingGame)
        {
            yield return StartCoroutine(LoadAdditiveScene(graphSceneName));
            yield return StartCoroutine(LoadAdditiveScene(targetScene));

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
            while (!op.isDone) yield return null;
            loadedGameplayScenes.Clear();
        }

        if (spinCoroutine != null) StopCoroutine(spinCoroutine);
        if (loadingIcon != null) loadingIcon.gameObject.SetActive(false);

        yield return StartCoroutine(FinishVideoRoutine());

        transitionCanvas.gameObject.SetActive(false);
        Application.backgroundLoadingPriority = ThreadPriority.Normal;
        isTransitioning = false;

        TryConsumePendingGameLoad();
    }

    private IEnumerator PlayVideoFromMiddleOnStart()
    {
        isTransitioning = true;
        transitionCanvas.gameObject.SetActive(true);

        backgroundBlocker.enabled = true;

        if (videoPlayer != null && videoPlayer.clip != null)
        {
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared) yield return null;

            videoPlayer.time = videoPlayer.length / 2.0;
            videoPlayer.Play();

            while (!videoPlayer.isPlaying) yield return null;

            backgroundBlocker.enabled = false;

            while (videoPlayer.isPlaying) yield return null;
        }

        transitionCanvas.gameObject.SetActive(false);
        isTransitioning = false;

        TryConsumePendingGameLoad();
    }

    private void TryConsumePendingGameLoad()
    {
        if (string.IsNullOrWhiteSpace(pendingGameplaySceneName) || isTransitioning)
            return;

        string sceneToLoad = pendingGameplaySceneName;
        pendingGameplaySceneName = null;
        LoadGame(sceneToLoad);
    }

    private IEnumerator PlayVideoUntilMiddle()
    {
        if (videoPlayer == null || videoPlayer.clip == null) yield break;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        videoPlayer.time = 0;
        videoPlayer.Play();

        double halfDuration = videoPlayer.length / 2.0;
        while (videoPlayer.time < halfDuration && videoPlayer.isPlaying)
        {
            yield return null;
        }

        videoPlayer.Pause();
    }

    private IEnumerator FinishVideoRoutine()
    {
        if (videoPlayer == null || videoPlayer.clip == null) yield break;

        videoPlayer.Play();
        yield return new WaitForSecondsRealtime(0.1f);

        while (videoPlayer.isPlaying) yield return null;
    }

    private IEnumerator LoadAdditiveScene(string sceneName)
    {
        if (!IsSceneLoaded(sceneName))
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            if (!loadedGameplayScenes.Contains(sceneName)) loadedGameplayScenes.Add(sceneName);
        }
    }

    private IEnumerator SpinIconRoutine()
    {
        if (loadingIcon == null) yield break;
        while (true)
        {
            loadingIcon.Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
    }

    private bool IsSceneLoaded(string name)
    {
        Scene s = SceneManager.GetSceneByName(name);
        return s.IsValid() && s.isLoaded;
    }
}