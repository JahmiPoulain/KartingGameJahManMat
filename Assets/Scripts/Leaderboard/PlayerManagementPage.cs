using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManagementPage : MonoBehaviour
{
    private const int MaxRows = 10;

    [Header("Rows")]
    [SerializeField] private PlayerManagementRow[] rows = new PlayerManagementRow[MaxRows];

    [Header("Add Player")]
    [SerializeField] private Button addPlayerButton;

    [Header("Popup Background / Blur")]
    [Tooltip("Image/panel semi-transparent ou blurred placé devant la page joueurs et derrière le popup.")]
    [SerializeField] private GameObject popupBackgroundRoot;

    [Header("Name Popup")]
    [SerializeField] private GameObject namePopupRoot;
    [SerializeField] private TextMeshProUGUI namePopupTitleText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button confirmNameButton;
    [SerializeField] private Button cancelNameButton;

    [Header("Delete Confirmation Popup")]
    [SerializeField] private GameObject deletePopupRoot;
    [SerializeField] private TextMeshProUGUI deletePopupText;
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private Button cancelDeleteButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Labels")]
    [SerializeField] private string addTitle = "Ajouter un joueur";
    [SerializeField] private string renameTitle = "Renommer le joueur";
    [SerializeField] private string deleteConfirmationFormat = "Supprimer {0} ?";

    private Action<string> addPlayerCallback;
    private Action<string, string> renamePlayerCallback;
    private Action<string> deletePlayerCallback;
    private Action<string> choosePlayerCallback;

    private string editedPlayerId;
    private string deletedPlayerId;
    private bool isRenamePopup;
    private bool isPopupVisible;
    private bool canAddPlayer = true;
    private int maxPlayerNameLength = 12;

    private void Awake()
    {
        BindButtons();
        CloseAllPopups();
    }

    private void OnEnable()
    {
        BindButtons();
    }

    public void SetMaxPlayerNameLength(int maxLength)
    {
        maxPlayerNameLength = Mathf.Max(1, maxLength);

        if (nameInputField != null)
            nameInputField.characterLimit = maxPlayerNameLength;
    }

    public void SetCanAddPlayer(bool canAdd)
    {
        canAddPlayer = canAdd;
        UpdateAddPlayerButtonVisibility();
    }

    public void SetCallbacks(
        Action<string> onAddPlayer,
        Action<string, string> onRenamePlayer,
        Action<string> onDeletePlayer,
        Action<string> onChoosePlayer
    )
    {
        addPlayerCallback = onAddPlayer;
        renamePlayerCallback = onRenamePlayer;
        deletePlayerCallback = onDeletePlayer;
        choosePlayerCallback = onChoosePlayer;
    }

    public void ClearPage()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] != null)
                rows[i].Clear();
        }
    }

    public void SetPlayer(int rowIndex, string playerId, string playerName, bool isActive)
    {
        if (rowIndex < 0 || rowIndex >= rows.Length || rows[rowIndex] == null)
            return;

        rows[rowIndex].SetPlayer(
            playerId,
            playerName,
            isActive,
            OpenRenamePopup,
            OpenDeletePopup,
            id => choosePlayerCallback?.Invoke(id)
        );
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message ?? string.Empty;
    }

    public void CloseAllPopups()
    {
        SetPopupBackgroundVisible(false);

        if (namePopupRoot != null)
            namePopupRoot.SetActive(false);

        if (deletePopupRoot != null)
            deletePopupRoot.SetActive(false);

        editedPlayerId = string.Empty;
        deletedPlayerId = string.Empty;
        isRenamePopup = false;
    }

    private void BindButtons()
    {
        if (addPlayerButton != null)
        {
            addPlayerButton.onClick.RemoveListener(OpenAddPopup);
            addPlayerButton.onClick.AddListener(OpenAddPopup);
        }

        if (confirmNameButton != null)
        {
            confirmNameButton.onClick.RemoveListener(ConfirmNamePopup);
            confirmNameButton.onClick.AddListener(ConfirmNamePopup);
        }

        if (cancelNameButton != null)
        {
            cancelNameButton.onClick.RemoveListener(CloseAllPopups);
            cancelNameButton.onClick.AddListener(CloseAllPopups);
        }

        if (confirmDeleteButton != null)
        {
            confirmDeleteButton.onClick.RemoveListener(ConfirmDeletePopup);
            confirmDeleteButton.onClick.AddListener(ConfirmDeletePopup);
        }

        if (cancelDeleteButton != null)
        {
            cancelDeleteButton.onClick.RemoveListener(CloseAllPopups);
            cancelDeleteButton.onClick.AddListener(CloseAllPopups);
        }
    }

    private void OpenAddPopup()
    {
        if (!canAddPlayer)
            return;

        isRenamePopup = false;
        editedPlayerId = string.Empty;

        OpenNamePopup(addTitle, string.Empty);
    }

    private void OpenRenamePopup(string playerId, string currentName)
    {
        isRenamePopup = true;
        editedPlayerId = playerId;

        OpenNamePopup(string.Format(renameTitle, currentName), currentName);
    }

    private void OpenNamePopup(string title, string initialValue)
    {
        SetPopupBackgroundVisible(true);

        if (deletePopupRoot != null)
            deletePopupRoot.SetActive(false);

        if (namePopupRoot != null)
            namePopupRoot.SetActive(true);

        if (namePopupTitleText != null)
            namePopupTitleText.text = title;

        if (nameInputField != null)
        {
            nameInputField.characterLimit = maxPlayerNameLength;
            nameInputField.text = initialValue ?? string.Empty;
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }
    }

    private void ConfirmNamePopup()
    {
        string playerName = nameInputField != null ? nameInputField.text : string.Empty;

        if (isRenamePopup)
            renamePlayerCallback?.Invoke(editedPlayerId, playerName);
        else
            addPlayerCallback?.Invoke(playerName);

        CloseAllPopups();
    }

    private void OpenDeletePopup(string playerId, string playerName)
    {
        deletedPlayerId = playerId;

        SetPopupBackgroundVisible(true);

        if (namePopupRoot != null)
            namePopupRoot.SetActive(false);

        if (deletePopupRoot != null)
            deletePopupRoot.SetActive(true);

        if (deletePopupText != null)
            deletePopupText.text = string.Format(deleteConfirmationFormat, playerName);
    }

    private void ConfirmDeletePopup()
    {
        string playerId = deletedPlayerId;

        CloseAllPopups();

        if (!string.IsNullOrWhiteSpace(playerId))
            deletePlayerCallback?.Invoke(playerId);
    }

    private void SetPopupBackgroundVisible(bool visible)
    {
        isPopupVisible = visible;

        if (popupBackgroundRoot != null)
            popupBackgroundRoot.SetActive(visible);

        UpdateAddPlayerButtonVisibility();
    }

    private void UpdateAddPlayerButtonVisibility()
    {
        if (addPlayerButton != null)
            addPlayerButton.gameObject.SetActive(canAddPlayer && !isPopupVisible);
    }
}
