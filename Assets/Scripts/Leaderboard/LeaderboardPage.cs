using TMPro;
using UnityEngine;

public class LeaderboardPage : MonoBehaviour
{
    private const int SlotsPerPage = 10;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI[] nameTexts = new TextMeshProUGUI[SlotsPerPage];
    [SerializeField] private TextMeshProUGUI[] scoreTexts = new TextMeshProUGUI[SlotsPerPage];

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
        if (!IsValidSlot(slotIndex))
            return;

        Color color = isLocalPlayer ? localPlayerColor : normalColor;

        nameTexts[slotIndex].color = color;
        scoreTexts[slotIndex].color = color;

        nameTexts[slotIndex].text = prefixRank ? $"#{rank} {playerName}" : playerName;
        scoreTexts[slotIndex].text = score;
    }

    public void ClearEntry(int slotIndex, bool prefixRank, int rank)
    {
        if (!IsValidSlot(slotIndex))
            return;

        nameTexts[slotIndex].color = normalColor;
        scoreTexts[slotIndex].color = normalColor;

        nameTexts[slotIndex].text = prefixRank ? $"#{rank} {emptyName}" : emptyName;
        scoreTexts[slotIndex].text = emptyScore;
    }

    public void ClearPage(bool prefixRank, int startRank)
    {
        for (int i = 0; i < SlotsPerPage; i++)
        {
            ClearEntry(i, prefixRank, startRank + i);
        }
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
