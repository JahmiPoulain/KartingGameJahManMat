using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuUIManager : MonoBehaviour
{
    public static MenuUIManager Instance;

    public enum MenuState { TitleScreen, MainWheel, SubWheel, PanelMode }
    [Header("État Actuel")]
    public MenuState currentState = MenuState.TitleScreen;

    [Header("Conteneurs UI (RectTransforms)")]
    public RectTransform titleScreenUI;
    public RectTransform mainWheelUI;
    public RectTransform subWheelUI;
    public RectTransform currentOpenPanel; // Le panneau qui descend d'en haut

    [Header("Boutons par défaut (pour la manette)")]
    public GameObject firstMainWheelButton;
    public GameObject firstSubWheelButton;

    [Header("Inputs")]
    public InputActionReference backAction; // Bouton Sud (B/Rond)
    public InputActionReference anyKeyAction; // N'importe quelle touche pour le Title Screen

    [Header("Paramètres d'animation")]
    public float transitionSpeed = 10f;

    // Positions cibles pour l'animation (Lerp)
    private Vector2 mainWheelTargetPos;
    private Vector2 subWheelTargetPos;
    private Vector2 panelTargetPos;
    private Vector2 titleTargetPos;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1.0f;
    }

    private void Start()
    {
        // Initialisation des positions hors écran (à configurer selon ton Canvas)
        mainWheelTargetPos = new Vector2(1000, 0); // Caché à droite
        subWheelTargetPos = new Vector2(-1000, 0); // Caché à gauche
        titleTargetPos = Vector2.zero; // Au centre au début

        if (currentOpenPanel != null)
            panelTargetPos = new Vector2(0, 1500); // Caché en haut

        EnableInputs();
    }

    private void Update()
    {
        // Animations fluides de tes éléments UI vers leurs positions cibles
        mainWheelUI.anchoredPosition = Vector2.Lerp(mainWheelUI.anchoredPosition, mainWheelTargetPos, Time.deltaTime * transitionSpeed);
        subWheelUI.anchoredPosition = Vector2.Lerp(subWheelUI.anchoredPosition, subWheelTargetPos, Time.deltaTime * transitionSpeed);
        titleScreenUI.anchoredPosition = Vector2.Lerp(titleScreenUI.anchoredPosition, titleTargetPos, Time.deltaTime * transitionSpeed);

        if (currentOpenPanel != null)
            currentOpenPanel.anchoredPosition = Vector2.Lerp(currentOpenPanel.anchoredPosition, panelTargetPos, Time.deltaTime * transitionSpeed);
    }

    private void EnableInputs()
    {
        backAction.action.Enable();
        backAction.action.performed += OnBackButtonPressed;

        anyKeyAction.action.Enable();
        anyKeyAction.action.performed += OnAnyKeyPressed;
    }

    private void OnDisable()
    {
        backAction.action.performed -= OnBackButtonPressed;
        anyKeyAction.action.performed -= OnAnyKeyPressed;
    }

    // --- GESTION DES INPUTS ---

    private void OnAnyKeyPressed(InputAction.CallbackContext ctx)
    {
        if (currentState == MenuState.TitleScreen)
        {
            GoToMainWheel();
        }
    }

    private void OnBackButtonPressed(InputAction.CallbackContext ctx)
    {
        switch (currentState)
        {
            case MenuState.PanelMode:
                ClosePanelAndReturnToSubWheel(); // ou MainWheel selon le cas
                break;
            case MenuState.SubWheel:
                GoToMainWheel();
                break;
            case MenuState.MainWheel:
                GoToTitleScreen();
                break;
        }
    }

    // --- TRANSITIONS D'ÉCRANS ---

    public void GoToTitleScreen()
    {
        currentState = MenuState.TitleScreen;
        titleTargetPos = Vector2.zero; // Descend au centre
        mainWheelTargetPos = new Vector2(1000, 0); // Repart à droite
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void GoToMainWheel()
    {
        currentState = MenuState.MainWheel;
        titleTargetPos = new Vector2(0, 1500); // Monte et sort de l'écran
        mainWheelTargetPos = Vector2.zero;     // Arrive à droite
        subWheelTargetPos = new Vector2(-1000, 0); // Reste caché à gauche

        DynamicUICursor.Instance.SetCursorActive(false); // On cache le curseur libre !
        EventSystem.current.SetSelectedGameObject(firstMainWheelButton);
    }

    public void GoToSubWheel(GameObject firstSelectedButton)
    {
        currentState = MenuState.SubWheel;
        mainWheelTargetPos = new Vector2(1000, 0); // La roue principale s'en va à droite
        subWheelTargetPos = Vector2.zero;          // La roue secondaire vient de la gauche

        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }

    // Appelée quand on clique sur "Affichage", "Audio", ou "Jouer"
    public void OpenPanel(RectTransform panelToOpen)
    {
        currentState = MenuState.PanelMode;
        currentOpenPanel = panelToOpen;

        // Les deux roues dégagent sur les côtés
        mainWheelTargetPos = new Vector2(1000, 0);
        subWheelTargetPos = new Vector2(-1000, 0);

        // Le panneau descend du haut
        panelTargetPos = Vector2.zero;

        // ON ACTIVE LE CURSEUR LIBRE !
        DynamicUICursor.Instance.SetCursorActive(true);

        // Sélectionne le premier élément du panneau (à configurer dans l'EventSystem)
        EventSystem.current.SetSelectedGameObject(panelToOpen.GetComponentInChildren<UnityEngine.UI.Selectable>().gameObject);
    }

    public void ClosePanelAndReturnToSubWheel()
    {
        currentState = MenuState.SubWheel;
        panelTargetPos = new Vector2(0, 1500); // Le panneau remonte
        subWheelTargetPos = Vector2.zero;      // La roue secondaire revient

        DynamicUICursor.Instance.SetCursorActive(false); // On recache le curseur
        EventSystem.current.SetSelectedGameObject(firstSubWheelButton);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}