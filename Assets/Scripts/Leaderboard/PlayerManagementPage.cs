using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerManagementPage : MonoBehaviour
{
    private const int MaxRows = 6;

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
    [SerializeField] private TextMeshProUGUI namePopupErrorText;
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
    [SerializeField] private string emptyNameError = "Enter a nickname.";
    [SerializeField] private string duplicateNameError = "This profile already exists.";
    [SerializeField] private string checkingProfilesMessage = "Checking profiles...";

    private Action<string> addPlayerCallback;
    private Action<string, string> renamePlayerCallback;
    private Action<string> deletePlayerCallback;
    private Action<string> choosePlayerCallback;
    private Action<string, string, Action<bool>> validatePlayerNameCallback;

    private string editedPlayerId;
    private string deletedPlayerId;
    private bool isRenamePopup;
    private bool isPopupVisible;
    private bool canAddPlayer = true;
    private bool isWaitingForNameValidation;
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
        Action<string> onChoosePlayer,
        Action<string, string, Action<bool>> onValidatePlayerName
    )
    {
        addPlayerCallback = onAddPlayer;
        renamePlayerCallback = onRenamePlayer;
        deletePlayerCallback = onDeletePlayer;
        choosePlayerCallback = onChoosePlayer;
        validatePlayerNameCallback = onValidatePlayerName;
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

    public void FocusFirstSelectable()
    {
        if (!isActiveAndEnabled)
            return;

        if (namePopupRoot != null && namePopupRoot.activeInHierarchy)
        {
            ConfigureNamePopupNavigation();

            if (nameInputField != null && nameInputField.gameObject.activeInHierarchy && nameInputField.interactable)
            {
                SelectGameObject(nameInputField.gameObject);
                nameInputField.ActivateInputField();
                return;
            }

            if (TrySelect(confirmNameButton))
                return;

            TrySelect(cancelNameButton);
            return;
        }

        if (deletePopupRoot != null && deletePopupRoot.activeInHierarchy)
        {
            ConfigureDeletePopupNavigation();

            if (TrySelect(cancelDeleteButton))
                return;

            TrySelect(confirmDeleteButton);
            return;
        }

        ConfigurePageNavigation();
        List<Selectable> selectables = GetPageSelectables();

        foreach (Selectable selectable in selectables)
        {
            if (TrySelect(selectable))
                return;
        }
    }

    public bool TryHandleCancel()
    {
        if (!isPopupVisible)
            return false;

        CloseAllPopups();
        return true;
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

        if (isActiveAndEnabled)
            FocusFirstSelectable();
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

        if (nameInputField != null)
        {
            nameInputField.onValueChanged.RemoveListener(OnNameInputChanged);
            nameInputField.onValueChanged.AddListener(OnNameInputChanged);
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
        EnsureNamePopupErrorText();
        SetNamePopupError(string.Empty);
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
            nameInputField.interactable = true;
        }

        SetNamePopupWaiting(false);
        FocusFirstSelectable();
    }

    private void ConfirmNamePopup()
    {
        if (isWaitingForNameValidation)
            return;

        string playerName = GetSanitizedNameInput();

        if (string.IsNullOrWhiteSpace(playerName))
        {
            SetNamePopupError(emptyNameError);
            return;
        }

        string ignoredProfileId = isRenamePopup ? editedPlayerId : null;

        if (validatePlayerNameCallback == null)
        {
            ConfirmValidatedName(playerName);
            return;
        }

        SetNamePopupWaiting(true);
        SetNamePopupError(checkingProfilesMessage);

        validatePlayerNameCallback.Invoke(playerName, ignoredProfileId, isAvailable =>
        {
            SetNamePopupWaiting(false);

            if (!isAvailable)
            {
                SetNamePopupError(duplicateNameError);
                return;
            }

            ConfirmValidatedName(playerName);
        });
    }

    private void ConfirmValidatedName(string playerName)
    {
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

        FocusFirstSelectable();
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

    private string GetSanitizedNameInput()
    {
        if (nameInputField == null || string.IsNullOrWhiteSpace(nameInputField.text))
            return string.Empty;

        string playerName = nameInputField.text.Trim();

        if (playerName.Length > maxPlayerNameLength)
            playerName = playerName.Substring(0, maxPlayerNameLength);

        return playerName;
    }

    private void OnNameInputChanged(string _)
    {
        if (isWaitingForNameValidation)
            return;

        SetNamePopupError(string.Empty);
    }

    private void SetNamePopupError(string message)
    {
        if (namePopupErrorText == null)
            return;

        namePopupErrorText.text = message ?? string.Empty;
        namePopupErrorText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
    }

    private void SetNamePopupWaiting(bool waiting)
    {
        isWaitingForNameValidation = waiting;

        if (confirmNameButton != null)
            confirmNameButton.interactable = !waiting;

        if (cancelNameButton != null)
            cancelNameButton.interactable = !waiting;

        if (nameInputField != null)
            nameInputField.interactable = !waiting;
    }

    private static bool TrySelect(Selectable selectable)
    {
        if (selectable == null || !selectable.gameObject.activeInHierarchy || !selectable.interactable)
            return false;

        SelectGameObject(selectable.gameObject);
        return true;
    }

    private void ConfigurePageNavigation()
    {
        List<List<Selectable>> grid = GetPageSelectableGrid();

        for (int rowIndex = 0; rowIndex < grid.Count; rowIndex++)
        {
            List<Selectable> row = grid[rowIndex];

            for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                Selectable selectable = row[columnIndex];
                if (selectable == null)
                    continue;

                Navigation navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.wrapAround = false;
                navigation.selectOnLeft = FindHorizontalSelectable(row, columnIndex, -1);
                navigation.selectOnRight = FindHorizontalSelectable(row, columnIndex, 1);
                navigation.selectOnUp = FindVerticalSelectable(grid, rowIndex, columnIndex, -1);
                navigation.selectOnDown = FindVerticalSelectable(grid, rowIndex, columnIndex, 1);
                selectable.navigation = navigation;
            }
        }

        if (addPlayerButton != null && addPlayerButton.gameObject.activeInHierarchy && addPlayerButton.interactable)
        {
            Selectable lastRowSelectable = FindLastSelectableInGrid(grid);

            Navigation addNavigation = addPlayerButton.navigation;
            addNavigation.mode = Navigation.Mode.Explicit;
            addNavigation.wrapAround = false;
            addNavigation.selectOnUp = lastRowSelectable;
            addNavigation.selectOnDown = null;
            addNavigation.selectOnLeft = null;
            addNavigation.selectOnRight = null;
            addPlayerButton.navigation = addNavigation;

            if (lastRowSelectable != null)
            {
                Navigation lastNavigation = lastRowSelectable.navigation;
                lastNavigation.selectOnDown = addPlayerButton;
                lastRowSelectable.navigation = lastNavigation;
            }

            LinkBottomRowToAddButton(grid, addPlayerButton);
        }
    }

    private List<Selectable> GetPageSelectables()
    {
        List<Selectable> selectables = new();

        foreach (PlayerManagementRow row in rows)
            row?.AddUsableButtons(selectables);

        AddIfUsable(selectables, addPlayerButton);

        return selectables;
    }

    private List<List<Selectable>> GetPageSelectableGrid()
    {
        List<List<Selectable>> grid = new();

        foreach (PlayerManagementRow row in rows)
        {
            if (row == null || !row.gameObject.activeInHierarchy)
                continue;

            List<Selectable> rowSelectables = new()
            {
                row.GetUsableButtonInColumn(0),
                row.GetUsableButtonInColumn(1),
                row.GetUsableButtonInColumn(2)
            };

            if (HasAnySelectable(rowSelectables))
                grid.Add(rowSelectables);
        }

        return grid;
    }

    private void ConfigureNamePopupNavigation()
    {
        List<Selectable> selectables = new();
        AddIfUsable(selectables, nameInputField);
        AddIfUsable(selectables, confirmNameButton);
        AddIfUsable(selectables, cancelNameButton);
        ConfigureNavigationLoop(selectables);
    }

    private void ConfigureDeletePopupNavigation()
    {
        List<Selectable> selectables = new();
        AddIfUsable(selectables, cancelDeleteButton);
        AddIfUsable(selectables, confirmDeleteButton);
        ConfigureNavigationLoop(selectables);
    }

    private static void ConfigureNavigationLoop(List<Selectable> selectables)
    {
        for (int i = 0; i < selectables.Count; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable == null)
                continue;

            Selectable previous = selectables.Count > 1 ? selectables[(i - 1 + selectables.Count) % selectables.Count] : null;
            Selectable next = selectables.Count > 1 ? selectables[(i + 1) % selectables.Count] : null;

            Navigation navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.wrapAround = selectables.Count > 1;
            navigation.selectOnUp = previous;
            navigation.selectOnLeft = previous;
            navigation.selectOnDown = next;
            navigation.selectOnRight = next;
            selectable.navigation = navigation;
        }
    }

    private static Selectable FindHorizontalSelectable(List<Selectable> row, int columnIndex, int direction)
    {
        if (row == null)
            return null;

        for (int i = columnIndex + direction; i >= 0 && i < row.Count; i += direction)
        {
            if (row[i] != null)
                return row[i];
        }

        return null;
    }

    private static Selectable FindVerticalSelectable(List<List<Selectable>> grid, int rowIndex, int columnIndex, int direction)
    {
        if (grid == null)
            return null;

        for (int i = rowIndex + direction; i >= 0 && i < grid.Count; i += direction)
        {
            List<Selectable> row = grid[i];

            if (row != null && columnIndex >= 0 && columnIndex < row.Count && row[columnIndex] != null)
                return row[columnIndex];
        }

        return null;
    }

    private static Selectable FindLastSelectableInGrid(List<List<Selectable>> grid)
    {
        if (grid == null)
            return null;

        for (int rowIndex = grid.Count - 1; rowIndex >= 0; rowIndex--)
        {
            List<Selectable> row = grid[rowIndex];
            if (row == null)
                continue;

            for (int columnIndex = row.Count - 1; columnIndex >= 0; columnIndex--)
            {
                if (row[columnIndex] != null)
                    return row[columnIndex];
            }
        }

        return null;
    }

    private static void LinkBottomRowToAddButton(List<List<Selectable>> grid, Selectable addButton)
    {
        if (grid == null || addButton == null)
            return;

        for (int rowIndex = grid.Count - 1; rowIndex >= 0; rowIndex--)
        {
            List<Selectable> row = grid[rowIndex];
            if (row == null)
                continue;

            bool linkedAnyButton = false;

            foreach (Selectable selectable in row)
            {
                if (selectable == null)
                    continue;

                Navigation navigation = selectable.navigation;
                if (navigation.selectOnDown == null)
                {
                    navigation.selectOnDown = addButton;
                    selectable.navigation = navigation;
                }

                linkedAnyButton = true;
            }

            if (linkedAnyButton)
                return;
        }
    }

    private static bool HasAnySelectable(List<Selectable> selectables)
    {
        if (selectables == null)
            return false;

        foreach (Selectable selectable in selectables)
        {
            if (selectable != null)
                return true;
        }

        return false;
    }

    private static void AddIfUsable(List<Selectable> selectables, Selectable selectable)
    {
        if (selectable == null || !selectable.gameObject.activeInHierarchy || !selectable.interactable)
            return;

        selectables.Add(selectable);
    }

    private static void SelectGameObject(GameObject target)
    {
        if (target == null)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(target);
        else
            target.GetComponent<Selectable>()?.Select();
    }

    private void EnsureNamePopupErrorText()
    {
        if (namePopupErrorText != null || namePopupRoot == null)
            return;

        GameObject errorObject = new GameObject("NamePopupErrorText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        errorObject.transform.SetParent(namePopupRoot.transform, false);

        RectTransform errorTransform = errorObject.GetComponent<RectTransform>();
        errorTransform.anchorMin = new Vector2(0.5f, 0.5f);
        errorTransform.anchorMax = new Vector2(0.5f, 0.5f);
        errorTransform.pivot = new Vector2(0.5f, 0.5f);
        errorTransform.sizeDelta = new Vector2(420f, 28f);

        if (nameInputField != null && nameInputField.transform is RectTransform inputTransform)
            errorTransform.anchoredPosition = inputTransform.anchoredPosition + new Vector2(0f, -45f);
        else
            errorTransform.anchoredPosition = new Vector2(0f, -45f);

        namePopupErrorText = errorObject.GetComponent<TextMeshProUGUI>();
        namePopupErrorText.alignment = TextAlignmentOptions.Center;
        namePopupErrorText.color = new Color(0.82f, 0.18f, 0.18f, 1f);
        namePopupErrorText.fontSize = 16f;
        namePopupErrorText.raycastTarget = false;
        namePopupErrorText.text = string.Empty;
        namePopupErrorText.gameObject.SetActive(false);
    }
}
