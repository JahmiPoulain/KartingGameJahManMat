using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManagementRow : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI chooseButtonText;

    [Header("Buttons")]
    [SerializeField] private Button chooseButton;
    [SerializeField] private Button renameButton;
    [SerializeField] private Button deleteButton;

    [Header("Colors")]
    [SerializeField] private Color activePlayerColor = new Color(0.9490196f, 0.654902f, 0.7098039f);
    [SerializeField] private Color inactivePlayerColor = new Color(0.4313726f, 0.5176471f, 0.6156863f);

    [Header("Labels")]
    [SerializeField] private string activeChooseLabel = "Active";
    [SerializeField] private string inactiveChooseLabel = "Choose";

    private string playerId;
    private string playerName;
    private bool isActivePlayer;

    public Button FirstSelectableButton
    {
        get
        {
            if (IsSelectable(chooseButton))
                return chooseButton;

            if (IsSelectable(renameButton))
                return renameButton;

            if (IsSelectable(deleteButton))
                return deleteButton;

            return null;
        }
    }

    public void SetPlayer(
        string id,
        string displayName,
        bool isActive,
        Action<string, string> onRenameRequested,
        Action<string, string> onDeleteRequested,
        Action<string> onChooseRequested
    )
    {
        playerId = id;
        playerName = displayName;
        isActivePlayer = isActive;

        gameObject.SetActive(true);

        Color rowColor = isActivePlayer ? activePlayerColor : inactivePlayerColor;

        if (nameText != null)
        {
            nameText.text = playerName;
            nameText.color = rowColor;
        }

        if (chooseButtonText != null)
        {
            chooseButtonText.text = isActivePlayer ? activeChooseLabel : inactiveChooseLabel;
            chooseButtonText.color = rowColor;
        }

        ConfigureButton(renameButton, () => onRenameRequested?.Invoke(playerId, playerName), true);
        ConfigureButton(deleteButton, () => onDeleteRequested?.Invoke(playerId, playerName), true);
        ConfigureButton(chooseButton, () =>
        {
            if (!isActivePlayer)
                onChooseRequested?.Invoke(playerId);
        }, !isActivePlayer);

        RefreshControllerNavigation();
    }

    public void Clear()
    {
        playerId = string.Empty;
        playerName = string.Empty;
        isActivePlayer = false;

        if (nameText != null)
            nameText.text = string.Empty;

        if (chooseButtonText != null)
            chooseButtonText.text = inactiveChooseLabel;

        ConfigureButton(renameButton, null, false);
        ConfigureButton(deleteButton, null, false);
        ConfigureButton(chooseButton, null, false);

        gameObject.SetActive(false);
    }

    public void RefreshControllerNavigation()
    {
        SetAutomaticNavigation(chooseButton);
        SetAutomaticNavigation(renameButton);
        SetAutomaticNavigation(deleteButton);
    }

    private static void ConfigureButton(Button button, Action action, bool interactable)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.interactable = interactable;

        if (action != null)
            button.onClick.AddListener(() => action.Invoke());
    }

    private static bool IsSelectable(Selectable selectable)
    {
        return selectable != null && selectable.gameObject.activeInHierarchy && selectable.interactable;
    }

    private static void SetAutomaticNavigation(Selectable selectable)
    {
        if (selectable == null)
            return;

        Navigation navigation = selectable.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        selectable.navigation = navigation;
    }
}
