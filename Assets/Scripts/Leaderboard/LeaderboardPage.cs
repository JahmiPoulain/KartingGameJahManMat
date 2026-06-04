using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPage : MonoBehaviour
{
    private const int SlotsPerPage = 10;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI[] nameTexts = new TextMeshProUGUI[SlotsPerPage];
    [SerializeField] private TextMeshProUGUI[] scoreTexts = new TextMeshProUGUI[SlotsPerPage];

    [Header("Optional Row Buttons")]
    [Tooltip("Optionnel : assigne ici les boutons/lignes si tu veux pouvoir cliquer sur une ligne, par exemple pour sélectionner un joueur.")]
    [SerializeField] private Button[] rowButtons = new Button[SlotsPerPage];

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color localPlayerColor = Color.yellow;

    [Header("Empty Slot")]
    [SerializeField] private string emptyName = "--";
    [SerializeField] private string emptyScore = "--:--.---";

    public void SetEntry(
        int slotIndex,
        string playerName,
        string score,
        bool isLocalPlayer,
        bool prefixRank,
        int rank
    )
    {
        SetEntry(slotIndex, playerName, score, isLocalPlayer, prefixRank, rank, null);
    }

    public void SetEntry(
        int slotIndex,
        string playerName,
        string score,
        bool isLocalPlayer,
        bool prefixRank,
        int rank,
        Action onClick
    )
    {
        if (!IsValidSlot(slotIndex))
            return;

        Color color = isLocalPlayer ? localPlayerColor : normalColor;

        nameTexts[slotIndex].color = color;
        scoreTexts[slotIndex].color = color;

        nameTexts[slotIndex].text = prefixRank ? $"#{rank} {playerName}" : playerName;
        scoreTexts[slotIndex].text = score;

        ConfigureRowButton(slotIndex, onClick);
    }

    public void ClearEntry(int slotIndex, bool prefixRank, int rank)
    {
        if (!IsValidSlot(slotIndex))
            return;

        nameTexts[slotIndex].color = normalColor;
        scoreTexts[slotIndex].color = normalColor;

        nameTexts[slotIndex].text = prefixRank ? $"#{rank} {emptyName}" : emptyName;
        scoreTexts[slotIndex].text = emptyScore;

        ConfigureRowButton(slotIndex, null);
    }

    public void ClearPage(bool prefixRank, int startRank)
    {
        for (int i = 0; i < SlotsPerPage; i++)
            ClearEntry(i, prefixRank, startRank + i);
    }

    private void ConfigureRowButton(int slotIndex, Action onClick)
    {
        if (rowButtons == null)
            return;

        if (slotIndex < 0 || slotIndex >= rowButtons.Length)
            return;

        Button rowButton = rowButtons[slotIndex];

        if (rowButton == null)
            return;

        rowButton.onClick.RemoveAllListeners();

        bool hasAction = onClick != null;
        rowButton.interactable = hasAction;

        if (hasAction)
            rowButton.onClick.AddListener(() => onClick.Invoke());
    }

    private bool IsValidSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotsPerPage)
            return false;

        if (nameTexts == null || scoreTexts == null)
            return false;

        if (slotIndex >= nameTexts.Length || slotIndex >= scoreTexts.Length)
            return false;

        return nameTexts[slotIndex] != null && scoreTexts[slotIndex] != null;
    }
}
