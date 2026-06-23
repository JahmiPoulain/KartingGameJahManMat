using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Checkpoint Progress Bar UI
/// 
/// HIERARCHY SETUP (Canvas > CheckpointProgressBar):
/// 
///   CheckpointProgressBar          (RectTransform) → assign to [progressBarRect]
///   ├── Background                 (Image → checkpoint_contour.png)
///   ├── FillArea                   (RectTransform, anchors: left=0 right=0 top=0 bot=0, no Image)
///   │   └── Fill                   (Image → checkpoint_fond.png, anchor stretch full)
///   │                              → assign Fill's RectTransform to [fillRect]
///   ├── TicksContainer             (RectTransform, anchors stretch full) → assign to [ticksContainer]
///   └── Cursor                     (Image → checkpoint_curseur.png, anchor: left=0, pivot X=0.5)
///                                  → assign to [cursorRect]
///
/// HOW THE FILL WORKS:
///   Fill's anchorMax.x is driven from 0 to 1 (no sizeDelta tricks, no mask needed).
///   This is pixel-perfect at any canvas resolution.
///
/// HOW TICKS WORK:
///   Each tick anchor is set to (ratio, 0.5) so they scale with the bar automatically.
/// </summary>
public class CheckpointProgressBarUI : MonoBehaviour
{
    public static CheckpointProgressBarUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  Inspector References
    // ─────────────────────────────────────────────

    [Header("Bar Rects")]
    [Tooltip("Root RectTransform of the whole bar (the 'contour' image object).")]
    [SerializeField] private RectTransform progressBarRect;

    [Tooltip("The fill image (checkpoint_fond). Its anchorMax.x will be driven 0→1.")]
    [SerializeField] private RectTransform fillRect;

    [Tooltip("The cursor image (checkpoint_curseur). Anchored left, pivot centred.")]
    [SerializeField] private RectTransform cursorRect;

    [Header("Ticks")]
    [Tooltip("Parent container for checkpoint tick marks.")]
    [SerializeField] private RectTransform ticksContainer;

    [Tooltip("Prefab: a small Image using checkpoint.png, pivot (0.5, 0.5), anchors (0,0.5).")]
    [SerializeField] private GameObject tickPrefab;

    [Header("Delta Time Display")]
    [SerializeField] private TextMeshProUGUI deltaText;
    [SerializeField] private Color aheadColor = new Color(0.2f, 1f, 0.4f);
    [SerializeField] private Color behindColor = new Color(1f, 0.3f, 0.3f);

    [Header("Animation")]
    [Tooltip("How fast the fill and cursor lerp toward the target (units/sec, set 0 for instant).")]
    [SerializeField][Range(0f, 20f)] private float smoothSpeed = 8f;

    // ─────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────

    private int totalCheckpoints;
    private float targetProgress; // 0 → 1
    private float currentProgress;

    private Coroutine deltaFadeCoroutine;

