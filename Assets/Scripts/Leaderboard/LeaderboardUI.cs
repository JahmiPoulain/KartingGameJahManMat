using UnityEngine;
using TMPro;

public class LeaderboardEntryUI : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;

    public void SetColor(Color color)
    {
        rankText.color = color;
        nameText.color = color;
        scoreText.color = color;
    }
}