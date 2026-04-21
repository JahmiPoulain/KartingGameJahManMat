using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class MainMenuUIController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement mainContainer;
    private VisualElement roueDroite;

    private Button btnJouer;
    private Button btnOption;
    private Button btnCredit;

    private int indexSelection = 0;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        mainContainer = root.Q<VisualElement>("MainContainer");
        btnJouer = root.Q<Button>("BtnJouer");
        btnOption = root.Q<Button>("BtnOption");
        btnCredit = root.Q<Button>("BtnCredit");

        btnOption.clicked += OnOptionClicked;

    }

    void OnOptionClicked()
    {
        mainContainer.style.translate = new StyleTranslate(new Translate(new Length(100, LengthUnit.Percent), 0));
    }

    public void OnBackInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            mainContainer.style.translate = new StyleTranslate(new Translate(new Length(0, LengthUnit.Percent), 0));
        }
    }
}