    private readonly Dictionary<int, float> bestTimes = new Dictionary<int, float>();

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Update()
    {

        if (Mathf.Abs(currentProgress - targetProgress) < 0.001f)
        {
            currentProgress = targetProgress;
            ApplyProgress(currentProgress);
            return;
        }
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * smoothSpeed);
        ApplyProgress(currentProgress);
    }


    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>Call this once when the race/lap starts, before any checkpoint is passed.</summary>
    public void InitializeUI(int checkpointCount)
    {
        totalCheckpoints = checkpointCount;
        currentProgress = 0f;
        targetProgress = 0f;

        ApplyProgress(0f);
        ClearDeltaText();

        // Must wait one frame so Unity has calculated RectTransform sizes.
        StartCoroutine(BuildTicksNextFrame());
    }

    /// <summary>Overload accepting a CheckpointManager (keeps compatibility with previous code).</summary>
    public void InitializeUI(CheckpointManager manager)
    {
        InitializeUI(manager.TotalCheckpointCount);
    }

    /// <summary>Call each time the player crosses a checkpoint.</summary>
    public void OnCheckpointPassed(int checkpointIndex, float currentLapTime)
    {
        // Advance fill to this checkpoint's position
        bool inverted = InversionCatcher.instance != null && InversionCatcher.instance.Inverted;
        targetProgress = inverted
            ? 1f - ((float)(checkpointIndex - 1) / totalCheckpoints)
            : (float)checkpointIndex / totalCheckpoints;
        Debug.Log($"[ProgressBar] checkpoint={checkpointIndex}, total={totalCheckpoints}, target={targetProgress}, barWidth={progressBarRect.rect.width}");

        // ── Delta Logic ──────────────────────────────
        string key = GetSaveKey(checkpointIndex);

        // Lazy-load best time from PlayerPrefs on first encounter this session
        if (!bestTimes.ContainsKey(checkpointIndex) && PlayerPrefs.HasKey(key))
            bestTimes[checkpointIndex] = PlayerPrefs.GetFloat(key);

        if (!bestTimes.ContainsKey(checkpointIndex))
        {
            // First ever time at this checkpoint → set as best, show "RECORD SET"
            SaveBest(checkpointIndex, currentLapTime, key);
            ShowDelta(0f, isAhead: true, isFirst: true);
        }
        else
        {
            float delta = currentLapTime - bestTimes[checkpointIndex];
            if (delta < 0f)
            {
                // New best!
                SaveBest(checkpointIndex, currentLapTime, key);
            }
            ShowDelta(delta, isAhead: delta <= 0f, isFirst: false);
        }
    }

    /// <summary>Resets the bar to empty (call at lap start / race reset).</summary>
    public void ResetProgressBar()
    {
        targetProgress = 0f;
        currentProgress = 0f;
        ApplyProgress(0f);
        ClearDeltaText();
    }

    // ─────────────────────────────────────────────
    //  Internal Helpers
    // ─────────────────────────────────────────────

    /// <summary>
    /// Drives the fill and cursor from a 0–1 progress value.
    /// Uses anchorMax.x so it works at any canvas resolution without reading pixel widths.
    /// </summary>
    private void ApplyProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        bool inverted = InversionCatcher.instance != null && InversionCatcher.instance.Inverted;

        if (fillRect != null)
        {
            if (inverted)
            {
                // Remplissage de droite à gauche : anchorMin.x va de 1 → 0
                Vector2 anchorMin = fillRect.anchorMin;
                anchorMin.x = 1f - progress;
                fillRect.anchorMin = anchorMin;

                // anchorMax.x fixé à 1
                Vector2 anchorMax = fillRect.anchorMax;
                anchorMax.x = 1f;
                fillRect.anchorMax = anchorMax;
            }
            else
            {
                // Remplissage normal de gauche à droite
                Vector2 anchorMin = fillRect.anchorMin;
                anchorMin.x = 0f;
                fillRect.anchorMin = anchorMin;

                Vector2 anchorMax = fillRect.anchorMax;
                anchorMax.x = progress;
                fillRect.anchorMax = anchorMax;
            }

            Vector2 sd = fillRect.sizeDelta;
            sd.x = 0f;
            fillRect.sizeDelta = sd;
        }

        if (cursorRect != null && progressBarRect != null)
        {
            float barWidth = progressBarRect.rect.width;

            if (inverted)
            {
                // Curseur part de la droite
                cursorRect.anchoredPosition = new Vector2(
                    barWidth - (progress * barWidth),
                    cursorRect.anchoredPosition.y
                );
                // Retourne le curseur horizontalement
                cursorRect.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                cursorRect.anchoredPosition = new Vector2(
                    progress * barWidth,
                    cursorRect.anchoredPosition.y
                );
                cursorRect.localScale = Vector3.one;
            }
        }
    }

    /// <summary>Waits one frame then builds tick marks so rect sizes are ready.</summary>
    private IEnumerator BuildTicksNextFrame()
    {
        yield return null;

        foreach (Transform child in ticksContainer)
            Destroy(child.gameObject);

        for (int i = 1; i <= totalCheckpoints; i++)
        {
            GameObject tick = Instantiate(tickPrefab, ticksContainer);
            RectTransform tickRect = tick.GetComponent<RectTransform>();

            float ratio = (float)i / totalCheckpoints;
            bool inverted = InversionCatcher.instance != null && InversionCatcher.instance.Inverted;

            Vector2 pos = tickRect.anchoredPosition;
            pos.x = inverted
                ? ticksContainer.rect.width - (ratio * ticksContainer.rect.width) - 10f
                : (ratio * ticksContainer.rect.width) - 10f;
            pos.y = 10f;
            tickRect.anchoredPosition = pos;
        }
    }

    private void ShowDelta(float delta, bool isAhead, bool isFirst)
    {
        if (deltaText == null) return;

        if (deltaFadeCoroutine != null)
            StopCoroutine(deltaFadeCoroutine);

        if (isFirst || delta == 0f)
        {
            deltaText.text = "Recor Set";
            deltaText.color = Color.white;
        }
        else
        {
            string sign = isAhead ? "-" : "+";
            float abs = Mathf.Abs(delta);
            int mins = (int)(abs / 60f);
            float secs = abs % 60f;

            deltaText.text = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}{1:00}:{2:00.000}", sign, mins, secs);
            deltaText.color = isAhead ? aheadColor : behindColor;
        }

        deltaText.alpha = 1f;
        deltaFadeCoroutine = StartCoroutine(FadeOutDelta());
    }

    private IEnumerator FadeOutDelta()
    {
        yield return new WaitForSeconds(2.5f);

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            deltaText.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        deltaText.alpha = 0f;
        deltaText.text = "";
    }

    private void ClearDeltaText()
    {
        if (deltaFadeCoroutine != null) StopCoroutine(deltaFadeCoroutine);
        if (deltaText != null) { deltaText.text = ""; deltaText.alpha = 1f; }
    }

    private void SaveBest(int index, float time, string key)
    {
        bestTimes[index] = time;
        PlayerPrefs.SetFloat(key, time);
        PlayerPrefs.Save();
    }

    private string GetSaveKey(int checkpointIndex)
    {
        string suffix = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted)
            ? "_Inverted" : "_Normal";
        return ActiveProfileBridge.Key($"BestCheckpointTime_{checkpointIndex}{suffix}");
    }
}