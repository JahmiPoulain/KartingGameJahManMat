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
    [SerializeField] private Button renameButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button chooseButton;

    [Header("Colors")]
    [SerializeField] private Color activePlayerColor = new Color(1f, 0.25f, 0.75f);
    [SerializeField] private Color inactivePlayerColor = new Color(0.25f, 0.55f, 1f);

    [Header("Labels")]
    [SerializeField] private string activeChooseLabel = "Actif";
    [SerializeField] private string inactiveChooseLabel = "Choisir";

    private string playerId;
    private string playerName;
    private bool isActivePlayer;

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

    private static void ConfigureButton(Button button, Action action, bool interactable)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.interactable = interactable;

        if (action != null)
            button.onClick.AddListener(() => action.Invoke());
    }
}
