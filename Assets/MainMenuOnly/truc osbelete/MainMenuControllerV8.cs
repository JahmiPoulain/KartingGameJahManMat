using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class MainMenuControllerV8 : MonoBehaviour
{
    private VisualElement masterContainer, mainScreen, rightWheel, leftWheel, playScreen, customCursor;
    private Button btnJouer, btnOptions, btnQuitter, btnVisuals;

    private enum MenuState { PressAnyKey, Main, Options, Play }
    private MenuState currentState = MenuState.PressAnyKey;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Récupération des conteneurs
        masterContainer = root.Q<VisualElement>("MasterContainer");
        mainScreen = root.Q<VisualElement>("MainScreen");
        leftWheel = root.Q<VisualElement>("LeftWheel");
        playScreen = root.Q<VisualElement>("PlayScreen");
        customCursor = root.Q<VisualElement>("CustomCursor");

        // Récupération des boutons
        btnJouer = root.Q<Button>("BtnJouer");
        btnOptions = root.Q<Button>("BtnOptions");
        btnQuitter = root.Q<Button>("BtnQuitter");
        btnVisuals = root.Q<Button>("BtnVisuals");

        // --- ACTIONS DES BOUTONS ---
        btnOptions.clicked += OpenOptions;
        btnJouer.clicked += OpenPlayMenu;
        btnQuitter.clicked += Application.Quit;

        // --- GESTION DU CURSEUR (Remplace UISelectionPointer) ---
        // S'abonne à tous les éléments qui prennent le focus (Manette ou Clavier)
        root.RegisterCallback<FocusInEvent>(OnFocusChanged);
    }

    void Update()
    {
        // 1. Écran titre : Appuyer sur une touche pour monter
        if (currentState == MenuState.PressAnyKey && Input.anyKeyDown)
        {
            currentState = MenuState.Main;
            masterContainer.AddToClassList("move-up"); // Monte tout le bloc
            btnJouer.Focus(); // Donne le focus au bouton Jouer
        }

        // 2. Bouton Retour (Échap ou Touche Sud/B de la manette)
        // Remplace "Cancel" par ton action InputSystem si tu en as une
        if (Input.GetKeyDown(KeyCode.Escape) || Gamepad.current?.buttonEast.wasPressedThisFrame == true)
        {
            GoBack();
        }
    }

    void OpenOptions()
    {
        currentState = MenuState.Options;
        mainScreen.AddToClassList("move-right"); // Glisse à droite
        leftWheel.AddToClassList("show-left-wheel"); // Fait entrer la roue gauche
        btnVisuals.Focus(); // Focus le premier bouton de la roue gauche
    }

    void OpenPlayMenu()
    {
        currentState = MenuState.Play;
        playScreen.RemoveFromClassList("pos-top");
        playScreen.AddToClassList("pos-center"); // Fait tomber la fenêtre
        playScreen.Q<Button>("BtnModeCourse").Focus();
    }

    void GoBack()
    {
        if (currentState == MenuState.Options)
        {
            currentState = MenuState.Main;
            mainScreen.RemoveFromClassList("move-right");
            leftWheel.RemoveFromClassList("show-left-wheel");
            btnOptions.Focus(); // On remet le focus sur Options
        }
        else if (currentState == MenuState.Play)
        {
            currentState = MenuState.Main;
            playScreen.RemoveFromClassList("pos-center");
            playScreen.AddToClassList("pos-top"); // Remonte la fenêtre
            btnJouer.Focus();
        }
    }

    // --- LA MAGIE DU CURSEUR ---
    private void OnFocusChanged(FocusInEvent evt)
    {
        VisualElement focusedElement = evt.target as VisualElement;

        if (focusedElement != null && focusedElement.resolvedStyle.display != DisplayStyle.None)
        {
            // Récupère la position du bouton à l'écran
            Vector2 targetPos = focusedElement.worldBound.position;

            // Déplace le pointeur juste à gauche du bouton (ajuste le -50 selon ton image)
            customCursor.style.translate = new StyleTranslate(new Translate(targetPos.x - 50, targetPos.y + 10));
        }
    }
}