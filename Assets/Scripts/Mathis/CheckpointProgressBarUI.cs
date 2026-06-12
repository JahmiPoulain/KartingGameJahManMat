using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CheckpointProgressBarUI : MonoBehaviour
{
    public static CheckpointProgressBarUI Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform cursorRect;
    [SerializeField] private RectTransform progressBarRect;

    [Header("Delta Display (e.g., LapTimeUI)")]
    [SerializeField] private TextMeshProUGUI deltaText; // Glisses-y ton LapTimeUI ou équivalent

    [Header("Prefabs & Containers")]
    [SerializeField] private GameObject tickPrefab; // Image avec la texture 'checkpoint.png'
    [SerializeField] private Transform ticksContainer;

    [Header("Delta Text Colors")]
    [SerializeField] private Color aheadColor = Color.green;    // Plus rapide (Nouveau record)
    [SerializeField] private Color behindColor = Color.red;     // Plus lent

    private CheckpointManager checkpointManager;
    private int totalCheckpoints;

    // Sauvegarde locale du meilleur temps de passage absolu pour chaque checkpoint (Clé: Index du Checkpoint, Valeur: Temps en secondes)
    private Dictionary<int, float> personalBestCheckpointTimes = new Dictionary<int, float>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void InitializeUI(CheckpointManager manager)
    {
        checkpointManager = manager;
        totalCheckpoints = manager.TotalCheckpointCount;

        // Nettoyage des anciens repères visuels
        foreach (Transform child in ticksContainer) Destroy(child.gameObject);

        // Récupère la largeur réelle disponible de la barre
        float width = progressBarRect.rect.width;

        // Génération automatique des graduations le long de la barre (depuis la gauche vers la droite)
        for (int i = 1; i <= totalCheckpoints; i++)
        {
            GameObject tick = Instantiate(tickPrefab, ticksContainer);
            RectTransform tickRect = tick.GetComponent<RectTransform>();

            // Ratio de progression (Ex: si 4 checkpoints : 0.25, 0.50, 0.75, 1.00)
            float progressRatio = (float)i / totalCheckpoints;

            // Calcul de la position depuis le bord gauche (0) jusqu'au bord droit (width)
            float xPosition = progressRatio * width;

            // Comme le TicksContainer a son pivot à gauche (0), PosX = 0 signifie l'origine exacte de la jauge
            tickRect.anchoredPosition = new Vector2(xPosition, 0f);
        }

        ResetProgressBar();
    }

    public void OnCheckpointPassed(int checkpointIndex, float currentLapTime)
    {
        // 1. Mise à jour de la jauge et du curseur
        float progress = (float)checkpointIndex / totalCheckpoints;
        fillImage.fillAmount = progress;

        float width = progressBarRect.rect.width;

        // Le curseur glisse désormais de 0 (tout à gauche) à la largeur max (tout à droite)
        float newCursorX = progress * width;
        cursorRect.anchoredPosition = new Vector2(newCursorX, cursorRect.anchoredPosition.y);

        // 2. Calcul du Delta de temps par rapport au meilleur passage historique à ce checkpoint
        string savedKey = GetCheckpointSaveKey(checkpointIndex);

        // Charger le record du checkpoint depuis les PlayerPrefs s'il n'est pas encore en mémoire
        if (!personalBestCheckpointTimes.ContainsKey(checkpointIndex))
        {
            if (PlayerPrefs.HasKey(savedKey))
            {
                personalBestCheckpointTimes[checkpointIndex] = PlayerPrefs.GetFloat(savedKey);
            }
        }

        if (!personalBestCheckpointTimes.ContainsKey(checkpointIndex))
        {
            // Premier passage absolu dans l'histoire du jeu : on définit le temps de référence
            personalBestCheckpointTimes[checkpointIndex] = currentLapTime;
            PlayerPrefs.SetFloat(savedKey, currentLapTime);
            PlayerPrefs.Save();

            DisplayDeltaText(0f, true); // Indique "PASSED" ou "SEC. RECORD"
        }
        else
        {
            // Comparaison : Temps actuel - Meilleur temps enregistré
            float delta = currentLapTime - personalBestCheckpointTimes[checkpointIndex];

            if (delta < 0f)
            {
                // Nouveau record absolu sur ce secteur ! On écrase et on sauvegarde
                personalBestCheckpointTimes[checkpointIndex] = currentLapTime;
                PlayerPrefs.SetFloat(savedKey, currentLapTime);
                PlayerPrefs.Save();

                DisplayDeltaText(delta, true);
            }
            else
            {
                // Plus lent que le record
                DisplayDeltaText(delta, false);
            }
        }
    }

    private void DisplayDeltaText(float delta, bool isAhead)
    {
        if (deltaText == null) return;

        StopAllCoroutines();

        if (delta == 0f)
        {
            deltaText.text = "RECORD SET";
            deltaText.color = Color.white;
        }
        else
        {
            // Formatage de l'affichage en anglais (-00:01.230 ou +00:00.450)
            string sign = isAhead ? "-" : "+";
            float absDelta = Mathf.Abs(delta);
            int minutes = (int)(absDelta / 60);
            float seconds = absDelta % 60;

            deltaText.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}{1:00}:{2:00.000}", sign, minutes, seconds);
            deltaText.color = isAhead ? aheadColor : behindColor;
        }

        // On lance la disparition progressive après 2.5 secondes
        StartCoroutine(FadeOutDeltaText());
    }

    private IEnumerator FadeOutDeltaText()
    {
        yield return new WaitForSeconds(2.5f);
        deltaText.text = "";
    }

    public void ResetProgressBar()
    {
        fillImage.fillAmount = 0f;
        // Remet le curseur au point 0 (tout à gauche) au début du tour
        cursorRect.anchoredPosition = new Vector2(0f, cursorRect.anchoredPosition.y);
    }

    private string GetCheckpointSaveKey(int checkpointIndex)
    {
        // Différencie proprement les records selon que la map soit inversée ou non
        string suffix = (InversionCatcher.instance != null && InversionCatcher.instance.Inverted) ? "_Inverted" : "_Normal";
        return $"BestCheckpointTime_{checkpointIndex}{suffix}";
    }
}